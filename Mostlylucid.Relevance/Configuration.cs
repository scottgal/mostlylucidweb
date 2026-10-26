using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mostlylucid.Relevance;

/// <summary>Versioned configuration copied by the engine at construction; use a new identity or migration when semantics change.</summary>
public sealed class RelevanceDefinition
{
    /// <summary>Storage scope shared by this configuration, its entities, candidates and pooled transition counts.</summary>
    public string Id { get; set; } = "";
    /// <summary>Positive configuration revision included in the schema fingerprint.</summary>
    public int Version { get; set; } = 1;
    /// <summary>Elapsed minutes for old positive evidence to lose half its weight; must be positive.</summary>
    public double HalfLifeMinutes { get; set; } = 30;
    /// <summary>Minimum decayed event mass needed to use a point for recommendations; not a confidence probability.</summary>
    public double MinEvidence { get; set; } = 0.01;
    /// <summary>Retention for entity state, event deduplication and active model records. Counts are cumulative while retained.</summary>
    public int StateRetentionHours { get; set; } = 24;
    /// <summary>How long a decision remains available for attributed feedback.</summary>
    public int DecisionRetentionHours { get; set; } = 24;
    /// <summary>Hard cap on active entity summaries; overflow rejects and rolls back an update.</summary>
    public int MaxStateRecords { get; set; } = 10_000;
    /// <summary>Hard cap on stored candidates; overflow rolls back the upsert.</summary>
    public int MaxCandidates { get; set; } = 100_000;
    /// <summary>Hard cap on retained recommendation decisions; overflow rejects the read/decision transaction.</summary>
    public int MaxDecisions { get; set; } = 10_000;
    /// <summary>Hard cap on event and attributed-outcome deduplication tokens.</summary>
    public int MaxDeduplicationRecords { get; set; } = 100_000;
    /// <summary>Per-head retrieval window and maximum requested result count, from 1 to 10,000.</summary>
    public int CandidateLimit { get; set; } = 100;
    /// <summary>Positive denominator offset in weighted reciprocal rank fusion; larger values flatten rank differences.</summary>
    public int RrfConstant { get; set; } = 60;
    /// <summary>Optional observed states and absorbing outcomes used by destination prediction.</summary>
    public DestinationDefinition? Destinations { get; set; }
    /// <summary>Allowed case-sensitive dotted scalar metadata paths; every candidate must supply them.</summary>
    public List<string> Filters { get; set; } = [];
    /// <summary>Named candidate, observation and feedback input routes.</summary>
    public Dictionary<string, StreamDefinition> Streams { get; set; } = [];
    /// <summary>Named independent vector representations and their converters.</summary>
    public Dictionary<string, PointDefinition> Points { get; set; } = [];
    /// <summary>Named recommendation surfaces, each with mandatory rules and ranking heads.</summary>
    public Dictionary<string, PlacementDefinition> Placements { get; set; } = [];

    /// <summary>Parses strict YAML, rejecting duplicate and unknown keys; call Validate before using the result.</summary>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("YAML uses reflection. Use FromJson or a typed definition for Native AOT.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("YAML uses runtime type discovery. Use FromJson for Native AOT.")]
    public static RelevanceDefinition FromYaml(string yaml) => new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithDuplicateKeyChecking()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build().Deserialize<RelevanceDefinition>(yaml) ?? throw new ArgumentException("Empty configuration.");

