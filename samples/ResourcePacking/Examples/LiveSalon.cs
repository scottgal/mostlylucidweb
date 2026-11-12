using System.Collections.Frozen;
using NailSalonScheduling;
using PackingCalendars;
using StreamingAvailability;

namespace ResourcePacking.Examples;

public sealed record AppointmentProposal(PlanningSnapshot Snapshot, SalonRequest Request,
    string TechnicianId, string StationId, int Start);

public sealed record SalonPlan(PackingResult Selection, IReadOnlyList<AppointmentProposal> Proposals);

public sealed class LiveSalon
{
    private readonly object _gate = new();
    private readonly string _venue;
    private readonly DateTimeOffset _origin;
    private readonly BookingLedger _ledger;
    private readonly RollingAvailability _availability;
    private readonly Technician[] _technicians;
    private readonly FrozenDictionary<string, Station> _stations;
    private readonly Dictionary<string, Client> _clients;
    private readonly SalonService[] _services;
    private readonly Roster _roster;
    private readonly TimeRange _opening;
    private readonly Dictionary<Guid, (AppointmentProposal Proposal, TimeSpan Lifetime, Reply Reply)> _commands = new();
    private readonly Dictionary<string, (SalonRequest Request, IReadOnlyList<Offer> Offers)> _offerCache =
        new(StringComparer.Ordinal);
    private long _policyVersion;

