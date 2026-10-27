# Storage, SQLite HNSW and sharding

`IRelevanceStore.TransactAsync` runs a synchronous isolated callback and commits atomically. Do not await, re-enter the store or retain the transaction. Return detached data. The engine owns its store after successful construction. Providers roll back all changes on failure; cancellation before commit can abort, but cannot undo a successful commit.

The built-in `MemoryRelevanceStore` uses detached JSON and copy-on-write transactions for development. It is not durable. `SqliteRelevanceStore` uses ordinary candidate/vector/record tables, WAL/FULL durability, foreign keys, a busy timeout and synchronous SQLite calls behind the bounded engine worker. Parent directories must exist for direct stores; `SqliteRelevanceFactory` creates its root.

## Search policies

| Configuration | Retrieval |
|---|---|
| Default SQLite | Exact registered distance function over eligible float32 BLOBs |
| Operator-installed sqlite-vec via vectorExtensionPath | Native scalar exact distance over ordinary tables |
| SqliteVectorOptions | SQLite-owned HNSW for unfiltered queries; exact SQL for constrained queries |

The sqlite-vec path is not an HNSW index or vec0 virtual-table adapter. Explicit extension failures propagate; there is no silent download/fallback. HNSW is opt-in and approximate; compare recall against exact results for the same metric, representation and filter. The adapter returns L2 distances in core units even though the native backend's L2 is squared.

```csharp
using Mostlylucid.Sqlite.Vector;
var vectors = new SqliteVectorOptions { ExtensionPath = SqliteVectorExtension.Path };
await using var hub = new RelevanceHub(new SqliteRelevanceFactory("./data", vectors: vectors));
```

The separate native package supplies sqlite-vector-rs 0.3.1. Its source/locked dependencies and documented Windows compatibility changes are pinned. Supported native asset RIDs are linux-x64, osx-arm64 and win-x64; the helper rejects others explicitly. Exact SQLite is independent of this extra extension. AOT deployment still includes native libraries.

`SqliteVectorOptions`: required `ExtensionPath`; `Connections=16` (2–128); `ConstructionWidth=200` (at least Connections, at most 10,000); `SearchWidth=100` (1–10,000). Higher widths/connections consume work or memory; no universal latency/recall guarantee is made.

The extension owns virtual/shadow graph tables within SQLite. The source rows remain transactional. Graph persistence is lazy and reconciles on reconnect. It does not refresh live between independent connections. Per-space leases reject competing cooperating .NET owners; independent SQLite clients must honour the same single-owner contract. Use one live hub for a set of spaces. Back up through SQLite's backup API so committed WAL state is included.

## Spaces and shard topology

`RelevanceSpace(Tenant, Name)` identifies an isolated catalogue, state and inference space. Each component is nonempty and at most 128 characters. Structured identities prevent separator collisions. Authorisation belongs to the caller; never use an untrusted request's tenant string as proof of access.

`RelevanceHub(factory, maxSpaces: 128)` reuses compatible opened engines and owns their lifetimes. The factory remains caller-owned. Converters are fixed by the first open; later incompatible registration fails. `ShardFor` uses deterministic rendezvous hashing over a snapshotted, ordered set of unique shard names.

`SqliteRelevanceFactory(directory, shards?, vectors?)` defaults to one shard `main`. Names are unique, 1–64 ASCII letters/digits/underscore/hyphen. Each complete tenant/space routes to one `.db` file. Candidates, state, outcomes and attribution remain in that local transaction. This does not split a hot catalogue or implement distributed top-K/cross-shard atomicity.

The persisted topology manifest rejects silent shard-list changes and a missing manifest beside existing databases. Reshard explicitly into a new directory and migrate under application control. Do not just add a shard name and expect data to move.

## Custom provider records

Built-in persistence already has generated metadata. A custom predictor/provider can store additional application records by constructing the store with its `recordTypes` JSON context. Unknown record types fail explicitly instead of reflecting. See [extensions](extensions.md) and [AOT](aot.md). Keep generic get/put semantics compatible with detached records, scope/kind/ID keys, expiry, schema binding and deduplication.
