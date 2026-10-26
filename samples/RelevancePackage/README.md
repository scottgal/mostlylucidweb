# A small framework consumer

The sample starts with the shipped `RelevanceDefinition.ForVectors` baseline; it needs no custom converter, store or learner implementation. It exercises one API across product, email and operational-action domains. Domains reuse the machinery, not each other's incompatible feature spaces. A two-file shard factory routes each whole tenant/space with rendezvous hashing and checks a persisted topology manifest. It also exercises outcome learning and database reopen.

The sample disables reflection-based JSON serialization and treats compiler warnings as errors. Its inputs use a source-generated JSON context. The core, SQLite and optional learning packages are marked AOT-compatible; the native executable is the verification, not the annotation alone.

## Run and publish

```sh
dotnet run --project samples/RelevancePackage -c Release -- /tmp/relevance-example
# After building or installing the native-assets package:
dotnet run --project samples/RelevancePackage -c Release -- /tmp/relevance-hnsw bundled

dotnet publish samples/RelevancePackage -c Release -r osx-arm64 -o /tmp/relevance-aot
/tmp/relevance-aot/RelevancePackage /tmp/relevance-aot-data bundled
```

A second argument may instead be an operator-selected absolute path to sqlite-vector-rs 0.3.1. No native extension is required for exact mode. Use a fresh directory for a fresh intervention experiment; the stable assignment IDs deliberately reject conflicting replay.

## Test the actual packages

Build a native runtime with `scripts/relevance/build-sqlite-vector.sh <rid>`. Pack core, SQLite, Learning and Mostlylucid.Sqlite.Vector into a local feed. A host-only test pack explicitly uses `-p:AllowPartialNativePackage=true`; a complete package requires every declared RID.

Generate sources with `python scripts/relevance/write-nuget-config.py <local-feed> <config-path>`, restore with `dotnet restore samples/RelevancePackage -r <rid> -p:UsePackedRelevance=true --configfile <config-path>`, then publish with the same RID/property and `--no-restore`. The configuration file avoids URL/path rewriting in Windows shells. Run both modes from the published directory. Do not manually copy the extension: the point is to test the NuGet runtime asset selection. The workflow does this independently on linux-x64, osx-arm64 and win-x64 before assembling a complete native-assets package. Those jobs do not publish to NuGet.org.

## Trust boundary

Tenant identities are resolved and authorised by the calling application. Definitions, converters, provider factories, paths and native libraries are trusted startup configuration. Each opened engine is owned by the hub. Reuse the hub and avoid competing processes opening the same native vector tables. Database backup must include committed WAL state through SQLite's backup API.

See the shipped [agent guide](../../Mostlylucid.Relevance/AGENTS.md) for modification recipes and invariants. Package status is local preview; no public NuGet release is claimed.

## Get and use a preview

Download `relevance-nuget-packages` from a successful **Relevance packages and Native AOT** workflow run. Extract the artifact into a local feed directory. It includes all four packages, a `NuGet.Config` pointing at that directory and NuGet.org, and the agent guide. Restore a consuming application with `--configfile <feed-directory>/NuGet.Config`. Public NuGet.org publication is still pending; the preview feed is usable as supplied.

Customisation is optional. The vector baseline works directly; application-specific destination states and intervention objectives are configuration, not missing inference implementations.
