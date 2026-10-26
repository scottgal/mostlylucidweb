using System.Text.Json.Serialization;

namespace Mostlylucid.Relevance.Learning;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LearningDefinition))]
[JsonSerializable(typeof(InterventionVariant))]
[JsonSerializable(typeof(InterventionRequest))]
[JsonSerializable(typeof(InterventionEvent))]
[JsonSerializable(typeof(LearningState))]
[JsonSerializable(typeof(DecisionState))]
[JsonSerializable(typeof(LearningFingerprint))]
[JsonSerializable(typeof(CandidateIdentity))]
[JsonSerializable(typeof(ResponseObservation))]
internal partial class LearningJsonContext : JsonSerializerContext;

internal sealed record LearningFingerprint(int Contract, LearningDefinition Definition);
internal sealed record CandidateIdentity(string id);
internal sealed record ResponseObservation(string id, string subject, string candidate, string type);
