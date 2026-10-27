# Operations, ownership and limits

## Ownership

| Resource | Owner |
|---|---|
| Engines returned by RelevanceHub | Hub; callers borrow them |
| Store passed to successfully constructed engine | Engine |
| Standalone worker created by engine | Engine |
| Externally supplied worker | Host/caller; drain before disposing dependent engine |
| Converter/predictor instances | Host/caller |
| InterventionLearning store and worker | Learner |
| RelevanceEngine passed to learner | Caller/hub; dispose learner first |
| Factory passed to hub | Caller |

Stop application submissions before disposal. Owned workers stop admission and drain accepted work. A cancelled shutdown deadline stops waiting but does not make unfinished work safe to dispose. Abrupt termination may lose unacknowledged work; retry stable IDs. Do not interpret queue entry as durability.

## Input and state limits

JSON ingestion requires an object, at most 1 MiB UTF-8. Envelope IDs and recommendation subjects allow 1–256 characters. Tenant/space components allow 1–128. Filters allow 64 nodes and 16 nested levels. Point paths allow 128 characters and the declared dotted-property grammar. Vector dimensions are 1–65,536 and coordinates must be finite; the temporal estimator caps combined coordinates at 64.

Queues, open spaces and retained state have explicit capacities. Hitting capacity rejects/rolls back the operation rather than silently expanding a model. Defaults and configuration are in [configuration](configuration.md) and [learning](learning.md). The application still needs admission/rate limits for its own callers and must await backpressure instead of launching unbounded tasks.

## Trust boundary

Authenticate and authorise tenants in the host. Space strings do not grant access. Definitions, converter/predictor implementations, native extension paths, shard topology and database directories are trusted startup decisions, not arbitrary request parameters. Native library loading executes trusted native code. Protect the data directory and packaged libraries with normal application permissions.

Parameterised filters are not a replacement for authorisation. Preserve mandatory placement rules. Return application response DTOs where the raw stored metadata is too broad. Keep credentials/secrets out of candidates; results include candidate data.

## Expected failures

Invalid mappings, unknown routes/types, missing properties, incompatible dimensions or non-finite coordinates fail with argument/JSON errors. YAML parse errors may be YamlDotNet exceptions; unknown members are JSON errors after the bounded YAML conversion. Schema reuse, capacities, expired attribution and competing native ownership fail explicitly. Native library/platform errors are not silently downgraded to another mode.

Cancellation before commit can abort. After commit, retry identity determines whether an operation is already accepted. Out-of-order observations are rejected; temporal mode requires strict time order. Separate incidents/journeys after absorbing endpoints.

## Evidence and monitoring

Inspect recommendation reasons, prediction status/support/window and learner snapshots/journeys. Keep ranking scores, semantic classifier readings, expected reward, assignment propensity and observed outcomes distinct. Neither geometry nor standard error is universal calibrated confidence.

Model cohorts and transition counts are cumulative while retained; normal references and affinity have their declared decay. Drift adaptation is not automatic everywhere: a cumulative reward model may need an explicitly designed forgetting/window strategy. Changing the model requires versioning and evaluation rather than silently reinterpreting old coordinates.

Back up SQLite using its backup API, including WAL state. Restore under the same schema/routing contract; use explicit migration for changed representations or shard topology. The native graph is derived and reconciled against transactional rows, but independent live owners are unsupported.
