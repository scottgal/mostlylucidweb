# Relevance: Part 2 implementation plan

Research date: 2026-10-23. Initial implementation: .NET 10, local preview packages, no remote dependency.

Series progression: **Understand the data → Understand the behaviour → Change the outcome.** Part 1 establishes representation and relevance; Part 2 builds the statistical engine and two-domain demo; [Part 3](../../Mostlylucid/Markdown/show-me-what-you-got-part3-predict.md) demonstrates online destination learning with a Bogus fixture and held-out forecasts. [Part 4](part4-intervention-learning.md) introduces controlled experimentation. [Part 5](part5-richer-representations.md) adds images and richer representations. The [optional embedding research](optional-embeddings.md) recommends a small ONNX text baseline and qualification of EmbeddingGemma 2/GGUF for the multimodal path.

## Demonstration and public surface

Part 2 should demonstrate representation, adaptation, and inference separately. Ecommerce and diagnostic telemetry use the same engine. Converters produce coordinates; statistical updates retain experience; a separate predictor relates observed state transitions to destinations. This is learning without requiring a separately trained neural predictor. It can still reasonably be described as online statistical learning.

Normal integration is one setup call and `SaveAsync` / `RecommendAsync`. `PredictAsync` is optional when destinations are defined. Feedback is an input stream rather than another event-ingestion API. Existing object shapes can be mapped in YAML.

The implemented baseline includes async converter plugins, scalar formulas, SQLite/memory providers, a bounded channel and hosted service, transactional idempotency, decaying profiles, pre-filtered retrieval, placement-specific weighted RRF, explicit outcome feedback, and finite-horizon Markov destination estimation. See the core README for precise semantics and limits.

The interactive companion is `Mostlylucid.Relevance.Demo`: a generated shop plus a structured-log monitor and reversible local rate-limit handler. Its before/after intervention notebook is observational. The planned `Mostlylucid.Relevance.Learning` project owns action-selection propensities, controlled comparisons and intervention-effect evaluation.

The optional `samples/RelevanceMail` bridge demonstrates a third domain using the real StyloMail.Core question/evidence types. Ordered question rubrics, producer, preprocessing and tenant identity define its representation. Complete semantic assessments rank host-owned review playbooks behind an existing policy action; incomplete, partial or stale assessments abstain. Each immutable assessment gets its own subject, with no automatic update to trusted sender baselines. The component is not installed in StyloMail's live host. Its separate test project requires the StyloMail checkout; it is intentionally outside the general solution.

## SQLite research and initial decision

| Option | Evidence relevant to this workload | Decision |
|--------|------------------------------------|----------|
| `sqlite-vec` | Small dependency-free C extension; float/int8/binary vectors; scalar functions and `vec0` tables. Still pre-v1. Latest stable release checked: v0.1.9. | Initial native backend to qualify. Use scalar exact search over ordinary BLOB columns first. |
| SQLite AI `sqlite-vector` | Ordinary BLOB columns, SIMD exact search and quantised scans; latest release checked: 1.1.2. Quantisation refresh adds lifecycle work for changing data. | Benchmark as an alternative, not a claimed performance loser. |
| libSQL/Turso | Native vector functions and ANN options, but changes the SQLite engine/driver choice. Upstream distinguishes maintained libSQL from the newer Turso engine. | Possible later provider, not the default embedded dependency. |
| `sqlite-vss` | Upstream says active development moved to `sqlite-vec`. | Exclude from a new integration. |

