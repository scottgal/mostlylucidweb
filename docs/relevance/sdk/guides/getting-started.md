# Getting started

Use .NET 10. The ready-to-use baseline needs no custom converter, store, predictor, background service or JSON source generator in your application.

## Obtain the packages

A successful **Relevance packages and Native AOT** workflow publishes the `relevance-nuget-packages` artifact. Extract it; the directory contains all four `.nupkg` files, `NuGet.Config`, the SDK documentation and the agent guide. Until public publication completes, restore with that configuration file. After public release, use NuGet.org normally.

Reference `Mostlylucid.Relevance.Sqlite` for the core and durable store. Add `Mostlylucid.Relevance.Learning` for intervention choice. Add `Mostlylucid.Sqlite.Vector` for native SQLite HNSW; exact SQL needs no extra extension package. Native runtime assets are platform-specific.

## First working recommender

```csharp
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;

await using var hub = new RelevanceHub(new SqliteRelevanceFactory("./relevance-data"));
var engine = await hub.OpenAsync(new RelevanceSpace("authorised-tenant", "products"),
    RelevanceDefinition.ForVectors(2));

await engine.SaveAsync("items", new RelevanceItem("boots", [.2f, .7f]));
await engine.SaveAsync("items", new RelevanceItem("jacket", [.8f, .1f]));
await engine.SaveAsync("events", new RelevanceObservation("event-1", "session-1", "boots"));
var result = await engine.RecommendAsync("session-1", "next", count: 2);
foreach (var item in result.Items)
    Console.WriteLine($"{item.Id}: {item.Score}");
```

The supplied envelopes are AOT-safe without application-generated JSON metadata. `ForVectors` supplies `items`, `events` and `outcomes` streams, the `traits` representation and `next` placement. Default metric is L2. Use `metric: "cosine"` for compatible nonzero vectors. Observation types `observe` and `helpful` add positive evidence. This is ranking, not a calibrated probability.

The application chooses meaningful coordinates and authorises the tenant. An awaited save means conversion and commit completed before the next operation. Stable event IDs permit retries within retention; changed content under the same ID is rejected. Candidates upsert by ID.

## Metadata and filtering

```csharp
var metadata = RelevanceJson.Element(new Dictionary<string, bool> { ["available"] = true });
var definition = RelevanceDefinition.ForVectors(2, filters: ["metadata.available"]);
var available = await hub.OpenAsync(new RelevanceSpace("authorised-tenant", "available-products"), definition);
await available.SaveAsync("items", new RelevanceItem("boots", [.2f, .7f], metadata));
var availableResult = await available.RecommendAsync("session-1", "next",
    Filter.Eq("metadata.available", true), count: 2);
```

`RelevanceItem.Metadata` is nested under `metadata`. Existing application DTOs and JSON can keep their own shape through explicit stream/point mappings; the nested convention belongs only to the supplied envelope. Metadata is returned with results: store fields appropriate for that boundary.

Dispose the hub, not its borrowed engines. Start with one space and exact search. Add representations, destinations, interventions, native indexing or shards when needed. The complete runnable package consumer is `samples/RelevancePackage` in the repository.
