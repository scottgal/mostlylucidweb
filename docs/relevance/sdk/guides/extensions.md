# Extending the SDK

The supplied implementations are working defaults. Use configuration first: routes, fields, weights, vocabulary, formulas, heads, eligibility, outcomes, time scales and reward objectives. Implement an interface only when the application needs a new representation, inference method or storage backend.

## IPointConverter

Provide a unique case-sensitive `Name`, stable `Version`, `Dimensions(PointDefinition)` and asynchronous `ConvertAsync`. The engine passes the configured property's JSON value, not the entire envelope unless that property is structured. Validate settings in Dimensions. Return a newly owned finite array of exactly the declared length; cosine requires nonzero vectors.

Register an instance in the engine/hub constructor or DI. Names cannot shadow built-ins. The host owns converter resources. Shared instances must support concurrent calls from different engines; each engine worker serialises only its own operations. Treat the supplied frozen settings as read-only. Version model/tokeniser/pooling/preprocessing/units together, even when dimensions remain unchanged.

A converter can call an application-owned model asynchronously. The library does not download one implicitly. Keep input secrets, model credentials and outbound service policy in the application.

## IDestinationPredictor

Implement `PredictAsync(PredictionContext, token)`. The context contains detached subject state and transition/model evidence, configured destinations and the request time. The call runs outside the storage transaction but on the engine worker. Do not call back into that engine and await its queue.

Preserve the stated horizon, supported observations, unresolved mass and staleness behaviour. A result should identify insufficient evidence. Do not label unvalidated estimates calibrated. Use a new model/configuration version when semantics change and evaluate against later observed episodes.

## IRelevanceStore and IRelevanceTransaction

`TransactAsync` admits one isolated synchronous callback and commits atomically. Failure rolls back every change. An accepted update's receipt follows commit. The callback must not await, retain provider state or re-enter the store. The store is owned by its engine; factory resources remain caller-owned.

| Transaction member | Required contract |
|---|---|
| BindSchema | Bind scope to fingerprint; incompatible reuse fails |
| Accept | Same ID/hash returns false; conflicting reuse throws |
| Get/Put | Detached typed records by scope/kind/ID, with absolute expiry |
| Count | Scope/kind capacity counts, including unpruned records |
| Upsert | Replace candidate and all named vectors atomically |
| Nearest | Compatible signature/metric, full eligibility, ordered returned distances and ordinal tie order |
| Eligible | Matching candidates in ordinal ID order before limit |
| Prune | Expiring records at/before now; retain candidates/schema bindings |

Approximate retrieval may trade recall for latency, but must not lose mandatory eligibility. Scalar comparisons preserve JSON type and case-sensitive path/string semantics. A PostgreSQL provider needs equivalent filtering, transaction, deduplication and schema behaviour, not SQLite SQL copied into another dialect.

Use generated metadata for custom stored records. The built-in stores accept a `JsonSerializerContext` via `recordTypes`. Public core models are already supported; the learning layer serializes its own private records. Test custom records through a native consumer as well as the JIT test runner.

## IRelevanceStoreFactory

Supply immutable, unique shard names and return a new exclusive store lifetime from `Open(shard)`. The hub snapshots names, routes whole spaces and owns each returned store through its engine. Preserve stable routing and explicit migration. Do not share one disposable store instance across independent owning engines.

The shipped `AGENTS.md` maps application problems to these boundaries and identifies invariants for a coding assistant. It is guidance for changing a working framework, not an automatic plugin loader or permission to bypass the host's policies.
