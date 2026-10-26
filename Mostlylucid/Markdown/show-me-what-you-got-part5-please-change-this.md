# SHOW ME WHAT YOU GOT (Part 5): Please Change This

> COMING SOON - Working on the nuget package, stay tuned!

In [Part 4](/blog/show-me-what-you-got-part4-mind-control), the recommendation became an intervention. We characterised a proposed change, applied it under an experiment and learned from the observed response. An email could be a candidate in the same way that a product was a candidate in Part 2. A service incident could supply a behavioural trajectory in the same way that a shopping session did in Part 3.

The domain kept changing. The mathematical jobs were remarkably consistent: represent observations, accumulate evidence, find relevant neighbours, estimate outcomes and choose among permitted actions.

This part turns that shared machinery into a small framework: **Mostlylucid.Relevance**. It runs in the application's process, keeps its state in SQLite and can handle several recommendation domains at once. An optional native package gives SQLite an HNSW vector index. Its store boundary remains replaceable, so a PostgreSQL implementation can preserve the same application-facing operations.

It is ready to use with the supplied implementations: built-in vector converters, SQLite persistence, ranking, destination predictors and intervention learning. Configuration supplies the meaning of your data and objectives. Custom implementations are an option when those supplied components no longer fit.

It is also my first deliberately **“please change this” package**. Alongside the code, the packages ship an `AGENTS.md` describing the decisions, extension points and invariants. The intention is to give a developer and their coding assistant a useful starting point for adapting the framework to their own problem.

Use it as supplied first. The agent guide helps a developer or coding assistant customise an already working system, with clear extension points and invariants.

<datetime class="hidden">2026-10-26T12:00</datetime>
<!-- category -- AI,Architecture,Machine Learning,Recommendations,C#,SQLite,Native AOT -->

[TOC]

# The Series, Implemented

This package is the implementation the earlier parts were building towards. Each introduced a job that now has a concrete home:

| Earlier part | What it contributes to the package |
|---|---|
| [Part 1](/blog/show-me-what-you-got-streaming-recommendations) | Characterisation and retained evidence: named points, `PointSummary` and subject state |
| [Part 1.5](/blog/show-me-what-you-got-part1-5-questions-as-coordinates) | Questions and measurements as coordinates: built-in converters and `IPointConverter` |
| [Part 2](/blog/show-me-what-you-got-part2-building-relevance) | Eligible retrieval and rank fusion: `RelevanceEngine` and store queries |
| [Part 3](/blog/show-me-what-you-got-part3-predict) | Time, motion and destination forecasts: trajectory state and `IDestinationPredictor` |
| [Part 4](/blog/show-me-what-you-got-part4-mind-control) | Measured interventions: `InterventionLearning`, assignments and reward estimates |

We will follow those same boundaries through the package. The earlier articles explain the mathematics and modelling choices in detail; here we show how those choices fit together, where application-specific decisions enter, and which component to change when the domain changes.

For a shop, the entity may be a session and the candidate a product. For marketing, the entity may be a detected behavioural group and the candidate an exact email version. For operations, the entity may be an incident episode and the candidate an allowed diagnostic action.

The application defines the coordinates, permitted candidates and desirable outcomes. The framework supplies the reusable operations over that evidence.

> **ML aside: a reusable learner still needs a problem definition.** Inputs, targets, units, eligibility and observation windows are part of the model. The package makes those decisions explicit so we can reuse the machinery across domains without assuming the same measurements have the same meaning everywhere.

The [working package sample](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevancePackage) puts product, email and operations domains in one process. It demonstrates the shared packaging and persistence path. The richer shopping, trajectory and email examples from the previous parts supply the domain-specific evidence.

# Give the Existing Engine a Space

A **space** is a tenant-local recommendation domain. It owns a candidate catalogue, entity summaries, representations, outcome evidence and schema binding.

Begin with the shipped vector baseline and one SQLite-backed space. `ForVectors` supplies the converter, streams and ranking head introduced in [Part 2](/blog/show-me-what-you-got-part2-building-relevance). You supply the vector length and any filter fields:

```csharp
var definition = RelevanceDefinition.ForVectors(2, filters: ["available"]);

await using var hub = new RelevanceHub(
    new SqliteRelevanceFactory("./relevance-data"));

var products = await hub.OpenAsync(
    new RelevanceSpace(authorisedTenant, "products"), definition);

await products.SaveAsync("items", item, AppJson.Default.Item);
await products.SaveAsync("events", observation, AppJson.Default.Observation);

var result = await products.RecommendAsync(
    "entity-1", "next", Filter.Eq("available", true), count: 2);
```

