using Mostlylucid.Packing.Planning;
using static AdaptiveAllocation.AllocationModel;

namespace AdaptiveAllocation;

public sealed record RestaurantTable(string Id, int Places, bool AllowsHighChair);
public sealed record PartyRequest(string Id, int People, int HighChairs = 0, string? PreferredTable = null);
public sealed record MealRequest(string Id, bool MayUseProtectedStock, long SpecialMargin = 900,
    long OrdinaryMargin = 500);

public static class RestaurantAllocation
{
    // One fixed service interval: tables are exclusively allocated. Place units are suitability, not shared seats.
    public static PackingProblem Seat(PartyRequest party, IReadOnlyList<RestaurantTable> tables,
        int availableHighChairs, long revision = 0)
    {
        if (party.People <= 0 || party.HighChairs < 0 || party.HighChairs > party.People)
            throw new ArgumentException("High-chair users are part of the party, not additional people.");
        ArgumentOutOfRangeException.ThrowIfNegative(availableHighChairs);
        if (tables.Any(table => table.Places <= 0)) throw new ArgumentException("Tables need positive capacity.");
        // This venue chooses two place units per high chair. It is not a universal furniture rule.
        var places = checked(party.People + party.HighChairs);
        var capacities = tables.Select(table => new Capacity(Dimension("table/" + table.Id), 1))
            .Append(new Capacity(Dimension("high-chair"), availableHighChairs));
        var choices = tables.Where(table => table.Places >= places &&
            (party.HighChairs == 0 || table.AllowsHighChair)).Select(table =>
        {
            var demands = new List<Consumption> { new(Dimension("table/" + table.Id), 1) };
            if (party.HighChairs > 0) demands.Add(new(Dimension("high-chair"), party.HighChairs));
            return new AllocationOption($"{party.Id}@{table.Id}", party.Id, demands,
                Value: 100, Cost: table.Places - places, Preference: party.PreferredTable == table.Id ? 10 : 0);
        });
        return Build(capacities, [new Request(party.Id)], choices, revision);
    }

    // Counted inventory within one service. This proposes a plan; it does not decrement a durable stock ledger.
    public static PackingProblem Meals(IReadOnlyList<MealRequest> requests, int specialsOnHand,
        int protectedSpecials, long revision = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(specialsOnHand);
        if (protectedSpecials < 0 || protectedSpecials > specialsOnHand)
            throw new ArgumentOutOfRangeException(nameof(protectedSpecials));
        Capacity[] capacities =
        [
            new(Dimension("special/total"), specialsOnHand),
            new(Dimension("special/public"), specialsOnHand - protectedSpecials)
        ];
        var options = requests.SelectMany(request =>
        {
            var demands = new List<Consumption> { new(Dimension("special/total"), 1) };
            if (!request.MayUseProtectedStock) demands.Add(new(Dimension("special/public"), 1));
            return new AllocationOption[]
            {
                new($"{request.Id}@special", request.Id, demands, request.SpecialMargin),
                new($"{request.Id}@ordinary", request.Id, [], request.OrdinaryMargin)
            };
        });
        return Build(capacities, requests.Select(request => new Request(request.Id)), options, revision);
    }
}
