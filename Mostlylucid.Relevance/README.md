# Mostlylucid.Relevance

An in-process streaming relevance library. Save products or observations, get filtered recommendations, and optionally estimate destination probabilities. No neural network, remote service or model download is required for the statistical baseline.

This is an initial .NET 10 preview. Packages are buildable locally; they have not been published.

## Quick start

Reference `Mostlylucid.Relevance.Sqlite`, then provide a YAML definition like [the shop example](../samples/RelevanceShop/relevance.yaml).

```csharp
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;

await using var relevance = await SqliteRelevance.OpenAsync("relevance.yaml");

await relevance.SaveAsync("products", product);
await relevance.SaveAsync("events", new
{
    id = eventId, subject = sessionId, candidate = productId, type = "view"
});

var result = await relevance.RecommendAsync(sessionId, "home",
    Filter.All(Filter.Eq("category", "outdoors"), Filter.Lte("price", 120)));

var prediction = await relevance.PredictAsync(sessionId); // Requires configured destinations.
```

`SaveAsync` accepts existing objects. Its first argument chooses a configured input stream. Candidates are upserted; observations update live state; feedback updates retrieval-head reliability.

In an existing Generic Host or ASP.NET Core app, use `services.AddRelevanceSqlite("relevance.yaml")` and inject `RelevanceEngine`. The same `RelevanceWorker` is registered as the hosted service. The standalone factory starts and owns that worker for you.

## What happens in the background?

A bounded channel feeds one `BackgroundService`. Conversion, persistence, model updates, and reads run in submission order. A full channel applies asynchronous backpressure. An awaited save means conversion and commit completed; it does not merely mean the event entered a volatile queue. No flush method is needed before an awaited read. Individual failures go back to the caller without stopping the worker.

Graceful shutdown stops admission and drains accepted work. Abrupt process termination can lose unacknowledged work, so callers should retry using stable event IDs. Deduplication applies within the configured retention window. Older events outside retention and out-of-order events within a subject are rejected. When `at` is absent, processing time is used.

The first preview serialises reads and updates through the same worker for simple consistency. It is not designed as a high-throughput distributed queue.

## Three concerns

| Concern | Baseline implementation |
|---------|-------------------------|
| Representation | Named converters with explicit dimensions, semantics, distance and versions |
| Adaptation | Time-decaying weighted sums, evidence mass, transition counts and feedback |
| Inference | Filtered nearest neighbours, weighted RRF and finite-horizon Markov destination estimates |

An EWMA or centroid update tracks observations. Predictive capability comes from an explicit inference strategy supported by observed outcomes. Optional models should be measured against this baseline, not assumed to improve it.

## Converter plugins and formulas

Configure points, not database tables:

```yaml
points:
  price:
    path: price
    converter: formula
    expression: "clamp((x - 20) / 180, 0, 1)"
    metric: l2
    displays: Price scaled between the configured bounds.
```

Built-in converter names:

| Name | Input and output |
|------|------------------|
| `numeric` | Number divided by `scale`; one coordinate |
| `formula` | Number mapped by an expression over `x`; one coordinate |
| `boolean` | Boolean mapped to 0 or 1 |
| `categorical` | String encoded using an explicit ordered `categories` vocabulary |
| `vector` | Precomputed float coordinates with declared `dimensions` |
| `rgb-oklab` | An actual `#RRGGBB` sRGB swatch converted to three Oklab coordinates; requires `l2` |

Formulas accept numbers, `x`, parentheses, `+ - * /`, and `abs`, `min`, `max`, `clamp`. They have bounded length and nesting and reject division by zero and non-finite results. They cannot run C#, access files, or invoke arbitrary functions. Formula input is a single numeric property; use a C# converter for more complex structured input.

Implement `IPointConverter` to supply a stable name/version, output dimension, and `ConvertAsync`. Supply plugins to `OpenAsync(..., converters: [...])`, or register them with DI. The converter name is selected in YAML. Async conversion can call an application-owned encoder; no embedding provider is built into the core. See the [optional local embedding research](../docs/relevance/optional-embeddings.md) for the MiniLM/ONNX baseline and EmbeddingGemma 2/GGUF qualification plan, including the later image extension.

Raw behavioural observations use `point_source: input`. Product interactions use the default `point_source: candidate` and refer to an already saved product. Point paths refer to the incoming object or referenced product respectively.

## Existing event shapes

Map conventional envelope fields to existing property paths:

```yaml
streams:
  page_events:
    kind: observation
    fields:
      id: event_id
      subject: session.id
      candidate: product.sku
      type: action
      at: occurred_at
    weights: {opened: 1, added_to_basket: 3}
```