    /// <summary>Loads configuration using generated JSON metadata; supported in Native AOT.</summary>
    public static RelevanceDefinition FromJson(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > 1_048_576) throw new ArgumentException("Configuration exceeds 1 MiB.");
        var options = new JsonSerializerOptions(CoreJsonContext.Default.Options)
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };
        return JsonSerializer.Deserialize(json, new CoreJsonContext(options).RelevanceDefinition)
            ?? throw new ArgumentException("Empty configuration.");
    }

    /// <summary>Checks identity, ranges, paths, routes, heads and destinations; converter validation occurs at engine construction.</summary>
    /// <exception cref="ArgumentException">A configuration value is inconsistent or unsupported.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || Version < 1 || !double.IsFinite(HalfLifeMinutes) || HalfLifeMinutes <= 0
            || !double.IsFinite(MinEvidence) || MinEvidence <= 0 || StateRetentionHours < 1 || DecisionRetentionHours < 1
            || MaxStateRecords < 1 || MaxCandidates < 1 || MaxDecisions < 1 || MaxDeduplicationRecords < 1
            || CandidateLimit is < 1 or > 10_000 || RrfConstant < 1)
            throw new ArgumentException("Invalid engine identity, bounds, retention or decay settings.");
        if (Points.Count == 0 || Placements.Count == 0 || Streams.Count == 0)
            throw new ArgumentException("Configure points, streams and placements.");
        foreach (var path in Filters.Concat(Points.Values.Select(p => p.Path))) ValidatePath(path);
        if (Filters.Distinct(StringComparer.Ordinal).Count() != Filters.Count)
            throw new ArgumentException("Duplicate filter field.");
        foreach (var stream in Streams.Values)
        {
            if (stream.Kind is not ("candidate" or "observation" or "feedback")) throw new ArgumentException("Unknown stream kind.");
            if (stream.PointSource is not ("candidate" or "input")) throw new ArgumentException("Unknown observation point source.");
            foreach (var path in stream.Fields.Values) ValidatePath(path);
            if (stream.Weights.Values.Any(w => !double.IsFinite(w) || w < 0)) throw new ArgumentException("Event weights cannot be negative.");
        }
        foreach (var point in Points.Values)
            if (point.Metric is not ("cosine" or "l2") || !double.IsFinite(point.Scale) || point.Scale <= 0)
                throw new ArgumentException("Unsupported point metric or invalid scale.");
        foreach (var placement in Placements.Values)
        {
            if (placement.Heads.Count == 0) throw new ArgumentException("A placement needs at least one head.");
            foreach (var (point, weight) in placement.Heads)
                if (!Points.ContainsKey(point) || !double.IsFinite(weight) || weight <= 0)
                    throw new ArgumentException($"Invalid placement head '{point}'.");
            ValidateFilter(placement.BuildFilter());
        }
        Destinations?.Validate();
        if (Destinations?.Trajectory is { } trajectory && trajectory.HorizonSeconds > StateRetentionHours * 3600.0)
            throw new ArgumentException("The trajectory forecast horizon cannot exceed episode state retention.");
    }

    /// <summary>Rejects undeclared fields, unsupported operators and non-scalar or non-finite values.</summary>
    public void ValidateFilter(Filter? filter)
    {
        filter = Filter.Snapshot(filter);
        ValidateNode(filter);
        void ValidateNode(Filter? node)
        {
            switch (node)
            {
                case null: return;
                case Conjunction and:
                    foreach (var child in and.Filters) ValidateNode(child);
                    return;
                case Comparison c:
                    if (!Filters.Contains(c.Field, StringComparer.Ordinal)) throw new ArgumentException($"Filter field '{c.Field}' is not declared.");
                    if (!Enum.IsDefined(c.Operator) || c.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
                        throw new ArgumentException("Unsupported filter operation or value.");
                    if (c.Value.ValueKind == JsonValueKind.Number && !double.IsFinite(c.Value.GetDouble())) throw new ArgumentException("Filter value must be finite.");
                    if (c.Operator != ComparisonOperator.Equal && c.Value.ValueKind != JsonValueKind.Number) throw new ArgumentException("Numeric range required.");
                    return;
                default: throw new ArgumentException("Unsupported filter.");
            }
        }
    }

    internal static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(RelevanceJson.Serialize(value))));
    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 128 || !Regex.IsMatch(path, @"\A[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*\z"))
            throw new ArgumentException($"Invalid property path '{path}'.");
    }
}

/// <summary>Maps one input route to candidate upserts, observations or attributed feedback.</summary>
public sealed class StreamDefinition
{
    /// <summary>Route kind: candidate, observation or feedback.</summary>
    public string Kind { get; set; } = "";
    /// <summary>For observations, candidate reuses a stored candidate vector; input converts properties on the observation.</summary>
    public string PointSource { get; set; } = "candidate";
    /// <summary>Maps envelope fields such as subject or candidate to existing dotted property paths.</summary>
    public Dictionary<string, string> Fields { get; set; } = [];
    /// <summary>Non-negative observation-type weights. Zero updates sequence state without adding positive affinity.</summary>
    public Dictionary<string, double> Weights { get; set; } = [];
    /// <summary>Returns the mapped envelope property path, defaulting to the conventional field name.</summary>
    public string Path(string field) => Fields.GetValueOrDefault(field, field);
}

