using ResourcePacking.Examples;
using StreamingAvailability;

namespace ResourcePacking.Tests;

public sealed class SalonPackingTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(2);
    private readonly CpSatPackingSolver _solver = new();

    [Fact]
    public void JointOptimisationSavesTheOnlyTechnicianForTheConstrainedClient()
    {
        var (salon, _, _, start, end, requests) = Scenarios.Salon(_solver);
        var problem = salon.Capture(requests, start, end).Problem;
        Assert.Single(new GreedyPackingSolver().Solve(problem, Budget).Selected);
        var result = salon.Plan(_solver, requests, start, end, Budget);
        Assert.Equal(2, result.Proposals.Count);
        Assert.Contains(result.Proposals, proposal => proposal.Request.Id == requests[0].Id && proposal.TechnicianId == "cara");
        Assert.Contains(result.Proposals, proposal => proposal.Request.Id == requests[1].Id && proposal.TechnicianId == "anna");
    }

    [Fact]
    public void PreferenceBreaksTiesAndBlockedClientsHaveNoCandidate()
    {
        var (salon, _, _, start, end, requests) = Scenarios.Salon(_solver);
        var blocked = requests[0] with { Id = "blocked", Appointment = requests[0].Appointment with { ClientId = "blocked" } };
        var plan = salon.Plan(_solver, [blocked, requests[0]], start, end, Budget);
        Assert.Equal("anna", Assert.Single(plan.Proposals).TechnicianId);
        Assert.Contains("blocked", plan.Selection.Unfulfilled);
        Assert.False(salon.Capture(requests, start, end, candidateLimit: 1).Problem.CandidatesComplete);
    }

    [Fact]
    public async Task StaleCompetingProposalsHaveOneWinnerAndExpiryMakesCapacityVisibleAgain()
    {
        var (salon, ledger, clock, start, end, requests) = Scenarios.Salon(_solver);
        var first = requests[1];
        var second = first with { Id = "rival", Appointment = first.Appointment with { ClientId = "rival" } };
        var firstOffer = Assert.Single(salon.Plan(_solver, [first], start, end, Budget).Proposals);
        var secondOffer = Assert.Single(salon.Plan(_solver, [second], start, end, Budget).Proposals);
        var lifetime = TimeSpan.FromMinutes(5);
        var replies = await Task.WhenAll(
            Task.Run(() => salon.TryHold(Guid.NewGuid(), first.Appointment.ClientId, firstOffer, lifetime)),
            Task.Run(() => salon.TryHold(Guid.NewGuid(), second.Appointment.ClientId, secondOffer, lifetime)));

        var held = Assert.Single(replies, reply => reply.Success);
        Assert.Contains(replies, reply => reply.Reason == "NoCapacity");
        Assert.Empty(salon.Plan(_solver, [first, second], start, end, Budget).Proposals);
        clock.Advance(lifetime);
        var refreshed = salon.Plan(_solver, [first, second], start, end, Budget);
        Assert.Single(refreshed.Proposals);
        Assert.True(refreshed.Selection.Snapshot!.AvailabilityVersion > held.Version);
        Assert.Equal(ReservationState.Expired, ledger.GetReservation(held.ReservationId!.Value)!.State);
    }

    [Fact]
    public void PolicyChangeInvalidatesNewHoldsWhileAnAcceptedCommandCanReplay()
    {
        var (salon, ledger, _, start, end, requests) = Scenarios.Salon(_solver);
        var offer = Assert.Single(salon.Plan(_solver, [requests[0]], start, end, Budget).Proposals);
        var owner = offer.Request.Appointment.ClientId;
        var id = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(1);
        var held = salon.TryHold(id, owner, offer, ttl);
        Assert.True(held.Success);
        salon.SetClientMayBook(owner, false);
        Assert.Equal(held, salon.TryHold(id, owner, offer, ttl));
        Assert.Equal("PolicyChanged", salon.TryHold(Guid.NewGuid(), owner, offer, ttl).Reason);
        Assert.Equal("ClientMismatch", salon.TryHold(Guid.NewGuid(), "somebody-else", offer, ttl).Reason);
        Assert.Empty(salon.Plan(_solver, [requests[0]], start, end, Budget).Proposals);
        Assert.True(ledger.Confirm(Guid.NewGuid(), owner, held.ReservationId!.Value).Success);
    }

    [Fact]
    public void TemplatesAreReusedAcrossHoldsAndInvalidatedForTheChangedClient()
    {
        var (salon, _, _, start, end, requests) = Scenarios.Salon(_solver);
        Assert.Equal(2, salon.Capture(requests, start, end).GeneratedRequests);
        var before = salon.Capture(requests, start, end);
        Assert.Equal(0, before.GeneratedRequests);
        var offer = Assert.Single(salon.Plan(_solver, [requests[1]], start, end, Budget).Proposals);
        Assert.True(salon.TryHold(Guid.NewGuid(), offer.Request.Appointment.ClientId, offer, TimeSpan.FromMinutes(1)).Success);
        var held = salon.Capture([requests[1]], start, end);
        Assert.Equal(0, held.GeneratedRequests);
        Assert.True(held.Problem.Snapshot!.AvailabilityVersion > before.Problem.Snapshot!.AvailabilityVersion);
        salon.SetClientMayBook(requests[1].Appointment.ClientId, false);
        Assert.Equal(1, salon.Capture([requests[1]], start, end).GeneratedRequests);
        Assert.Empty(salon.Capture([requests[1]], start, end).Offers);
    }

    [Fact]
    public void CandidateIdsRemainStableWhenTimeAdvancesAndSelectivePlansUseCurrentCapacity()
    {
        var (salon, ledger, clock, start, end, requests) = Scenarios.Salon(_solver);
        var wide = requests[1] with { Appointment = requests[1].Appointment with { EarliestStart = 0, LatestStart = 60 } };
        var before = salon.Capture([wide], start, end);
        clock.Advance(TimeSpan.FromMinutes(5));
        var after = salon.Capture([wide], start, end);
        Assert.Equal(0, after.GeneratedRequests);
        foreach (var (id, offer) in after.Offers)
            Assert.Equal(offer, before.Offers[id]);

        var incremental = new IncrementalPackingSolver(_solver);
        var plan = salon.Plan(incremental, [wide], start, end, Budget);
        var chosen = Assert.Single(plan.Proposals);
        var command = Guid.NewGuid();
        Assert.True(salon.TryHold(command, chosen.Request.Appointment.ClientId, chosen, TimeSpan.FromMinutes(1)).Success);
        Assert.True(ledger.Confirm(Guid.NewGuid(), chosen.Request.Appointment.ClientId, command).Success);
        var changed = salon.Plan(incremental, [wide], start, end, Budget);
        var full = salon.Plan(_solver, [wide], start, end, Budget);
        Assert.Equal(full.Selection.Selected.Count, changed.Selection.Selected.Count);
        Assert.Equal(full.Selection.Utility, changed.Selection.Utility);
        Assert.DoesNotContain(changed.Proposals, proposal => proposal.Start == chosen.Start);
    }
}