The core envelope uses `id`, `subject`, `candidate`, `type`, optional `at` and optional transition `state`. Candidate inputs use `id`; feedback inputs use `id`, `recommendation`, `candidate`, and numeric `reward` in `[0,1]`.

## Placements and rank fusion

A placement defines a surface such as home, an area of the shop, or diagnostic actions. Its `where` conditions are mandatory and combine with caller filters using AND. Declare allowed filter paths in `filters`. Preview filters support equality, numeric lower/upper bounds, and conjunction; values are parameterised in SQL.

Each head retrieves a ranked list using one configured point. RRF combines ranks rather than raw incomparable distances. Head windows are bounded by `candidate_limit`, so fusion is over that candidate pool, not a claim to rank the entire catalogue exhaustively. Results include per-head ranks, distances and weights; scores are not probabilities.

Cold-start or expired profiles return eligible items in deterministic ID order with score zero and no personalised reasons. This is a transparent fallback, not a popularity model. Upsert `available: false` and use a mandatory availability filter to retire products.

Submit explicit feedback through the same save API:

```csharp
await relevance.SaveAsync("outcomes", new
{
    id = feedbackId, recommendation = result.Id, candidate = selectedProductId, reward = 1.0
});
```

Only returned candidates can receive attributed feedback. Only one outcome per result/candidate is accepted, even with different transport IDs. Use a defined attribution horizon before reporting success/failure, rather than sending provisional failure and later success for the same exposure. A bounded, smoothed, placement-specific reliability heuristic allocates credit among supporting heads. It is not an unbiased causal estimate or calibrated probability. Record actual exposure in the calling application; returning a result does not establish that it was displayed.

## Destination probabilities

```yaml
destinations:
  states: [view, compare, cart, purchase, cancel]
  outcomes: [purchase, cancel]
  horizon_events: 8
  min_transitions: 10
```

Observations increment first-order transition counts, pooled within the configured engine ID. `state` defaults to event `type`; raw telemetry may supply an explicit known state. Destinations are absorbing for prediction. A new event after a destination starts a new path rather than learning an outgoing transition from that destination.

`PredictAsync` returns probabilities of reaching each destination **within the next N observed events**, supporting transition counts, current state/time, and unresolved mass. Missing/insufficient rows retain unknown mass rather than inventing confident estimates. Unfinished trajectories within the horizon also contribute to that unresolved mass. Do not renormalise it away. An already observed destination is reported as such, not as a forecast. `IsCalibrated` is false for the baseline; support counts are not a confidence interval. Sample dependence, stationarity, incomplete journeys, censoring and reporting bias still matter.

The default predictor uses transition frequencies, not centroid velocity or geometric distance. A registered `IDestinationPredictor` can use richer history or a separately evaluated model. Velocity/acceleration features, outcome-conditioned trajectory populations and calibration evaluation are subsequent work.

## Storage and versioning

`IRelevanceStore` supplies atomic transactions, records, filtered neighbours and bounded-retention cleanup. The core has no SQLite dependency. `MemoryRelevanceStore` is a reference/test provider; the SQLite package is the durable implementation. Custom providers must pass equivalent atomicity, filter and isolation tests.

Configuration, converter versions and the storage contract are fingerprinted. This preview rejects changes against an existing engine ID. Use a new engine ID and replay/re-encode, or implement an explicit migration; silently mixing coordinate systems is forbidden. The current storage contract uses structured deduplication identities to distinguish stream/event pairs even when names contain separators. Databases created by the earlier preview require a new engine ID or replay into a fresh database. To keep an earlier demo database intact, run with `Demo__DataPath` pointing to a new directory.

Filters must be present non-null scalars on candidates. Unknown converters, categories, stream types, transition states and filter fields fail closed. Scalar comparisons do not coerce types: a numeric bound on a text-valued field makes that candidate ineligible in both providers. Caller-owned filter lists are snapshotted before queue admission.

State, deduplication and decision records expire lazily during operations. Configured cardinality limits reject growth rather than silently evict active evidence. Candidate records persist until updated; catalogue capacity is explicit. Raw observation bodies are not retained as an event log, so use the host's source events for replay. This is neither an event broker nor a forever-growing analytics store.

## Run the two-domain demo

```sh
dotnet run --project samples/RelevanceShop/RelevanceShop.csproj
dotnet test Mostlylucid.Relevance.Tests/Mostlylucid.Relevance.Tests.csproj
```

The demo uses products and raw diagnostic telemetry through the same API. Diagnostic state labels and action prototypes are supplied explicitly; the preview does not automatically discover those labels or execute any action. The synthetic diagnostic destination probabilities are a mathematical demonstration, not measured production accuracy.

