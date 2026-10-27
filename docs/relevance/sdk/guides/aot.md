# Native AOT and hosting

All four packages enable AOT/trim analyzers. Compatibility is checked by publishing and executing actual NuGet consumers on linux-x64, osx-arm64 and win-x64 with reflection-based JSON disabled and warnings treated as errors. The consumer covers YAML/JSON loading, standalone and hosted engines, all converters, explicit custom converters/records, prediction/normal references, learning, filters and public result serialization.

## Supported ingestion paths

| API | Native AOT |
|---|---|
| SaveAsync(source, RelevanceItem/RelevanceObservation/RelevanceFeedback) | Supported with shipped metadata |
| SaveAsync(source, JsonElement) | Supported; snapshots input |
| SaveAsync(source, customValue, JsonTypeInfo<T>) | Supported with caller-generated metadata |
| SaveAsync<T>(source, customValue) without metadata | JIT convenience; annotated RequiresDynamicCode/RequiresUnreferencedCode |
| ForVectors / typed definition / FromJson / FromYaml | Supported |
| SqliteRelevance.OpenAsync(file) | YAML/JSON supported |
| AddRelevanceSqlite with typed or file definition | Supported |
| Custom provider record get/put | Supported when its generated context is supplied to the store |

There is no reflection fallback in internal persistence. `ForVectors` and the supplied input envelopes let an application start without its own generator. Arbitrary application DTOs still need metadata; AOT cannot discover unknown shapes at runtime.

```csharp
using System.Text.Json.Serialization;
await engine.SaveAsync("items", customItem, AppJson.Default.Product);

[JsonSerializable(typeof(Product))]
internal partial class AppJson : JsonSerializerContext;
```

The DTO's property shape must match its point/stream mappings. With built-in envelopes, metadata fields live beneath `metadata`. Do not change paths merely to add a serializer.

## Serialize public results

`RelevanceJson.Metadata` supplies core input/result/filter/configuration model metadata. `LearningJson.Metadata` supplies public intervention requests, assignments and evidence. Both are ordinary `JsonSerializerContext` objects usable in resolver chains.

```csharp
var json = RelevanceJson.Serialize(prediction);
var evidenceJson = RelevanceJson.Serialize(snapshot, LearningJson.Metadata);
```

For a Minimal API host, add both to `ConfigureHttpJsonOptions(...).SerializerOptions.TypeInfoResolverChain` along with the application's context. Do not enable reflection globally to hide a missing DTO. Core filter polymorphism uses `$filter` with `comparison` and `all` discriminator values; validate filters before applying them.

## Hosted engine

```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddRelevanceSqlite(RelevanceDefinition.ForVectors(2), "./relevance.db");
using var host = builder.Build();
await host.StartAsync();
var engine = host.Services.GetRequiredService<RelevanceEngine>();
// Await application work before stopping the host.
await host.StopAsync();
```

Register once for the single-engine helper. Converters and optional predictor are explicit DI registrations, not assembly scanning. Typed configuration is copied during registration; file configuration is loaded when the engine resolves. The host starts/stops the shared worker and disposes the engine/store. For multiple domains, register a hub with a singleton factory delegate and let DI dispose that owned hub; do not also dispose individual engines. Registering an already created instance has different DI ownership, so keep one clear owner.

## Deployment

Publish for a supported RID with `PublishAot=true`. The SDK/compiler and native toolchain are build-time prerequisites only; consumers of the native-assets NuGet do not compile Rust. Deploy the produced directory, including SQLite/native extension libraries. AOT is not the same as eliminating native dependencies or supporting arbitrary dynamic plugins.

Do not suppress trim/AOT warnings to paper over missing metadata. Custom component constructors should be explicit. Avoid dynamic assembly loading, generated expression compilation and runtime type discovery in converters/predictors. The sample's `AotCoverage` is the executable acceptance test for these paths.