The preset names its candidate stream `items`, observation stream `events`, vector point `traits` and recommendation placement `next`. An observation of type `observe` contributes positive affinity; the `helpful` type also supports the learning layer's observed-response bridge. These defaults work without implementing an interface.

The first save adds or updates a candidate. The second records what the entity did. The recommendation then uses the committed evidence and returns eligible candidates with their ranking reasons. `item` and `observation` are application data; the `AppJson` arguments supply generated serialization metadata, which we will return to in the AOT section.

Exact search works immediately, with no additional vector extension. A second domain opens through the same hub:

```csharp
var emails = await hub.OpenAsync(
    new RelevanceSpace(authorisedTenant, "emails"), emailDefinition);
```

The hub reuses an engine when the same space is opened with the same semantics. An incompatible repeated definition is rejected, protecting retained evidence from an accidental change of meaning.

The application resolves and authorises the tenant before this call. The string is an identity supplied to the framework, not a credential. A web endpoint must not let an arbitrary request select another tenant's space merely because the API accepts a name.

Within a space, subject IDs identify the entities being observed. Use separate spaces when candidate types, representations, retention or objectives differ. Use stable subject or episode IDs within each one. A service's next incident should not accidentally continue an earlier incident's already completed trajectory.

The sample uses the same candidate and subject IDs in different domains. Its results remain independent.

Internally, the compound identity is encoded as a structured pair:

```csharp
public string Key => RelevanceJson.Serialize(new[] { Tenant, Name });
```

This avoids ambiguous delimiter concatenation. Tenant `a:b`, space `c` must not become the same identity as tenant `a`, space `b:c`. The storage scope is derived from the complete key; each engine still checks its representation/configuration fingerprint.

The hub owns the engines it opens. Application code disposes the hub after callers finish, allowing accepted channel work to drain before its stores close. Individual saves still complete after conversion and commit, as [Part 2](/blog/show-me-what-you-got-part2-building-relevance) established.

# Representation: The Points From Parts 1 and 1.5

Every named point retains its converter semantics, dimension and distance metric. A vector supplied by a decision model can coexist with deterministic measurements and an optional semantic encoder.

For an email, that could mean structural coordinates, interpreted meaning, timing and offer size. For a service, it might mean measured pressure, transition features and displacement from its qualified normal reference. [Part 1.5](/blog/show-me-what-you-got-part1-5-questions-as-coordinates) explains how classifier questions can produce additional coordinates.

This is where adaptability belongs. Add a converter, define its units and version, and measure whether the resulting representation helps the decision.

Changing the output length is obviously incompatible. Changing the meaning while keeping the length is just as important. A price measured in pounds cannot quietly become a percentage, and a new semantic model cannot reuse the old representation identity merely because both return 384 numbers.

The engine freezes its configuration when constructed. A provider binds the resulting fingerprint to the space. Reusing stored state with different semantics fails explicitly; migration or a new identity is an application decision.

> **ML aside: inference depends on the representation.** A more powerful nearest-neighbour index cannot recover distinctions that the producer discarded. If the coordinates fail to distinguish help choosing from pressure to purchase, an extremely fast search will still find neighbours under that inadequate description. Index quality and representation quality are different questions.

The [converter contract](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance/Contracts.cs) therefore remains small: a name, a version, declared dimensions and an asynchronous conversion operation. Register actual converter instances at startup. There is no need for an assembly scanner or a runtime scripting engine to make that pluggable.

# Adaptation: Follow an Observation Through the Engine

The streaming behaviour from Parts 1 and 2 lives in [RelevanceEngine.SaveAsync](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance/RelevanceEngine.cs), the [bounded worker](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance/RelevanceWorker.cs) and the store transaction. The public call chooses a configured stream; the stream determines whether the input is a candidate, an observation or attributed feedback.

A candidate is converted into its named points. An observation either refers to those saved candidate points, as a product view does, or supplies measured input for conversion, as service telemetry does. Both routes reach the same evidence-update machinery.

```text
SaveAsync → bounded worker → resolve/convert points
                                    ↓
                  transaction: deduplicate + update evidence
                                    ↓
                          committed subject state
```

