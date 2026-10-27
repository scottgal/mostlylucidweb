using System.Text.Json.Serialization;

namespace Mostlylucid.Relevance.Learning;

/// <summary>Generated serialization metadata for public intervention requests and evidence.</summary>
public static class LearningJson
{
    /// <summary>Add to a serializer resolver chain or pass to RelevanceJson for learning records.</summary>
    public static JsonSerializerContext Metadata => LearningJsonContext.Default;
}


[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LearningSnapshot))]
[JsonSerializable(typeof(InterventionJourney))]
[JsonSerializable(typeof(InterventionDecision))]
[JsonSerializable(typeof(InterventionEvidence))]
[JsonSerializable(typeof(DetectedGroup))]
[JsonSerializable(typeof(InterventionScore))]
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
