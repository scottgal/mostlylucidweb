# Mostlylucid.Relevance.Sqlite

Durable SQLite provider for [Mostlylucid.Relevance](../Mostlylucid.Relevance/README.md), with one setup call:

```csharp
await using var engine = await SqliteRelevance.OpenAsync("relevance.yaml");
```

The database defaults to the YAML file's path with a `.db` extension. Supply a second path to choose another location. For an existing .NET host, call `services.AddRelevanceSqlite("relevance.yaml")` and inject `RelevanceEngine`.

The provider stores candidates, separate versioned vector points, aggregate state, transition counts, decision attribution and deduplication records. Save/update operations are atomic. WAL, FULL synchronous mode, foreign keys and a busy timeout are configured. All operations within an engine flow through the bounded hosted-service channel. SQLite calls themselves use the synchronous API because Microsoft.Data.Sqlite does not implement asynchronous SQLite I/O.

## Vector search modes

The default has no additional native-extension setup: vectors are float32 BLOBs in normal tables and an engine-registered exact distance function evaluates them. This is a correctness-first, no-extension baseline, not a large-scale ANN index.

To use an installed, pinned sqlite-vec extension:

```csharp
await using var engine = await SqliteRelevance.OpenAsync(
    "relevance.yaml", vectorExtensionPath: "/installed/extensions/vec0.dylib");
```

The extension is loaded with its explicit entry point and checked with `vec_version()`. Failure is surfaced rather than silently changing modes. Both modes enforce eligibility in the same SQL query **before ORDER BY/LIMIT**. Native search currently uses scalar distance functions over ordinary BLOB columns, retaining full supported filter semantics; `vec0` virtual-table acceleration is not implemented in this preview.

`SqliteRelevanceStore.SearchMode` identifies the implementation. The native v0.1.9 release was smoke-tested on macOS ARM64; Windows and Linux remain subject to their native package CI checks; the later section describes the now-verified local AOT path. Native binaries are not bundled or downloaded at runtime. A managed-only installation still uses the SQLite native runtime supplied by Microsoft.Data.Sqlite.

## Portable query intent

`Filter` is a small storage-independent tree. `SqliteFilterCompiler` converts the current subset to parameterised SQLite SQL over declared JSON properties. It does not accept arbitrary SQL or compile the entire inference engine into SQL. PostgreSQL/SQL Server providers will need their own dialect/compiler and equivalent filter semantics.

A future sidecar can host the same engine and reuse those query contracts. HTTP hosting and other SQL providers are planned; there is no remote service in this preview.

## Verification

Run all tests including native distance parity:

```sh
dotnet test Mostlylucid.Relevance.Tests/Mostlylucid.Relevance.Tests.csproj \
  --environment RELEVANCE_SQLITE_VEC=/installed/extensions/vec0.dylib
```

Without that variable, the explicit native test is skipped; managed SQLite and memory-provider tests still run. Performance crossover points have not been benchmarked. See the [research and release gates](../docs/relevance/part2-implementation-plan.md).

## SQLite-owned HNSW and named shards

The optional [Mostlylucid.Sqlite.Vector](../Mostlylucid.Sqlite.Vector/README.md) package carries the pinned sqlite-vector-rs extension and AOT-compatible asset discovery:

```csharp
var vectors = new SqliteVectorOptions { ExtensionPath = SqliteVectorExtension.Path };
await using var hub = new RelevanceHub(new SqliteRelevanceFactory("./data", ["east", "west"], vectors));
var products = await hub.OpenAsync(new RelevanceSpace(authorisedTenant, "products"), definition);
```

Each named point/signature/space gets a SQLite virtual table. The extension owns its HNSW graph and persists it in shadow tables inside SQLite. Upserts and rollback are checked against the ordinary authoritative rows. No managed HNSW library or separate index file/service is used. A held per-space file lease rejects competing .NET provider owners of the same native space. Independent SQLite clients do not participate in that lease. Connections must not compete over the same vector table: use one live hub per set of spaces. Existing native graphs are refreshed on reopen, not live between independent connections.

Unfiltered retrieval can use HNSW. Queries with eligibility constraints use the existing exact SQL path, including mandatory placement restrictions. This preserves correctness while keeping the first adapter small. `SearchMode` reports the configured policy; it does not claim every query used ANN. Distances are returned in core units, including the square root of squared-L2 backend distances. Approximate candidate discovery may miss true neighbours; validate recall against exact search.

`SqliteRelevanceFactory` persists a topology manifest and refuses changes to shard names that would silently reroute data. Shards partition complete tenant/spaces. One large catalogue is not distributed across files, and no cross-shard transaction or distributed top-K is claimed. The provider factory remains replaceable by a PostgreSQL implementation.

Native AOT has now been published and executed locally on macOS ARM64 with both exact and SQLite-owned HNSW search, plus learning and reopen checks. The dedicated package workflow verifies host packages on Linux x64, Windows x64 and macOS ARM64 before assembling release assets. Native binaries are supplied through NuGet; no runtime download or consumer-side Rust toolchain is required. These packages remain unpublished previews.
