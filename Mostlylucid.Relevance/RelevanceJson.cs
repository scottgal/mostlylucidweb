using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Mostlylucid.Relevance;

/// <summary>Reflection-free record serialization with optional application-generated metadata.</summary>
public static class RelevanceJson
{
    /// <summary>Serializes a known record; custom provider records require supplied generated metadata.</summary>
    public static string Serialize<T>(T value, JsonSerializerContext? context = null) => JsonSerializer.Serialize(value, Info<T>(context));
    /// <summary>Deserializes a known record; custom provider records require supplied generated metadata.</summary>
    public static T? Deserialize<T>(string json, JsonSerializerContext? context = null) => JsonSerializer.Deserialize(json, Info<T>(context));
    /// <summary>Snapshots a known value as JSON without runtime type discovery.</summary>
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Info<T>(null));
    /// <summary>Encodes a supported scalar or existing JSON value; arbitrary application objects require explicit metadata.</summary>
    public static JsonElement Scalar(object value) => value switch
    {
        JsonElement json => json.Clone(), string x => Element(x), bool x => Element(x),
        int x => Element(x), long x => Element(x), double x => Element(x), float x => Element(x), decimal x => Element(x),
        _ => throw new ArgumentException("Use a JSON scalar or string, boolean, int, long, float, double or decimal.")
    };
    private static JsonTypeInfo<T> Info<T>(JsonSerializerContext? context)
    {
        var info = context?.GetTypeInfo(typeof(T)) ?? CoreJsonContext.Default.GetTypeInfo(typeof(T));
        if (info is JsonTypeInfo<T> typed) return typed;
        throw new NotSupportedException($"Generated JSON metadata required for {typeof(T)}.");
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RelevanceDefinition))]
[JsonSerializable(typeof(Candidate))]
[JsonSerializable(typeof(SubjectState))]
[JsonSerializable(typeof(TransitionModel))]
[JsonSerializable(typeof(NormalReference))]
[JsonSerializable(typeof(RecommendationResult))]
[JsonSerializable(typeof(Dictionary<string, HeadEvidence>))]
[JsonSerializable(typeof(PointFingerprint))]
[JsonSerializable(typeof(SchemaFingerprint))]
[JsonSerializable(typeof(EventIdentity))]
[JsonSerializable(typeof(OutcomeIdentity))]
[JsonSerializable(typeof(TrajectoryCell))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(string[]))]
internal partial class CoreJsonContext : JsonSerializerContext;

internal sealed record PointFingerprint(PointDefinition point, string Version, int dimensions);
internal sealed record PointSignature(string Key, string Signature);
internal sealed record SchemaFingerprint(int StorageContract, RelevanceDefinition definition, PointSignature[] Points);
internal sealed record EventIdentity(string source, string id);
internal sealed record OutcomeIdentity(string decisionId, string candidateId);
internal sealed record TrajectoryCell(string State, string Group, string[] Bins);
