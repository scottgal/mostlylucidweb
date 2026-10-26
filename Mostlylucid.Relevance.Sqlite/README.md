# Mostlylucid.Relevance.Sqlite

Durable storage and vector retrieval for [Mostlylucid.Relevance](../Mostlylucid.Relevance/README.md). Start with one SQLite file and the same engine used throughout the series:

```csharp
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;

var definition = RelevanceDefinition.ForVectors(2, filters: ["available"]);
await using var hub = new RelevanceHub(new SqliteRelevanceFactory("./relevance-data"));
var products = await hub.OpenAsync(new RelevanceSpace(authorisedTenant, "products"), definition);
```

`definition` supplies the configured points, streams, placements and optional destinations. Use a typed definition or `RelevanceDefinition.FromJson` for Native AOT; the [package sample](../samples/RelevancePackage/Program.cs) includes a complete working example. The application authorises the tenant before opening a space. The hub owns returned engines and drains accepted work on disposal.

## Start with exact search

The default needs no vector-extension setup. It stores float32 vectors in ordinary tables and registers an exact distance function with SQLite. Eligibility is evaluated before ordering and limiting results. This supports small catalogues and supplies the reference for testing approximate retrieval.

Candidates, their named representations, subject/model state, attribution and deduplication are stored transactionally. WAL, FULL synchronous mode, foreign keys and a busy timeout are configured. The bounded engine worker orders operations; Microsoft.Data.Sqlite's actual database calls are synchronous.

## Enable SQLite-owned HNSW when useful

Add the separate [Mostlylucid.Sqlite.Vector](../Mostlylucid.Sqlite.Vector/README.md) native-assets package and supply its pinned extension:

```csharp
using Mostlylucid.Sqlite.Vector;

var vectors = new SqliteVectorOptions { ExtensionPath = SqliteVectorExtension.Path };
var stores = new SqliteRelevanceFactory("./relevance-data", vectors: vectors);
await using var hub = new RelevanceHub(stores);
```

Use this factory in place of the exact-only factory above. Save, recommend, predict and related-candidate calls stay the same. The package carries the platform library; no runtime download or consumer-side Rust build is required. Packages remain unpublished previews.

Each point/signature/space gets a native virtual table. The extension owns its HNSW graph and persists it in SQLite shadow tables. No separate managed graph or external index file/service is used. Unfiltered retrieval can use HNSW; queries with eligibility constraints use exact SQL, including mandatory placement restrictions. `SearchMode` describes that policy, not a promise that every query uses ANN. Distances retain core units, and recall must be checked against the exact baseline.

The extension refreshes graphs on reconnect, not between independent live connections. A per-space lease rejects competing .NET provider owners of the same native space; independent SQLite clients must respect the same single-owner contract. Reuse one live hub per set of spaces. Back up an active database through SQLite's backup API to include committed WAL state.

## Add independent domains and shards

Open another `RelevanceSpace` to isolate another tenant/domain. One file remains sufficient until separate shard files are useful:

```csharp
var stores = new SqliteRelevanceFactory("./sharded-relevance-data", ["east", "west"], vectors);
```

Choose this topology when creating a new data directory. Each complete tenant/space routes to one shard so state, candidates and attribution remain atomic. The persisted manifest rejects topology changes that would silently reroute existing data; migrate explicitly into a new directory. This is whole-space partitioning, not distributed top-K within one large catalogue.

## Provider and filter contracts

`Filter` expresses typed comparisons and conjunctions. `SqliteFilterCompiler` generates parameterised SQL over declared JSON properties; it accepts no arbitrary query SQL. Other providers implement their own equivalent semantics through `IRelevanceStore` and `IRelevanceStoreFactory`. See the shipped [agent guide](../Mostlylucid.Relevance/AGENTS.md) for invariants and adaptation recipes.

For existing JIT integrations, `SqliteRelevance.OpenAsync("relevance.yaml")` still opens one owned engine, and `services.AddRelevanceSqlite(...)` registers one hosted engine. The optional `vectorExtensionPath` argument on that convenience API selects **sqlite-vec scalar exact distances**, not HNSW. It requires an operator-installed binary, loads its explicit entry point and verifies `vec_version()`; failures propagate. Use `SqliteVectorOptions` for the SQLite-owned HNSW path above.

## Verification

Run the suite with both native paths enabled when the corresponding binaries are installed:

```sh
RELEVANCE_SQLITE_VEC=/installed/vec0.dylib \
RELEVANCE_SQLITE_HNSW=/installed/libsqlite_vector_rs.dylib \
dotnet test Mostlylucid.Relevance.Tests -c Release
```

Unset variables explicitly skip their native tests. Local macOS ARM64 verification includes published Native AOT consumers of the actual NuGet packages, exercising exact/HNSW search, learning and reopening. The dedicated workflow builds and verifies Linux x64, Windows x64 and macOS ARM64 packages before assembling the complete native asset set. Performance crossover points have not been benchmarked.
