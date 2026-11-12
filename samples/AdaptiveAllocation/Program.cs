using AdaptiveAllocation;
using Mostlylucid.Packing.OrTools;
using Mostlylucid.Packing.Planning;
using Mostlylucid.Relevance;

var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
var budget = TimeSpan.FromSeconds(30);
RestaurantTable[] tables = [new("four", 4, true), new("six", 6, true)];
var party = new PartyRequest("family", 4, PreferredTable: "four");
var before = solver.Solve(RestaurantAllocation.Seat(party, tables, 1), budget);
var changed = solver.Solve(RestaurantAllocation.Seat(party with { HighChairs = 1 }, tables, 1, revision: 1), budget);
Console.WriteLine($"Party of four: {string.Join(", ", before.Selected)}");
Console.WriteLine($"With one high chair (five place units): {string.Join(", ", changed.Selected)}");

MealRequest[] meals =
[
    new("public-a", false, SpecialMargin: 1_200), new("public-b", false, SpecialMargin: 1_200),
    new("public-c", false, SpecialMargin: 1_200), new("invited-customer", true)
];
var protectedPlan = solver.Solve(RestaurantAllocation.Meals(meals, 3, 1), budget);
var releasedPlan = solver.Solve(RestaurantAllocation.Meals(meals, 3, 0, revision: 1), budget);
Console.WriteLine($"Chef protects one special: {string.Join(", ", protectedPlan.Selected)}");
Console.WriteLine($"Chef releases that reserve: {string.Join(", ", releasedPlan.Selected)}");

await using var relevance = new RelevanceEngine(RelevanceDefinition.ForVectors(2), new MemoryRelevanceStore());
foreach (var snapshot in DemoFixture.Suppliers())
    await relevance.SaveAsync("items", new RelevanceItem(snapshot.Supplier.Id, snapshot.Supplier.PreferenceTraits));
await relevance.SaveAsync("events", new RelevanceObservation("previous-choice", "operator", "online"));

var memory = DemoFixture.Outcomes();
Console.WriteLine("Operator history favours online. Supplier costs below are fictional utility points.");
await RouteAsync("New work, no retained history", DemoFixture.Work() with { CurrentSupplier = null },
    DemoFixture.Suppliers(retainContext: false), DemoFixture.Quotes(0));
await RouteAsync("Conversation already warm locally", DemoFixture.Work(), DemoFixture.Suppliers(), DemoFixture.Quotes());
await RouteAsync("Local context was evicted", DemoFixture.Work(), DemoFixture.Suppliers(retainContext: false), DemoFixture.Quotes());
var full = DemoFixture.Suppliers().Select(snapshot => snapshot.Supplier.Id == "local"
    ? snapshot with { Supplier = snapshot.Supplier with { Slots = 0 } } : snapshot).ToArray();
await RouteAsync("Local supplier has no execution slots", DemoFixture.Work(), full, DemoFixture.Quotes());

for (var index = 0; index < 5; index++)
    memory.Observe(new($"online-failed-{index}", "extract-json", "online", false));
await RouteAsync("New completed failures change the estimate", DemoFixture.Work() with { CurrentSupplier = null },
    DemoFixture.Suppliers(retainContext: false), DemoFixture.Quotes(0));

Console.WriteLine("All results are proposals against supplied snapshots; no booking, stock or provider call was committed.");

async Task RouteAsync(string label, WorkRequest request, SupplierSnapshot[] suppliers, TokenQuote[] quotes)
{
    var input = await WorkRouting.BuildAsync([request], suppliers, quotes, memory, DemoFixture.Now,
        preferences: relevance);
    var result = solver.Solve(input.Problem, budget);
    if (result.Status is not (SolveStatus.Optimal or SolveStatus.Feasible) || result.Selected.Count != 1)
        throw new InvalidOperationException($"The fixture did not return a feasible allocation: {result.Status}.");
    var choice = input.Choices[result.Selected.Single()];
    Console.WriteLine($"{label}: {choice.Supplier} ({result.Status}); replay {choice.ReplayTokens:N0} tokens; " +
        $"cost {choice.Allocation.Cost}; preference bonus {choice.Allocation.Preference}; " +
        $"outcome support {choice.Evidence.Completed}.");
}
