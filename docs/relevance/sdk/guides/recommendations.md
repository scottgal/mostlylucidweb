# Observations and recommendations

All engine operations use a bounded `RelevanceWorker`. Standalone engines/hubs start owned workers; host registration starts its worker with the host. Await `SaveAsync` for committed visibility. Queue admission is not a durable acknowledgement. Cancellation can stop uncommitted work but cannot undo a commit; retry uncertain operations using the same stable event ID.

## Input routes

| Route kind | Conventional fields | Behaviour |
|---|---|---|
| candidate | id plus point/filter properties | Atomic upsert; candidates do not expire |
| observation | id, subject, type, optional candidate/at/state | Deduplicate, update affinity and configured transition/trajectory evidence |
| feedback | id, recommendation, candidate, reward | Attribute one observed outcome to a retained recommendation item |

Use `RelevanceItem`, `RelevanceObservation` and `RelevanceFeedback` for the conventional AOT-safe shape. `RelevanceObservation.Vector` supports input-sourced precomputed measurements. `State` supplies a transition state independent of event type; `BaselineGroup` identifies a qualified normal population. Omitted `At` uses processing time; supplied timestamps must be within retention, not future, and ordered per subject. Time-based trajectories require increasing timestamps.

Raw `JsonElement` and custom types with `JsonTypeInfo<T>` preserve existing application shapes. The unconstrained generic overload is a JIT compatibility convenience and deliberately has trimming/AOT annotations. A source name selects configured mapping; it is not a subject ID.

## Query operations

- `RecommendAsync(subject, placement, filter, count, token)` returns a retained `RecommendationResult` for later feedback attribution. Returning an item does not establish exposure.
- `RankAsync(...)` returns the ranked items without storing a core attribution decision. Use when another ledger, such as intervention learning, owns attribution.
- `RelatedAsync(candidate, point, filter, count, token)` searches one compatible representation, excludes the seed and returns neighbours with distances. Similarity is not causal evidence.
- `PredictAsync(subject, token)` is separate outcome inference and requires configured destinations.

Count must be 1 through `CandidateLimit`. Head retrieval is bounded; fusion is over that retrieved pool. Reasons expose head, one-based rank, current weight and distance. Empty/expired affinity falls back to eligible ordinal IDs with zero scores and no personalised reasons, not popularity.

## Filters

`Filter.Eq` accepts scalar values; `Lte/Gte` use finite numeric bounds. `Filter.All` composes conjunctions. Matching is case-sensitive and type-preserving. Strings are not coerced into numbers or booleans. Mandatory placement rules combine with caller constraints using AND. Equality on an absent property does not match.

Filters are snapshotted before admission and limited to 64 nodes/16 nested levels. Metadata paths must be declared. Values and paths are parameterised by the SQLite compiler. Native HNSW does not weaken this contract: constrained queries currently use exact SQL.

## Feedback

```csharp
await engine.SaveAsync("outcomes",
    new RelevanceFeedback("checkout-42", result.Id, result.Items[0].Id, 1.0));
```

Reward must be an observed completed value in [0,1]. The decision must remain retained and the candidate must have been returned. One final result/candidate outcome is accepted, including across different transport IDs; conflicting reuse is rejected. Missing feedback is not zero reward. The host defines actual exposure, objective and attribution window.

`SaveReceipt.Duplicate` marks an identical accepted event retry. Candidate upserts are not duplicate events. Feedback updates a bounded per-placement head-reliability heuristic; it is not a causal estimator or calibrated probability.