/// <summary>Describes the semantics of one independently compared feature space.</summary>
public sealed class PointDefinition
{
    /// <summary>Case-sensitive dotted property selected from the candidate or raw observation.</summary>
    public string Path { get; set; } = "";
    /// <summary>Registered converter name, such as numeric, formula, categorical, vector or rgb-oklab.</summary>
    public string Converter { get; set; } = "numeric";
    /// <summary>Distance metric: l2 or cosine. It must fit the representation semantics.</summary>
    public string Metric { get; set; } = "l2";
    /// <summary>Human-readable meaning and units; descriptive metadata, not a formula or learned interpretation.</summary>
    public string Displays { get; set; } = "";
    /// <summary>Positive divisor used by the numeric converter; ignored by converters that define their own scaling.</summary>
    public double Scale { get; set; } = 1;
    /// <summary>Ordered vocabulary for categorical one-hot encoding; changing its order changes the representation.</summary>
    public List<string> Categories { get; set; } = [];
    /// <summary>Declared output length for the precomputed vector converter.</summary>
    public int Dimensions { get; set; }
    /// <summary>Bounded scalar formula over x; supports arithmetic and abs/min/max/clamp, not arbitrary code.</summary>
    public string Expression { get; set; } = "";
}

/// <summary>Finite-horizon transition baseline settings; observed counts are pooled by engine ID.</summary>
public sealed class DestinationDefinition
{
    /// <summary>Optional elapsed-time trajectory estimator; omitted configurations retain the event-count Markov baseline.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public TrajectoryDefinition? Trajectory { get; set; }
    /// <summary>Distinct observed state vocabulary, from 2 to 64 entries.</summary>
    public List<string> States { get; set; } = [];
    /// <summary>Non-empty subset of states treated as absorbing destinations during a prediction.</summary>
    public List<string> Outcomes { get; set; } = [];
    /// <summary>Number of future transitions to propagate, from 1 to 256; not a duration in seconds.</summary>
    public int HorizonEvents { get; set; } = 8;
    /// <summary>Minimum outgoing observations for a row to propagate probability; unsupported mass remains unresolved.</summary>
    public int MinTransitions { get; set; } = 10;
    /// <summary>Non-negative symmetric pseudo-count added to each target in a supported row.</summary>
    public double Smoothing { get; set; } = 0.5;
    /// <summary>Rejects inconsistent vocabularies, outcomes, support thresholds or probability settings.</summary>
    public void Validate()
    {
        if (States.Count is < 2 or > 64 || States.Any(string.IsNullOrWhiteSpace) || States.Distinct().Count() != States.Count
            || Outcomes.Count == 0 || Outcomes.Distinct().Count() != Outcomes.Count || Outcomes.Any(o => !States.Contains(o))
            || HorizonEvents is < 1 or > 256 || MinTransitions < 1 || !double.IsFinite(Smoothing) || Smoothing < 0)
            throw new ArgumentException("Invalid destination states, outcomes, horizon or probability settings.");
        Trajectory?.Validate(this);
    }
}

/// <summary>A recommendation surface with configured evidence contributors and mandatory eligibility rules.</summary>
public sealed class PlacementDefinition
{
    /// <summary>Point names and positive base weights for rank fusion; independent of observation event weights.</summary>
    public Dictionary<string, double> Heads { get; set; } = [];
    /// <summary>Mandatory scalar equalities, combined with caller filters using AND.</summary>
    public Dictionary<string, object> Where { get; set; } = [];
    /// <summary>Builds the mandatory equality conjunction, or null if there are no placement restrictions.</summary>
    public Filter? BuildFilter() => Where.Count == 0 ? null : new Conjunction(Where.Select(p => (Filter)new Comparison(p.Key, ComparisonOperator.Equal, RelevanceJson.Scalar(p.Value))).ToArray());
}