The transaction binds the representation schema, checks retry identity and updates the subject's summaries. Candidate-referenced interactions contribute their configured event weight to affinity. Input-sourced trajectories additionally preserve measured positions and timestamps for motion analysis.

That distinction is why one generic “vector update” method would be too vague. A product view contributes preference evidence. A latency reading measures current state. Both use vectors, but the configured observation route determines what we retain and infer from them.

Awaiting the save establishes visibility for a later recommendation or prediction. The channel supplies ordered work and backpressure; the transaction supplies atomic evidence updates. [Part 2](/blog/show-me-what-you-got-part2-building-relevance) explains that implementation boundary, which the package now reuses for every space.

# Retrieval: The Ranker From Part 2

The next question is which candidates are relevant to the accumulated evidence. `RecommendAsync` reads the subject's per-point summaries, retrieves eligible neighbours for each configured head and combines their ranks. A colour distance, price distance and semantic distance keep their separate meanings; weighted RRF combines rank evidence as Part 2 described.

The provider decides how to find those neighbours. Changing storage or enabling an approximate index leaves the configured heads, filters and returned ranking reasons in place.

## Why SQLite Is the Default

The requirements here are a small deployment, incremental adaptation and few moving parts. SQLite gives us a useful common boundary for candidates, observations, model summaries, deduplication and attributed decisions.

The default search is exact. Vectors are float32 BLOBs, and SQL evaluates compatible eligible rows by distance. This is useful for small catalogues, low-dimensional characterisations and a reference against which to test approximation. A vector database does not become necessary simply because the data happens to be an array.

Larger candidate sets can opt into a SQLite-owned HNSW index while keeping the same save and recommendation operations.

