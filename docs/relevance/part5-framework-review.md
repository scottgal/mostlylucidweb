# Part 5 API and provider review

Reviewed the embedded package API, not an HTTP authentication layer. Host configuration, converters, custom predictor implementations, database directories and native libraries are trusted. The application must authorise the tenant before opening its space; metadata returned in recommendations must be appropriate for the calling application.

## Implemented boundaries

- ForVectors supplies a usable vector baseline without custom components. Existing Save/Recommend/Predict operations are retained. RelatedAsync adds one named-space neighbour query; RelevanceHub and RelevanceSpace add multi-domain ownership rather than a second recommendation API.
- Constructor snapshots and generated JSON metadata preserve representation contracts. Internal serializers have no reflection fallback or warning suppression. YAML and reflection-based generic input have explicit AOT annotations; JSON/typed metadata paths are verified natively.
- Tenant/space keys are structured, hashed compound identities. Same candidate/subject IDs in separate spaces stay isolated. Shard paths use validated identifiers. Topology changes fail instead of silently remapping data.
- Payloads, IDs, filter depth/node count, queue admission, open-space count and retained records have bounds. A rejected operation does not terminate the worker.
- SQL values and JSON paths are parameters; native table identifiers are hashes and index settings are validated integers. No request SQL, native-path download, assembly scanning or arbitrary formula execution is introduced.
- Mandatory eligibility is ANDed with caller constraints. The native adapter uses exact SQL when constrained, rather than post-filtering a truncated ANN list.
- SQLite HNSW remains inside native virtual/shadow tables. Returned distances use core units. New-table creation, replacement and rollback are tested. Scope ownership leases reject competing cooperating provider instances because the extension does not refresh graphs across live independent connections.

## Practical limits

- Native ownership leases do not coordinate arbitrary external SQLite clients. Use a trusted data directory and one owner of each native space.
- The SQLite factory partitions whole tenant/spaces; no hot-catalog fan-out, automatic migration or cross-shard atomic transaction is implemented.
- sqlite-vector-rs is a pinned, young opt-in backend. Default exact SQL remains independent. Native RAM/latency is not benchmarked here; binary sizes are not RAM measurements.
- JSON/typed configuration is the supported AOT route. Generated custom provider metadata is required explicitly.

## Local evidence

97 tests passed with both RELEVANCE_SQLITE_VEC and RELEVANCE_SQLITE_HNSW set. Added cases cover multi-space persistence/isolation, topology, related filtering, HNSW replacement/rollback/new-table rollback, ownership conflicts, recall against exact distances, excessive input/filters, mandatory-rule conjunction and SQL-shaped values. The macOS ARM64 Native AOT consumer runs both from project references and actual local NuGet packages, including native asset discovery, learning and reopen. The workflow verifies three host RIDs before assembling a full native package; those other-platform results are not claimed as already run locally.
