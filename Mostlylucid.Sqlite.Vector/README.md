# Mostlylucid.Sqlite.Vector

A separate, small native-assets package for **SQLite-owned HNSW search**, built from unchanged sqlite-vector-rs 0.3.1 at `7ee2baf7b2783c1798f285cdff3b2285da38de04`. No search server, .NET HNSW index, runtime download or consumer-side Rust build.

```csharp
using Mostlylucid.Sqlite.Vector;

connection.LoadExtension(SqliteVectorExtension.Path, SqliteVectorExtension.EntryPoint);
```

With Mostlylucid.Relevance.Sqlite:

```csharp
var vectors = new SqliteVectorOptions { ExtensionPath = SqliteVectorExtension.Path };
await using var hub = new RelevanceHub(new SqliteRelevanceFactory("./data", vectors: vectors));
```

The helper uses fixed RID/name mappings and checks the native asset copied by NuGet. It uses no reflection or runtime code generation. A Native AOT app still carries platform-specific native libraries. Initial package targets are **linux-x64, osx-arm64 and win-x64**; unsupported platforms fail explicitly. Local verification currently covers macOS ARM64; the workflow builds and verifies the other targets before assembling a full package.

The upstream extension stores its HNSW graph in SQLite shadow tables. Its row data is transactional and graph persistence is lazy, with reconciliation on reconnect. Use **one live writer/connection per vector table**; other connections do not get a live graph refresh. Relevance's hub reuses one engine per tenant/space. Do not open the same space in competing hubs/processes. Back up through SQLite's backup API, not by copying an active WAL database file.

This is an opt-in young extension, not a performance claim over exact search. Relevance uses exact SQL for constrained queries so mandatory filters cannot be lost to ANN truncation. Build quality, search width, recall, memory and workload all matter.

Build the host asset with `scripts/relevance/build-sqlite-vector.sh <rid>`. Pack with `dotnet pack Mostlylucid.Sqlite.Vector -c Release`; a release pack requires all three assets. `-p:AllowPartialNativePackage=true` is only for local host verification. The package has not been published.
