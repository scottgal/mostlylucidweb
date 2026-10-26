# Mostlylucid.Relevance: please change this

This framework is intended to be adapted. Explain the application's decision first, then choose a representation, outcome and provider. Keep integration small. This file ships in the NuGet packages; ask the coding assistant to read it explicitly. NuGet does not inject it into an application's agent context.

## Read before editing

- Read README.md and public XML comments for the package being used.
- Read samples/RelevancePackage/Program.cs for the AOT, multi-space and sharding example.
- Read the corresponding tests before changing a provider or inference strategy.
- Existing extension points: IPointConverter, IDestinationPredictor, IRelevanceStore, IRelevanceStoreFactory.
- RelevanceHub owns its returned engines. Await SaveAsync for committed visibility; dispose the hub to drain accepted work.

## Decision record

Record these choices in the consuming application, with concrete values:

```yaml
entity: shopper-session | email-version | service-incident | application-defined
candidate: product | email | permitted-action | application-defined
authorised_scope: tenant resolved by the application, plus named recommendation space
representation: named coordinates, units, producer version, missing-value policy
observation: stable event ID, subject/episode ID, event time, measured properties
objective: observed endpoint, desirability/reward, attribution window
retrieval: exact | sqlite-hnsw
eligibility: mandatory configured rules, plus caller constraints
storage: provider and fixed shard topology
verification: held-out outcomes, exact-search recall baseline, isolation, persistence
```

## Which layer to change

| Need | Start here | Preserve |
|---|---|---|
| Product recommendations | Candidate/events streams, point converters, placements | Availability and tenant isolation |
| Email intervention choice | Characterise exact content; optional InterventionLearning | Actual exposure/outcome evidence, control eligibility, propensity |
| Incident diagnosis | Input-sourced observations, trajectory/destination definition | Real elapsed time, qualified normal reference, explicit permitted actions |
| Different encoders | Implement IPointConverter, register an instance at startup | Finite dimensions, stable units/model version, explicit missing evidence |
| PostgreSQL or another store | Implement IRelevanceStore and its transaction; supply a factory | Atomicity, schema binding, typed filters, detached results, rollback |
| More entity domains | Open separate RelevanceSpace values | Never compare incompatible feature spaces |
| Different predictions | Implement IDestinationPredictor | Support evidence, horizon and abstention; scores are not probabilities |

## Security and correctness invariants

- The application authenticates/authorises callers and resolves the tenant. A tenant string is not a credential. Do not let an untrusted request choose a space, database path, converter implementation or native extension path.
- Provider/plugin implementations and configuration are trusted startup code. Do not load user-supplied native binaries or assemblies. Formula conversion is a bounded arithmetic grammar, not arbitrary scripting.
- Keep secrets out of candidate metadata: results return that stored metadata. Map results into an application response DTO as needed.
- Mandatory placement rules combine with caller filters using AND. Never replace them, or apply eligibility only after top-K. Approximate search must remain explicit; the current SQLite provider uses exact SQL for constrained queries.
- JSON inputs are objects of at most 1 MiB; envelope IDs are at most 256 characters. Filters allow at most 64 nodes and 16 nested levels. Keep queues, open spaces, state, candidates, decisions and deduplication bounded.
- Use stable retry IDs. Conflicting reuse fails. Do not invent zero rewards for missing telemetry or count the same endpoint twice.
- A nearest-neighbour edge is similarity evidence, not a causal or semantic fact. A ranking score is not a calibrated probability.
- Change representation versions when units, model, axes or normalisation change. Never reinterpret existing arrays in place.
- Shards route complete tenant/spaces, not individual candidates. The topology manifest rejects silent remapping. Reshard by explicit migration into a new directory.
- Native SQLite HNSW requires one live connection per vector table. Reuse the hub; do not run competing hubs against the same spaces.

## Native AOT path

Use typed RelevanceDefinition or RelevanceDefinition.FromJson. YAML and the convenient reflection-based generic SaveAsync overload are JIT conveniences. In AOT use JsonElement or SaveAsync with caller-generated JsonTypeInfo<T>. Register converters explicitly; no assembly scanning.

InterventionLearning serializes its private records with its own generated metadata. Custom predictor/provider records require generated metadata supplied through the provider's recordTypes argument. Do not suppress a new trimming/AOT warning to hide unsupported reflection.

## Verification after a change

- dotnet test Mostlylucid.Relevance.Tests -c Release
- Native search: set RELEVANCE_SQLITE_VEC and/or RELEVANCE_SQLITE_HNSW to pinned native binaries; require those tests in native CI.
- dotnet publish samples/RelevancePackage -c Release -r <supported-rid>
- Run the published native executable in exact mode and with the bundled SQLite vector asset. Exercise persistence/reopen, constrained recommendations and outcome learning.
- For package changes, repeat with UsePackedRelevance=true against locally packed NuGet packages. A project-reference-only build does not verify native assets in a package.

Add meaningful tests for changed behaviour. Keep article code focused on modelling/inference rather than setup or parser boilerplate.
