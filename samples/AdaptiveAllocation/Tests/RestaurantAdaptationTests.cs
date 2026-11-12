using Mostlylucid.Packing.OrTools;
using Mostlylucid.Packing.Planning;

namespace AdaptiveAllocation.Tests;

public sealed class RestaurantAdaptationTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

    [Fact]
    public void AHighChairChangesTheFootprintWithoutChangingThePartySize()
    {
        RestaurantTable[] tables = [new("four", 4, true), new("six", 6, true)];
        var party = new PartyRequest("family", 4, PreferredTable: "four");
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var before = solver.Solve(RestaurantAllocation.Seat(party, tables, 1), Budget);
        var changed = RestaurantAllocation.Seat(party with { HighChairs = 1 }, tables, 1, revision: 1);
        var after = solver.Solve(changed, Budget);
        Assert.Equal(["family@four"], before.Selected);
        Assert.Equal(["family@six"], after.Selected);
        Assert.Equal(4, party.People);
        Assert.Equal(1, changed.Candidates["family@six"].Demands.Single(d => d.Dimension.Value == "high-chair").Units);
    }

    [Fact]
    public void EquipmentStockAndTableCompatibilityRemainHardRules()
    {
        var party = new PartyRequest("family", 4, 1);
        var solver = new CpSatPackingSolver();
        Assert.Empty(solver.Solve(RestaurantAllocation.Seat(party, [new("six", 6, true)], 0), Budget).Selected);
        Assert.Empty(solver.Solve(RestaurantAllocation.Seat(party, [new("six", 6, false)], 1), Budget).Selected);
        Assert.Throws<ArgumentException>(() => RestaurantAllocation.Seat(party with { HighChairs = 5 }, [], 1));
    }

    [Fact]
    public void ProtectedStockConstrainsHighMarginPublicOrdersAndCanBeReleased()
    {
        MealRequest[] requests =
        [
            new("a", false, 1_200), new("b", false, 1_200), new("c", false, 1_200), new("guest", true)
        ];
        var solver = new CpSatPackingSolver();
        var protectedPlan = solver.Solve(RestaurantAllocation.Meals(requests, 3, 1), Budget);
        Assert.Equal(4, protectedPlan.Selected.Count);
        Assert.Contains("guest@special", protectedPlan.Selected);
        Assert.Equal(2, protectedPlan.Selected.Count(id => id.EndsWith("@special") && !id.StartsWith("guest@")));
        var released = solver.Solve(RestaurantAllocation.Meals(requests, 3, 0), Budget);
        Assert.Contains("guest@ordinary", released.Selected);
        Assert.Equal(3, released.Selected.Count(id => id.EndsWith("@special")));
    }

    [Fact]
    public void ProtectedCustomersStillShareTheSameFiniteTotal()
    {
        var problem = RestaurantAllocation.Meals([new("a", true), new("b", true), new("c", true)], 2, 2);
        var selected = new CpSatPackingSolver().Solve(problem, Budget).Selected;
        Assert.Equal(2, selected.Count(id => id.EndsWith("@special")));
        Assert.Equal(1, selected.Count(id => id.EndsWith("@ordinary")));
    }

    [Fact]
    public void AReserveIsNotAutomaticallyConsumedByGeneralDemand()
    {
        var problem = RestaurantAllocation.Meals([new("a", false)], 1, 1);
        Assert.Equal(["a@ordinary"], new CpSatPackingSolver().Solve(problem, Budget).Selected);
    }
}
