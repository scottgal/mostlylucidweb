# Mostlylucid.Relevance SDK

A working in-process framework for streaming recommendations, outcome prediction and measured intervention learning. Start with the built-in converters, SQLite provider and statistical learners. Extend the interfaces only when your application needs different behaviour.

| Package | Purpose |
|---|---|
| Mostlylucid.Relevance | Configuration, converters, streaming state, recommendation, prediction, hub and provider contracts |
| Mostlylucid.Relevance.Sqlite | Durable storage, filtered SQL retrieval, named shard files and hosted registration |
| Mostlylucid.Relevance.Learning | Versioned interventions, context groups, assignments, observed outcomes and online reward learning |
| Mostlylucid.Sqlite.Vector | Pinned native SQLite HNSW extension and AOT-compatible native asset discovery |

[Start here](guides/getting-started.md), then use the [complete API reference](api/index.md) for signatures, defaults, parameters, return values and member documentation. The guides explain how those members fit together.

Target: .NET 10. Native AOT is tested using real package consumers on Linux x64, Windows x64 and macOS ARM64. Public NuGet publication is controlled by the release workflow; a successful verification build also produces a complete downloadable preview feed.
