# Configuration and representations

`RelevanceDefinition` is mutable configuration. The engine copies and validates it at construction; later caller mutations cannot alter the running space. `ForVectors` supplies a complete single-vector baseline. For other data, set named points, streams and placements with the supplied converters.

Typed definitions, JSON and YAML work under Native AOT. JSON accepts conventional case-insensitive property names and rejects unknown members. YAML uses snake_case property names, rejects unknown/duplicate keys, aliases and multiple documents. YAML/JSON configuration is capped at 1 MiB; YAML nesting is capped at 32 levels and 100,000 parser events. Quote numeric-looking identifiers and category strings to retain their string type.

```yaml
id: shop-v1
filters: [available, price]
points:
  price: {path: price, converter: formula, expression: "x / 200", metric: l2}
streams:
  items: {kind: candidate}
  events: {kind: observation, weights: {view: 1, basket: 3}}
  outcomes: {kind: feedback}
placements:
  next:
    heads: {price: 1}
    where: {available: true}
```

`RelevanceDefinition.FromYaml(text)` and `FromJson(text)` parse; engine construction validates. File-based SQLite helpers choose JSON for `.json` and YAML otherwise. Paths in definitions are case-sensitive dotted object-property paths, not arbitrary expressions. Undeclared filter paths fail. Missing values are not zero.

## Engine settings

| Property | Default | Contract |
|---|---:|---|
| Id | empty | Required storage/model identity; hub replaces it with tenant/space scope |
| Version | 1 | Positive schema revision |
| HalfLifeMinutes | 30 | Positive affinity evidence half-life |
| MinEvidence | 0.01 | Positive decayed mass needed for an active head |
| StateRetentionHours | 24 | State/model/deduplication retention |
| DecisionRetentionHours | 24 | Attributed-feedback decision retention |
| MaxStateRecords | 10,000 | Active subject capacity |
| MaxCandidates | 100,000 | Candidate capacity; overflow rolls back |
| MaxDecisions | 10,000 | Retained decision capacity |
| MaxDeduplicationRecords | 100,000 | Retry/outcome token capacity |
| CandidateLimit | 100 | Per-head window and max requested count, 1–10,000 |
| RrfConstant | 60 | Positive denominator offset for rank fusion |
| Destinations | null | Destination/trajectory inference disabled until configured |
| Filters | empty | Allowed scalar metadata paths |
| Streams / Points / Placements | empty | All three require entries |

## PointDefinition

`Path` is required. `Converter` defaults to `numeric`; `Metric` defaults to `l2`. `Displays` is descriptive meaning/units. `Scale` defaults to 1 and must be positive. `Categories` is an ordered vocabulary. `Dimensions` defaults to 0 and must be declared for precomputed vectors. `Expression` is the formula text.

| Converter | Input | Output |
|---|---|---|
| numeric | number | one coordinate divided by Scale |
| boolean | boolean | one coordinate, 0 or 1 |
| categorical | vocabulary string | ordered one-hot vector |
| vector | numeric array | declared number of finite float32 coordinates |
| formula | number x | one finite coordinate from bounded arithmetic |
| rgb-oklab | #RRGGBB sRGB swatch | three Oklab coordinates; requires L2 |

Formula grammar supports `x`, finite numbers, arithmetic, parentheses, `abs/min/max/clamp`. Length and nesting are bounded; there is no arbitrary code, I/O, reflection or scripting. Cosine vectors must be nonzero. Every representation is dimensioned and fingerprinted using converter semantics/version and settings. Changing units, vocabulary order or model requires a new identity/version or explicit migration.

## StreamDefinition and PlacementDefinition

Stream `Kind` is candidate, observation or feedback. `PointSource` defaults to candidate: observations refer to a saved candidate. Set input for measured vectors/properties. `Fields` maps envelope names to dotted paths; omitted entries use conventional field names. `Weights` maps observation types to nonnegative weights. Zero updates sequence state without positive affinity.

A placement requires named positive `Heads` weights. Its `Where` dictionary contains mandatory scalar equalities. Caller filters combine using AND, preserving eligibility. RRF combines head ranks, not incompatible distances; its score is not a probability.