The optional package uses [sqlite-vector-rs 0.3.1](https://github.com/quinnjr/sqlite-vector-rs/tree/v0.3.1). It exposes HNSW through SQLite virtual tables and persists the graph in SQLite shadow tables. The C# framework issues SQL; it does not maintain a separate managed HNSW structure or an external index file.

This is a young extension. We pin its source and verify the behaviours on which the adapter relies, rather than interpreting a feature list as a durability guarantee. The exact provider remains useful independently of it.

## Exact Search and Approximate Neighbours

Exact nearest-neighbour search compares a query with every eligible vector in the relevant representation. It can tell us the closest candidates under the declared distance, with deterministic tie ordering.

**Approximate nearest-neighbour search**, usually shortened to **ANN**, tries to avoid many of those comparisons. It searches promising parts of an index and accepts the possibility of missing a true neighbour.

Suppose the exact top ten candidates are our reference set. If an approximate search finds nine of those ten, its recall at ten for that query is `0.9`. That says something about retrieval fidelity. It says nothing about whether nine shoppers purchased, or whether a forecast is calibrated.

```csharp
var recallAtK = approximateIds.Count(exactIds.Contains)
    / (double)exactIds.Count;
```

The exact result is the reference for the same representation, metric, eligibility rules and requested count. Comparing incompatible filters or encoders would not measure index recall.

> **ML aside: approximation adds another source of error.** The response model may be imperfect, the representation may omit useful evidence, and the index may miss a relevant candidate. Those errors belong to different layers. Keeping an exact baseline lets us investigate whether changing retrieval altered the candidate pool before blaming the learner.

The native test includes a fixed-seed recall comparison, along with updates, rollback and reopen checks. That is a correctness fixture, not a benchmark predicting latency or recall for a million real product vectors.

## HNSW: Follow the Neighbourhood

HNSW expands to **Hierarchical Navigable Small World**. Think of a graph whose nodes are vectors and whose links offer routes to nearby vectors.

A search begins in a sparse upper layer to move quickly into a promising region. It descends into denser layers and explores a local neighbourhood. The hierarchy supplies shortcuts; the lower layer supplies a more detailed search. The [HNSW paper](https://arxiv.org/abs/1603.09320) describes the algorithm.

The index is doing navigation. It is not estimating whether a shopper will purchase or deciding whether a rate-limit change is safe.

Its settings expose useful trade-offs. More graph connections can improve navigability while consuming more memory. A wider construction search spends more effort building the graph. A wider query search considers more alternatives, usually trading work for better recall.

```csharp
var vectors = new SqliteVectorOptions
{
    ExtensionPath = SqliteVectorExtension.Path,
    Connections = 16,
    ConstructionWidth = 200,
    SearchWidth = 100
};
```

Those are the sample defaults, not universally optimal values. Tune them against the exact baseline and actual workload. A higher setting is not free accuracy.

HNSW can add new candidates incrementally, making it a useful match for this evolving catalogue. The graph still needs memory, updates and durable storage; the SQLite extension handles those responsibilities.

## Find Related Candidates

A recommender normally searches from a subject's accumulated evidence. We can also ask for neighbours of an existing candidate:

```csharp
var neighbours = await products.RelatedAsync(
    candidate: "walking-boot-a",
    point: "traits",
    count: 5);
```

The method selects one named representation, excludes the seed and returns compatible neighbours with distances. With the SQLite HNSW option, an unfiltered query can navigate the native graph.

This can reveal similar product characterisations, related email structures or nearby action descriptions. The meaning depends on `traits`. A structural email vector discovers structural neighbours, not necessarily messages with the same observed effect.

The HNSW graph's internal navigation edges are not exported as verified knowledge. We can construct a separate relationship view from neighbour results, retaining the representation version, metric and evidence. Later response observations may support stronger statements about behaviour. An LLM-supplied name can help describe a discovered group, but does not turn proximity into a causal claim.

This connects back to [Part 1's emergent recommendation](/blog/show-me-what-you-got-streaming-recommendations#emergent-recommendation-discover-the-patterns): the useful structure emerges from what we preserve and observe. Search makes that structure accessible; interpretation still needs evidence.

## Keep Eligibility Rules Intact

Eligibility is part of the query. Availability, permitted action scope or an allowed price range cannot disappear because a faster index is enabled.

The current SQLite adapter uses HNSW for unfiltered retrieval and exact SQL for constrained queries. That includes mandatory placement restrictions, so a request for ten available products considers the eligible catalogue before taking its result window.

This is a deliberate small first adapter. The extension has metadata filtering facilities, but translating the framework's complete typed predicate contract into them is additional work that needs its own tests. Until that translation exists, the exact path preserves the contract.

Mandatory placement rules combine with caller constraints using **AND**. A caller cannot weaken `available = true` by requesting `available = false`; the conjunction simply has no matches.

Filter paths must be declared, values are bound parameters, and comparisons preserve scalar types. A string containing SQL syntax remains a value. The public compiler also bounds predicate complexity; the engine caps filters at 64 nodes and 16 nested levels.

The API returns candidate metadata because it is part of the recommendation evidence. Applications should store only what belongs in that boundary and project results into their own response shape. A filtered catalogue is not a suitable place to hide secrets that happen not to be used by a ranking head.

# Prediction: The Time and Motion From Part 3

Recommendation asks which candidate matches the current evidence. Prediction asks where the observed journey is likely to end within a declared horizon. They share the saved observations but return different kinds of result.

With destinations configured, the application-facing call remains small:

```csharp
var forecast = await products.PredictAsync(sessionId);
```

The implementation snapshots the subject state, retained outcome model and destination definition, then passes them to `IDestinationPredictor`. The default uses the transition baseline when only event-count destinations are configured, or the trajectory estimator when elapsed-time prediction is enabled.

The [trajectory implementation](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance/TrajectoryPrediction.cs) preserves the distinction we developed in [Part 3](/blog/show-me-what-you-got-part3-predict): current position, fast behavioural centroid, velocity, acceleration and elapsed time. Historical completed episodes provide evidence about destinations reached from comparable motion. Qualified normal observations can also maintain a slower reference for drift and incident analysis.

Those statistics belong to the prediction model, not the candidate-search index. Enabling HNSW does not turn catalogue proximity into a destination probability. The predictor remains responsible for its horizon, supporting observations and unresolved mass when the evidence is insufficient.

This is also a practical extension point. A different prediction strategy can consume the same saved context through `IDestinationPredictor`. It should be evaluated against the existing baseline on later observed journeys, while preserving the meaning of the forecast and its evidence. The [trajectory sample](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevancePredict) contains the two-domain worked examples.

# Learning: The Intervention Loop From Part 4

[Mostlylucid.Relevance.Learning](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance.Learning/InterventionLearning.cs) is the separately packaged layer for choosing a change and observing what follows. The exact email or operational configuration becomes a candidate. The entity's observed context, the intervention descriptor and the eventual reward remain separate records.

Its three operations follow the loop from [Part 4](/blog/show-me-what-you-got-part4-mind-control):

```csharp
await learner.RegisterAsync(variant);
var assignment = await learner.ChooseAsync(request);
await learner.SaveAsync(observedOutcome);
```

Registration retains an exact version and its characterisation. Choice records the starting context, eligible alternatives and actual assignment probability. The observed outcome updates evidence once, under the declared objective and time window. Returning an assignment is not proof that an email was delivered or an action applied; those stages need observations of their own.

Inside that boundary are the detected context groups, descriptor-conditioned reward estimates and exploration policy introduced earlier. The learner calls the existing recommender to rank intervention candidates, then keeps its own experimental ledger. It reuses relevance machinery while retaining the additional evidence needed to learn from intervention.

Each experiment needs its own scoped identity and owned store. The application must bind that identity to the authorised tenant/domain; opening a recommendation space does not automatically create or authorise an experiment. Context axes, action axes and objective versions are explicit in `LearningDefinition`.

> **ML aside: retrieval, prediction and action choice have different targets.** Retrieval finds candidates supported by the representation. Prediction estimates a journey's destination. Intervention learning estimates reward under a chosen change and keeps exploring alternatives. They can share observations and infrastructure without treating their scores as interchangeable probabilities.

The [email example](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevanceLearning) supplies the full content-characterisation experiment. The smaller package sample checks that the same registration, assignment and outcome path also works under Native AOT.

# Package the Shared Machinery

The domain-facing operations are now in place. Spaces, shards, deployment and the agent guide make that implementation reusable without changing the meaning of the earlier techniques.

## Add Shards for Independent Spaces

Sharding divides stored work between partitions. Start with the single file above; add named shard files when several independent domains need separate storage and write queues.

Here the unit is the complete **tenant/space**. Candidates, subject state, transition evidence, decisions and deduplication for that space route to one shard file. That preserves the local transaction used throughout the earlier articles.

```csharp
await using var hub = new RelevanceHub(
    new SqliteRelevanceFactory("./data", ["east", "west"], vectors));
```

The sample's observed routing is:

```text
shop/products shard=west recommendations=a,b related=b
shop/emails shard=west recommendations=a,b related=b
ops/actions shard=east recommendations=a,b related=b
```

The routing uses **rendezvous hashing**: compute a stable score for the compound space identity paired with each shard name, then choose the highest score. Ordering the same shard names differently does not change the winner.

Using stable hashing matters because runtime string hash codes are not a durable routing contract. Encoding the whole compound identity matters because ambiguous concatenation could route distinct scopes as if they were one.

Adding a shard may change winners. It does not move any data. The SQLite factory persists a topology manifest and refuses an unexpected change rather than silently presenting an empty model on a different file. Explicit migration into a new directory keeps that operation visible.

This partitions many independent recommendation domains. It does **not** split one enormous catalogue across several files and merge a distributed top-K. That next design would need fan-out, comparable per-head results, failure semantics and a consistency policy for shared learning state. The whole-space boundary keeps this implementation small and each decision atomic.

## Keep the Same Workflow Under Native AOT

Native AOT compiles the application ahead of execution. A package can remain extensible without discovering arbitrary types at runtime: register converter and provider instances explicitly, and provide serialization metadata when the application is built.

The core, SQLite and learning projects are marked AOT-compatible. More usefully, the sample is published and executed as a native program. It disables reflection-based JSON serialization and treats warnings as errors.

For typed input, the important difference is the metadata argument:

```csharp
await engine.SaveAsync("items", item, AppJson.Default.Item);
```

`AppJson.Default.Item` comes from the application's source-generated JSON context. The engine can snapshot the input without discovering its properties at runtime. An existing `JsonElement` is another supported input route. Internal records use generated metadata too; an unknown custom provider record requires its own supplied metadata.

Use a typed definition or `RelevanceDefinition.FromJson` in this path. The YAML loader and generic reflection-based save convenience remain available to JIT applications, with explicit compatibility annotations. Microsoft's [source-generation guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation) explains the metadata mechanism.

The separate **Mostlylucid.Sqlite.Vector** package supplies native runtime assets and fixed path discovery. Consumers do not need Cargo, a Rust compiler, a download during startup or a manually copied extension. The SQLite provider loads the known extension through its explicit entry point.

AOT does not make native dependencies disappear. Initial asset targets are Linux x64, Windows x64 and macOS ARM64. Each needs its platform library. The package workflow builds and exercises each host before assembling the complete asset package; local verification here is on macOS ARM64.

## Please Change This, With a Map

The shipped `AGENTS.md` describes the framework as a set of decisions a coding assistant can help implement. It is part of the package, not a claim that NuGet automatically injects instructions into an agent's context. Give the assistant the guide when asking it to adapt the framework.

Its starting record asks for concrete meanings:

```yaml
entity: service-incident
candidate: permitted-diagnostic-action
representation: named coordinates, units, producer version, missing-value policy
observation: stable event ID, episode ID, event time, measured properties
objective: observed endpoint, desirability, attribution window
retrieval: exact | sqlite-hnsw
eligibility: mandatory configured rules, plus caller constraints
storage: provider and fixed shard topology
```

For a shop, most customisation belongs in converters, stream mappings, placements and observed feedback. For email experimentation, the important choices are the exact content characterisation, control, assignment unit, exposure evidence and reward. For incidents, real elapsed time, normal-reference admission and the set of allowed actions matter.

A new provider belongs behind `IRelevanceStore` and `IRelevanceStoreFactory`. It must preserve rollback, deduplication, scope isolation, typed filters and detached results. A PostgreSQL implementation should not require the application to learn a different save/recommend/predict vocabulary.

A different prediction strategy belongs behind `IDestinationPredictor`. It must explain its horizon, support and abstention rather than return a confident-looking number for every input. A new representation belongs behind `IPointConverter`, with its actual semantics and version.

The guide also states the trust boundary. Converters, configuration, provider factories and native library paths are trusted startup choices. They are not request parameters. Inputs are bounded, worker queues apply backpressure, and retained records have configured capacities. JSON observations/candidates are capped at 1 MiB, with bounded envelope identifiers.

> **ML aside: the framework does not choose the objective for you.** Purchase probability, margin reward, contact cost and recovery time answer different questions. An assistant can generate code for any of them. It still needs a clear statement of the intended result and the evidence that will count towards it.

This is the purpose of “please change this”: offer a small implementation whose assumptions are visible enough to change deliberately. The agent guide supplies the starting map and the tests supply examples of properties an adaptation must preserve.

# Evidence From the Package Sample

The native executable exercises recommendations in three independent domains, relationship search, observed intervention reward and persistence after reopening. It also runs as a consumer of the packed NuGet packages, so the test checks native asset delivery rather than succeeding because a developer copied a library into the output directory.

The local test run has 97 passing tests, including both the existing sqlite-vec path and the new SQLite-owned HNSW path. They check isolation and persistence, compare approximate retrieval with exact distances, and exercise the input and eligibility boundaries. The [review notes](https://github.com/scottgal/mostlylucidweb/blob/main/docs/relevance/part5-framework-review.md) retain the detailed coverage.

These are unpublished preview packages. The completed cross-platform checks cover Windows x64, Linux x64 and macOS ARM64, including native extension builds, tests and AOT consumers of the actual packages. The workflow assembles all four packages into a downloadable feed; publication to NuGet.org is a separate release step.

The native extension's connection model is also explicit. It assumes one live writer/connection per vector table, with graph reconciliation on reconnect. The provider holds a per-space ownership lease and rejects competing .NET provider instances; independent SQLite clients must respect the same single-owner contract. Reuse the hub. Use SQLite's backup API when backing up a live database, so committed WAL state is included.

The [package sample](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevancePackage) contains the verification commands and the [agent guide](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid.Relevance/AGENTS.md) contains the adaptation recipes. The article concentrates on why those boundaries exist.

# Use It First, Then Customise It

The implementation covers the progression from Parts 1–4: characterise observations, update evidence as it arrives, retrieve relevant candidates, predict configured destinations and learn which permitted interventions produce useful observed outcomes.

For a vector recommender, start with `ForVectors`, save candidates and interactions, then request recommendations. For destination prediction, configure the application's states, outcomes and timescale and use the shipped predictor. For interventions, include the learning package, register the allowed variants and control, then report their observed results. Those components are implemented and usable without writing a new learner.

Customisation begins with configuration: different axes, event weights, placements, eligibility rules and objectives. New converters, predictors or stores are extension points when the application needs them. The supplied SQLite provider, built-in representations and statistical learners remain a working starting point.

“Please change this” is an invitation to adapt a useful package to your domain. It is not a request to finish the package before you can use it.