For question-defined semantic coordinates, the optional [StyloMail integration sample](../samples/RelevanceMail/README.md) consumes real `StyloMail.Core` evidence and ranks policy-eligible review playbooks. It requires a separate StyloMail checkout and has its own tests, keeping this solution independent of mail-specific dependencies. Incomplete evidence explicitly abstains; the bridge does not replace StyloMail's delivery policy or trusted profiles.

Implementation sequence and SQLite research: [Part 2 plan](../docs/relevance/part2-implementation-plan.md).

## Sample API documentation

Both library projects generate XML documentation for IntelliSense and package consumers. Missing documentation on a public member is a build error (`CS1591`). Entry-point comments explain ownership, completion/cancellation, score meaning and failure conditions; provider and converter contracts document the invariants an extension must preserve.


## Part 3: learn and predict from generated journeys

The console tool now has a reproducible `predict` command using Bogus products/users and explicit synthetic outcome rules:

```sh
dotnet run --project samples/RelevanceShop -- predict --output /tmp/relevance-predict-814
```

It starts fresh, learns from observations, predicts new users, reopens SQLite and evaluates against a frozen later population. See [the sample](../samples/RelevancePredict/README.md) for actual output, optional frozen LLM copy and the prefix/label boundary. The sample's small predictor adapters inspect or freeze the existing transition baseline; the core consumer API is unchanged.

[Part 3](../Mostlylucid/Markdown/show-me-what-you-got-part3-predict.md) establishes outcome forecasts. [Part 4](../docs/relevance/part4-intervention-learning.md) uses controlled interventions to learn what changes the result. Richer image/embedding ingestion follows in [Part 5](../docs/relevance/part5-richer-representations.md).

## Timestamped trajectories and normal references

Destination prediction also supports a physical-time estimator. Configure `destinations.trajectory` to make it the default strategy; omitting it keeps the event-count Markov baseline. The public calls stay `SaveAsync` and `PredictAsync`.

```yaml
destinations:
  states: [healthy, degraded, outage, recovered]
  outcomes: [outage, recovered]
  trajectory:
    points: [behaviour]
    horizon_seconds: 60
    half_life_seconds: 2
    velocity_scale: 0.01
    acceleration_scale: 0.002
    elapsed_scale_seconds: 60
    baseline_states: [healthy]
    baseline_half_life_seconds: 86400
```

The selected points must be declared converters, with at most 64 total coordinates. Observation streams must use `point_source: input` for measured observations. An endpoint may omit all selected measurement fields: it closes the episode using the recorded outcome time and preserves the last measured motion, rather than inventing a new vector. If any selected measurement is supplied, provide the complete vector. `baseline_group` (or a mapped field) is required when learning normal references, so unrelated services do not share one norm. Empty `baseline_states` disables normal-reference learning for domains that do not need it.

The fast centroid uses elapsed-time exponential smoothing. Finite differences produce velocity per second and acceleration per second squared; irregular intervals use the separation between velocity midpoints. Three observations are needed for complete motion evidence. Equal/out-of-order timestamps are rejected in temporal mode, duplicate IDs remain idempotent, and gaps above `max_gap_seconds` reset motion and pending landmarks. An observed endpoint closes an episode; use a new subject ID for the next episode.

The temporal predictor learns bounded feature-cell centroids and within-window outcome counts. Each completed episode contributes its first motion-ready landmark per observed state, preventing repeated samples in the same state from multiplying its votes. Matching uses scaled centroid, velocity, acceleration and elapsed-time features, plus the same state and reference group. Nearest eligible cell populations supply empirical destination frequencies. Unsupported contexts abstain. `max_cohorts` bounds memory by evicting the least recently updated cell; counts within a retained cell are cumulative, not a rolling training window.

A completed episode whose first endpoint was outside the configured window contributes within-window unresolved mass. An unfinished episode does not become a failed outcome. These are uncalibrated completed-episode estimates; selective loss of feedback can bias the learned population. A different estimator can address censoring through the existing predictor interface.

`DestinationPrediction.HorizonSeconds` identifies a temporal forecast, and `HorizonEvents` is zero. The window is anchored at `ObservedAt`, not at the time a caller reads an old snapshot. A query after that window has expired returns `stale_trajectory` and unresolved mass. The `Trajectory` evidence contains raw position, fast centroid, derivatives, elapsed episode time and any learned normal reference.

Normal references use a separate slow, time-decayed mean/variance, updated only by explicitly qualified baseline states. Incident observations never update that reference. Deviation is RMS standardised displacement with a variance floor and minimum effective support; it is a diagnostic distance, not an incident probability. Normal admission and the meaning/scaling of every coordinate belong to the application. Reference records and the outcome model persist through the pluggable store and expire under configured retention.

Run the reproducible two-domain sample:

```sh
dotnet run --project samples/RelevanceShop -- predict-time --output /tmp/relevance-time-814
```