    public LiveSalon(string venue, DateTimeOffset origin, BookingLedger ledger, IReadOnlyList<Technician> technicians,
        IReadOnlyList<Station> stations, IReadOnlyList<Client> clients, IReadOnlyList<SalonService> services,
        Roster roster, TimeRange opening)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venue);
        _venue = venue;
        _origin = origin;
        _ledger = ledger;
        _availability = new RollingAvailability(ledger, TimeSpan.FromDays(1));
        _technicians = technicians.Select(technician => technician with
        {
            Services = technician.Services.ToFrozenSet(StringComparer.Ordinal),
            Availability = Array.AsReadOnly(technician.Availability.ToArray()),
            TimeOff = Array.AsReadOnly(technician.TimeOff.ToArray())
        }).ToArray();
        _stations = stations.Select(station => station with { Services = station.Services.ToFrozenSet(StringComparer.Ordinal) })
            .ToFrozenDictionary(station => station.Id, StringComparer.Ordinal);
        _clients = clients.Select(client => client with
        {
            ExcludedTechnicianIds = client.ExcludedTechnicianIds.ToFrozenSet(StringComparer.Ordinal)
        }).ToDictionary(client => client.Id, StringComparer.Ordinal);
        _services = services.ToArray();
        _roster = new Roster(Array.AsReadOnly(roster.Assigned.ToArray()), Array.AsReadOnly(roster.Unfilled.ToArray()));
        _opening = opening;
        _ = NewPlanner();
    }

    public SalonCandidates Capture(IReadOnlyList<SalonRequest> requests, long start, long end, int candidateLimit = 10_000)
    {
        var openingStart = Slots.Exact(_origin.AddMinutes(_opening.Start));
        var openingEnd = Slots.Exact(_origin.AddMinutes(_opening.End));
        if (start < openingStart || end > openingEnd || end <= start)
            throw new ArgumentOutOfRangeException(nameof(end), "The planning horizon must be inside this salon day.");

        var input = requests.ToDictionary(request => request.Id, StringComparer.Ordinal);
        var generated = new Dictionary<string, IReadOnlyList<Offer>>(StringComparer.Ordinal);
        var generatedRequests = 0;
        Snapshot availability;
        PlanningSnapshot snapshot;
        DateTimeOffset now;
        lock (_gate)
        {
            foreach (var old in _offerCache.Keys.Where(id => !input.ContainsKey(id)).ToArray())
                _offerCache.Remove(old);
            AppointmentPlanner? planner = null;
            foreach (var request in input.Values)
            {
                if (!_offerCache.TryGetValue(request.Id, out var entry) || entry.Request != request)
                {
                    planner ??= NewPlanner();
                    var offers = planner.FindOffers(request.Appointment, int.MaxValue).Offers.Select(offer => offer with
                    {
                        Demands = Array.AsReadOnly(offer.Demands.ToArray())
                    }).ToArray();
                    entry = (request, Array.AsReadOnly(offers));
                    _offerCache[request.Id] = entry;
                    generatedRequests++;
                }
                generated.Add(request.Id, entry.Offers);
            }
            _availability.Advance();
            now = _ledger.Clock.GetUtcNow();
            start = Math.Min(end, Math.Max(start, Slots.Floor(now)));
            availability = _availability.TryCapture(start, end) ?? _ledger.Capture(start, end);
            snapshot = new PlanningSnapshot(_venue, _policyVersion, availability.Version, start, end);
        }

        return CalendarPacking.Appointments(_origin, generated, _stations, input.Values.ToArray(), availability,
            snapshot, now, candidateLimit) with { GeneratedRequests = generatedRequests };
    }

    public SalonPlan Plan(IPackingSolver solver, IReadOnlyList<SalonRequest> requests, long start, long end,
        TimeSpan budget, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = requests.ToDictionary(request => request.Id, StringComparer.Ordinal);
        var candidates = Capture(input.Values.ToArray(), start, end);
        var result = solver.Solve(candidates.Problem, budget, cancellationToken);
        var proposals = result.Selected.Select(id =>
        {
            var offer = candidates.Offers[id];
            return new AppointmentProposal(candidates.Problem.Snapshot!, input[candidates.Problem.Candidates[id].RequestId],
                offer.TechnicianId, offer.StationId, offer.Start);
        }).ToArray();
        return new SalonPlan(result, Array.AsReadOnly(proposals));
    }

    public void SetClientMayBook(string clientId, bool mayBook)
    {
        lock (_gate)
        {
            var client = _clients[clientId];
            if (client.MayBook == mayBook) return;
            _clients[clientId] = client with { MayBook = mayBook };
            foreach (var id in _offerCache.Where(pair => pair.Value.Request.Appointment.ClientId == clientId)
                         .Select(pair => pair.Key).ToArray())
                _offerCache.Remove(id);
            _policyVersion++;
        }
    }

    public Reply TryHold(Guid commandId, string owner, AppointmentProposal proposal, TimeSpan lifetime)
    {
        // The caller supplies the authenticated client identity.
        if (!string.Equals(owner, proposal.Request.Appointment.ClientId, StringComparison.Ordinal))
            return new Reply(false, "ClientMismatch");

        lock (_gate)
        {
            if (_commands.TryGetValue(commandId, out var prior))
            {
                if (prior.Proposal != proposal || prior.Lifetime != lifetime)
                    throw new InvalidOperationException("An idempotency key was reused for a different offer.");
                return prior.Reply;
            }

            Reply reply;
            if (proposal.Snapshot.Venue != _venue || proposal.Snapshot.PolicyVersion != _policyVersion)
            {
                reply = new Reply(false, "PolicyChanged", Version: _ledger.Version);
            }
            else
            {
                var offer = NewPlanner().FindOffers(proposal.Request.Appointment, int.MaxValue).Offers.FirstOrDefault(
                    candidate => candidate.Start == proposal.Start && candidate.TechnicianId == proposal.TechnicianId &&
                                 candidate.StationId == proposal.StationId);
                var demands = offer?.Demands.Select(demand => CalendarPacking.ToLedgerDemand(_origin, demand)).ToArray();
                reply = demands is null || demands.Any(demand => demand.Start < proposal.Snapshot.Start ||
                                                                 demand.End > proposal.Snapshot.End)
                    ? new Reply(false, "OfferNoLongerEligible", Version: _ledger.Version)
                    : _ledger.TryHold(commandId, owner, demands, lifetime);
            }

            _commands.Add(commandId, (proposal, lifetime, reply));
            return reply;
        }
    }

    private AppointmentPlanner NewPlanner() =>
        new(_technicians, _stations.Values.ToArray(), _clients.Values, _services, _roster, _opening, []);
}
