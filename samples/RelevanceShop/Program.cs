using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;

var config = Path.Combine(AppContext.BaseDirectory, "relevance.yaml");
var database = args.FirstOrDefault() ?? Path.Combine(Path.GetTempPath(), $"relevance-demo-{Guid.NewGuid():N}.db");
// Standalone lifetime: the factory starts a bounded worker; await using drains it and closes SQLite.
await using var relevance = await SqliteRelevance.OpenAsync(config, database);

var products = new[]
{
    new { id = "boots", name = "Walking boots", category = "outdoors", price = 100, available = true },
    new { id = "jacket", name = "Waterproof jacket", category = "outdoors", price = 110, available = true },
    new { id = "tent", name = "Lightweight tent", category = "outdoors", price = 250, available = true },
    new { id = "headphones", name = "Headphones", category = "electronics", price = 105, available = true },
    new { id = "kettle", name = "Kettle", category = "home", price = 35, available = true },
    new { id = "unavailable", name = "Sold-out boots", category = "outdoors", price = 99, available = false }
};
// Catalogue conversion happens here. Later interactions reuse these stored vectors by candidate ID.
foreach (var product in products) await relevance.SaveAsync("products", product);

// Generated IDs are convenient for this demo. Real integrations keep the same ID when retrying an event.
await relevance.SaveAsync("events", new { id = Guid.NewGuid().ToString(), subject = "session-42", candidate = "boots", type = "compare" });
// The placement's availability rule AND this budget apply before each head takes its nearest candidates.
var result = await relevance.RecommendAsync("session-42", "outdoors", Filter.Lte("price", 120), count: 3);
Console.WriteLine($"Outdoors, up to £120; database: {database}");
foreach (var item in result.Items)
    Console.WriteLine($"{item.Data.GetProperty("name").GetString()}: {item.Score:F5}, heads: {string.Join(", ", item.Reasons.Select(r => r.Head))}");

// Simulate a final attributed success on the first displayed item. This changes a reliability heuristic,
// not a causal estimate. Production callers record actual exposure and use a defined attribution window.
await relevance.SaveAsync("outcomes", new { id = Guid.NewGuid().ToString(), recommendation = result.Id, candidate = result.Items[0].Id, reward = 1.0 });

// The new event changes live state before this awaited recommendation read.
await relevance.SaveAsync("events", new { id = Guid.NewGuid().ToString(), subject = "session-42", candidate = "headphones", type = "cart" });
var updated = await relevance.RecommendAsync("session-42", "home", count: 3);
Console.WriteLine("After adding headphones:");
foreach (var item in updated.Items) Console.WriteLine($"{item.Id}: {item.Score:F5}");

var prediction = await relevance.PredictAsync("session-42");
// Sparse rows abstain. RemainingProbability is deliberately not redistributed across named outcomes.
Console.WriteLine($"Destination estimate: {prediction.Status}; support={prediction.SupportingTransitions}, unresolved={prediction.RemainingProbability:F2}");

// Exactly the same engine, with raw system-state observations instead of product references.
await using var diagnostics = await SqliteRelevance.OpenAsync(Path.Combine(AppContext.BaseDirectory, "diagnostics.yaml"),
    Path.ChangeExtension(database, ".diagnostics.db"));
await diagnostics.SaveAsync("actions", new { id = "inspect-queue", handler = "inspect-queue-metrics", available = true,
    latency_ms = 600, error_fraction = 0.1, retries_per_request = 2, queue_depth = 150 });
for (var i = 0; i < 4; i++)
{
    // Independent synthetic journeys teach the transition baseline what followed "degraded".
    var subject = $"synthetic-incident-{Guid.NewGuid():N}";
    await diagnostics.SaveAsync("telemetry", new { id = Guid.NewGuid().ToString(), subject, type = "sample", state = "degraded",
        latency_ms = 600, error_fraction = 0.1, retries_per_request = 2, queue_depth = 150 });
    await diagnostics.SaveAsync("telemetry", new { id = Guid.NewGuid().ToString(), subject, type = "sample", state = i < 3 ? "outage" : "recovered",
        latency_ms = i < 3 ? 2000 : 100, error_fraction = i < 3 ? 0.8 : 0.01, retries_per_request = i < 3 ? 5 : 0, queue_depth = i < 3 ? 500 : 5 });
}
await diagnostics.SaveAsync("telemetry", new { id = Guid.NewGuid().ToString(), subject = "current-incident", type = "sample", state = "degraded",
    latency_ms = 550, error_fraction = 0.1, retries_per_request = 2, queue_depth = 140 });
var actions = await diagnostics.RecommendAsync("current-incident", "diagnostics");
Console.WriteLine($"Suggested diagnostic: {actions.Items[0].Id} (returned for the application to dispatch; not executed)");
var destination = await diagnostics.PredictAsync("current-incident");
Console.WriteLine("Synthetic diagnostic history, not a calibrated production forecast:");
foreach (var outcome in destination.Outcomes) Console.WriteLine($"{outcome.Destination}: {outcome.Probability:P0}");