Primary sources: [sqlite-vec README](https://github.com/asg017/sqlite-vec), [v0.1.9 release](https://github.com/asg017/sqlite-vec/releases/tag/v0.1.9), [SQLite AI API](https://github.com/sqliteai/sqlite-vector/blob/main/API.md), [1.1.2 release](https://github.com/sqliteai/sqlite-vector/releases/tag/1.1.2), [libSQL project direction](https://github.com/tursodatabase/libsql), [sqlite-vss maintenance notice](https://github.com/asg017/sqlite-vss).

This is a provisional fit decision, not a benchmark result. Native v0.1.9 was loaded and tested on macOS ARM64, including distance parity with the managed baseline and filters before top-K. Its release archive SHA-256 was verified. Native binaries remain outside the repository. Cross-platform packaging and scale benchmarks are release gates.

The default installation currently uses an exact managed distance function registered on the SQLite connection. An explicit extension path switches to native scalar distances. Both preserve filter semantics over ordinary tables. Native loading must fail visibly if explicitly requested; no runtime downloads.

The official .NET vector connector is worth watching, but its documented filter subset is currently equality-only, so it does not own the public filtering API. [Microsoft connector documentation](https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/sqlite-connector)

`vec0` has metadata/partition features, but the documented metadata operators are restricted and auxiliary fields cannot be KNN filters. An arbitrary filter applied after top-K can lose the correct eligible result. Retain exact SQL scoring as a correctness fallback, even if a future provider adds an accelerated virtual-table path. [vec0 metadata and partition documentation](https://alexgarcia.xyz/sqlite-vec/features/vec0.html), [scalar KNN functions](https://alexgarcia.xyz/sqlite-vec/features/knn.html)

## Data and lifecycle

Use ordinary candidate rows plus one versioned vector per candidate/point. Other records store subject sums/mass, transition counts, head feedback, decision attribution, and deduplication tokens. All updates for an event commit atomically. Provider transactions must roll back deduplication when an update fails.

A bounded `Channel` feeds one `BackgroundService`; callers await actual update completion. Stop admission and drain on graceful shutdown. No manual flush operation, unbounded detached tasks, or polling for ingestion. The volatile queue is not a durable broker; stable IDs permit retry of unacknowledged events.

SQLite operations use synchronous calls inside that worker. The provider serialises its transactions, uses WAL/FULL durability and a busy timeout. This keeps the first consistency model simple; it does not promise multi-writer scalability or network-filesystem support. [Microsoft async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async), [SQLite WAL concurrency and durability](https://sqlite.org/wal.html)

Persist version fingerprints, enforce dimensions/finite values, and reject mixed representations. In this preview, changing configuration for an existing engine ID requires replay under a new ID or an explicit migration. Hot configuration migration is subsequent work. Retention and cardinality limits are enforced; raw observation bodies are not retained as an event log.

## Retrieval, adaptation and prediction

Placements define eligible surfaces/areas; heads define independent retrieval rankings. A category restriction is a filter, not a neural head. Use weighted RRF within the placement. Per-head candidate windows are bounded; completeness is relative to that pool. The initial feedback rule is a bounded reliability heuristic with inverse-rank credit, not causal attribution or an unbiased bandit policy. [Original RRF paper](https://cormack.uwaterloo.ca/cormacksigir09-rrf.pdf)

Destination prediction uses observed event-state transitions and explicit absorbing outcomes. Return the probability of hitting each outcome within N events, unresolved mass, source-row support and the state observation time. Sparse rows abstain. Never rename RRF scores or cosine distances as probabilities. Counts are pooled by engine ID and cumulative while the model record stays active, not a sliding training window. Account for trajectory dependence, censoring and selection bias before interpreting support as confidence. [Markov-process teaching material](https://ocw.mit.edu/courses/6-262-discrete-stochastic-processes-spring-2011/resources/mit6_262s11_chap06/)

Position, velocity, acceleration, variance and historical outcome-conditioned trajectories belong in the next statistical extension. Derivatives need elapsed-time units, monotonic timestamps, missing-value rules, stable representation versions and zero-time handling. Movement alone does not establish a calibrated destination probability. Compare richer inference strategies against the transition baseline using time-ordered splits, Brier/log loss and reliability plots.

Colour conversion is a concrete non-neural representation plugin: measured sRGB swatches map to Oklab, with approximate perceptual distance under the stated colour model. A colour name is not a measured swatch. [Oklab derivation and reference transform](https://bottosson.github.io/posts/oklab/)

## Delivery sequence

1. **Working baseline:** local packages, converter registry/formulas, storage contracts, channel worker, filtered RRF and destination estimates. Provider/lifecycle/converter/probability tests. Two-domain console sample.
2. **Interactive companion demo:** reuse the existing audience demo's product model, generate products, capture shopping events, show recommendation reasons and destination estimates. Separately monitor structured request logs, inject labelled synthetic incidents, and show diagnostic candidates. A reversible, explicit handler may change only this demo's rate limit.
3. **Statistical trajectories:** per-point motion/variance, outcome-conditioned reference populations, multiple interest centroids and explicit probability evaluation. No required neural model.
4. **Emergent structure:** bounded neighbour graph, Leiden communities, representative summaries and optional small-model descriptions linked to cluster versions/evidence. Keep community discovery and description work off the immediate ingestion acknowledgement path.
5. **Packaging and extensions:** supported-RID native artifacts, scale benchmarks, additional SQL dialects, optional vector-store adapters, then thin HTTP sidecar hosting over the same engine. A sidecar needs normal host authentication/isolation; arbitrary client YAML/SQL is not an execution API.

## Regular SQL and sidecar interpretation

Portable filters describe intent; the implemented SQLite compiler parameterises property paths and values. Other SQL providers need their own dialect/compiler, transaction and distance strategy. Ordinary SQL can select eligible candidates; similarity needs native database support or engine computation. There is no claim that one SQL string implements every provider's vector search or adaptive model.

A sidecar wraps the same save/recommend/predict contracts. Store and host are independent extension points. Configuration selects registered converters; it cannot load arbitrary code or assemblies. Advanced application code can register converters and inference strategies through DI.

## Validation and release gates

- Provider parity: eligibility before truncation, ordering, transaction rollback, idempotency, scope isolation, persistence, expiry and explicit schema rejection.
- Worker lifecycle: backpressure, asynchronous conversion, read-after-awaited-save visibility, failure isolation, cancellation and graceful drain.
- Formula grammar and range checks; colour transform reference values; unknown converter/vocabulary handling.
- Probabilities: known synthetic destination proportions, finite-horizon unresolved mass, abstention, deduplicated transition learning, and raw system-state ingestion.
- Native sqlite-vec on each supported RID, plus trimming/AOT qualification if claimed.
- Benchmarks at 1K/10K/100K candidates, 8/64/384-dimensional points, varying filter selectivity and update/query mixes. Measure p50/p95 latency, allocations, database/WAL size and event-to-read freshness. No advertised scale threshold before those runs.
- Browser and HTTP flows for shopping, filter changes, cart outcomes, incident injection, diagnostic explanations, action application and reset.

The prototype does not yet include automatic state discovery, Leiden, language-model cluster naming, probabilistic calibration, full event replay, a generic remote sidecar, or external system administration. These are explicit extension milestones rather than implied capabilities of a vector store.
