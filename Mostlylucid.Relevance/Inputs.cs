using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mostlylucid.Relevance;

/// <summary>A precomputed-vector candidate supported without caller-generated serialization metadata.</summary>
/// <param name="Id">Stable candidate identity within the recommendation space.</param>
/// <param name="Vector">Finite coordinates matching the configured traits point.</param>
/// <param name="Metadata">Optional JSON object for application fields; filters use paths such as metadata.available.</param>
public sealed record RelevanceItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("vector")] float[] Vector,
    [property: JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonElement? Metadata = null);

/// <summary>An AOT-safe conventional observation envelope for candidate interactions or measured vectors.</summary>
/// <param name="Id">Stable retry identity for this observation.</param>
/// <param name="Subject">Subject or episode identity within the space.</param>
/// <param name="Candidate">Saved candidate identity; omit for input-sourced measurements or unmeasured endpoints.</param>
/// <param name="Type">Configured event weight key; ForVectors accepts observe and helpful.</param>
/// <param name="At">Observed UTC instant; omission uses processing time.</param>
/// <param name="State">Optional explicit configured transition state.</param>
/// <param name="Vector">Optional measured vector for input-sourced observations.</param>
/// <param name="BaselineGroup">Required reference group when qualified normal-reference learning is enabled.</param>
public sealed record RelevanceObservation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("candidate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Candidate = null,
    [property: JsonPropertyName("type")] string Type = "observe",
    [property: JsonPropertyName("at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? At = null,
    [property: JsonPropertyName("state"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? State = null,
    [property: JsonPropertyName("vector"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] float[]? Vector = null,
    [property: JsonPropertyName("baseline_group"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? BaselineGroup = null);

/// <summary>An observed attributed reward supported without application JSON metadata.</summary>
/// <param name="Id">Stable feedback event ID.</param>
/// <param name="Recommendation">Retained recommendation ID, before its attribution record expires.</param>
/// <param name="Candidate">An item actually returned in that recommendation.</param>
/// <param name="Reward">Completed, observed reward in [0,1]; never use for missing feedback.</param>
public sealed record RelevanceFeedback(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("recommendation")] string Recommendation,
    [property: JsonPropertyName("candidate")] string Candidate,
    [property: JsonPropertyName("reward")] double Reward);
