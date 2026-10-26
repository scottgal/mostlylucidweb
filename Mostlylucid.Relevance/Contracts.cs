using System.Text.Json;

namespace Mostlylucid.Relevance;

/// <summary>Confirms completed ingestion; candidates upsert and are never reported as duplicate events.</summary>
/// <param name="Id">The input's mapped candidate/event ID.</param>
/// <param name="Duplicate">True when the same event and content were already accepted within retention.</param>
public sealed record SaveReceipt(string Id, bool Duplicate = false);
/// <summary>Explains one retrieval head's contribution to a fused recommendation score.</summary>
/// <param name="Head">Configured point/head name.</param>
/// <param name="Rank">One-based position in this head's eligible candidate list.</param>
/// <param name="Weight">Configured weight multiplied by the head's current reliability heuristic.</param>
/// <param name="Distance">Raw distance in this head's representation; lower is closer, not more probable.</param>
public sealed record Contribution(string Head, int Rank, double Weight, double Distance);
/// <summary>A candidate with a ranking score and transparent contributing evidence.</summary>
/// <param name="Id">Candidate ID.</param>
/// <param name="Score">Weighted RRF score, or zero for the cold-start fallback; not a probability.</param>
/// <param name="Data">Candidate data as stored at decision time.</param>
/// <param name="Reasons">Supporting head ranks, weights and distances; empty for fallback results.</param>
public sealed record Recommendation(string Id, double Score, JsonElement Data, IReadOnlyList<Contribution> Reasons);
/// <summary>A retained decision used to attribute subsequent candidate feedback.</summary>
/// <param name="Id">Generated decision ID; include this in feedback before decision retention expires.</param>
/// <param name="Subject">Entity/session for which this decision was made.</param>
/// <param name="Placement">Configured recommendation surface.</param>
/// <param name="Items">Eligible items in descending score order, with ordinal candidate-ID tie breaking.</param>
public sealed record RecommendationResult(string Id, string Subject, string Placement, IReadOnlyList<Recommendation> Items);

/// <summary>Portable constraints on declared scalar metadata fields, applied before retrieval truncation.</summary>
/// <remarks>Property paths and strings are case-sensitive. Supported nodes are comparisons and conjunctions.</remarks>
public abstract record Filter
{
    /// <summary>Requires scalar equality without coercing numbers, strings or booleans across types.</summary>
    public static Filter Eq(string field, object value) => new Comparison(field, ComparisonOperator.Equal, RelevanceJson.Scalar(value));
    /// <summary>Requires a numeric property less than or equal to the inclusive bound.</summary>
    public static Filter Lte(string field, double value) => new Comparison(field, ComparisonOperator.LessOrEqual, RelevanceJson.Scalar(value));
    /// <summary>Requires a numeric property greater than or equal to the inclusive bound.</summary>
    public static Filter Gte(string field, double value) => new Comparison(field, ComparisonOperator.GreaterOrEqual, RelevanceJson.Scalar(value));
    /// <summary>Combines constraints with AND; an empty conjunction imposes no additional restriction.</summary>
    public static Filter All(params Filter[] filters) => new Conjunction(filters);

    // Records do not make a caller-owned list immutable. Freeze nested predicates before queue admission.
    internal static Filter? Snapshot(Filter? filter)
    {
        var remaining = 64;
        return Copy(filter, 0);
        Filter? Copy(Filter? node, int depth)
        {
            if (node is null) return null;
            if (depth > 16 || --remaining < 0) throw new ArgumentException("Filters are limited to 64 nodes and 16 nested levels.");
            return node switch
            {
                Comparison comparison => comparison with { Value = comparison.Value.Clone() },
                Conjunction and => new Conjunction(and.Filters.Select(child => Copy(child, depth + 1)
                    ?? throw new ArgumentException("Null filter child.")).ToArray()),
                _ => throw new ArgumentException("Unsupported filter node.")
            };
        }
    }

}
/// <summary>The supported scalar comparisons; range operators are numeric only.</summary>
public enum ComparisonOperator
{
    /// <summary>Exact scalar equality with matching JSON types.</summary>
    Equal,
    /// <summary>Inclusive numeric upper bound.</summary>
    LessOrEqual,
    /// <summary>Inclusive numeric lower bound.</summary>
    GreaterOrEqual
}
/// <summary>A typed scalar predicate; engines validate its field and value before use.</summary>
/// <param name="Field">Declared dotted metadata property path.</param>
/// <param name="Operator">Equality or an inclusive numeric bound.</param>
/// <param name="Value">Non-null finite number, string or boolean; range bounds must be numbers.</param>
public sealed record Comparison(string Field, ComparisonOperator Operator, JsonElement Value) : Filter;
/// <summary>Requires every contained predicate to match.</summary>
/// <param name="Filters">Child predicates, recursively combined with AND.</param>
public sealed record Conjunction(IReadOnlyList<Filter> Filters) : Filter;

/// <summary>Turns a configured input property into a stable, finite vector representation.</summary>
/// <remarks>
/// Converters may run asynchronously on the engine worker. Return owned coordinate arrays and do not retain or
/// mutate the supplied definition. Shared converter instances must support calls from independent engines.
/// The host owns converter resources; the engine does not dispose them.
/// </remarks>
public interface IPointConverter
{
    /// <summary>Unique, case-sensitive name selected by YAML; cannot shadow a built-in converter.</summary>
    string Name { get; }
    /// <summary>Identifies representation semantics, including model, tokenizer, pooling and preprocessing versions.</summary>
    /// <remarks>Changing semantics with the same dimensions still requires a new version.</remarks>
    string Version { get; }
    /// <summary>Validates point-specific settings and declares the output length before accepting observations.</summary>
    int Dimensions(PointDefinition point);
    /// <summary>Encodes one property using the declared dimensions and semantics.</summary>
    /// <param name="value">The selected JSON property, not the whole input unless that property is structured.</param>
    /// <param name="point">Frozen point settings, to be treated as read-only.</param>
    /// <param name="cancellationToken">Cancellation before the enclosing save commits.</param>
    /// <returns>A new finite vector; cosine representations must be nonzero.</returns>
    ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default);
}

/// <summary>Provider boundary for atomic state, candidate, attribution and deduplication updates.</summary>
/// <remarks>The engine owns the store lifetime after successful construction.</remarks>
public interface IRelevanceStore : IAsyncDisposable
{
    /// <summary>Runs a synchronous, isolated callback and commits all changes atomically.</summary>
    /// <typeparam name="T">Detached result type returned by the callback.</typeparam>
    /// <param name="action">Callback that must not await, retain the transaction, or re-enter this store.</param>
    /// <param name="cancellationToken">Cancellation before admission or commit; cancellation cannot undo a commit.</param>
    /// <returns>The detached callback result after commit succeeds.</returns>
    /// <remarks>Providers roll back every change on failure. Return detached values, never live provider objects.</remarks>
    ValueTask<T> TransactAsync<T>(Func<IRelevanceTransaction, T> action, CancellationToken cancellationToken = default);
}

/// <summary>Operations valid only inside an IRelevanceStore transaction callback.</summary>
/// <remarks>Providers must preserve scope isolation and return detached values. Expiry is enforced by Prune.</remarks>
public interface IRelevanceTransaction
{
    /// <summary>Binds an engine scope to a representation/configuration fingerprint, rejecting incompatible reuse.</summary>
    void BindSchema(string scope, string fingerprint);
    /// <summary>Records a deduplication token; returns false for the same hash, throws on conflicting reuse.</summary>
    bool Accept(string scope, string id, string hash, DateTimeOffset expiresAt);
    /// <summary>Reads a detached typed record, or the default value if absent; does not prune expired records.</summary>
    T? Get<T>(string scope, string kind, string id);
    /// <summary>Replaces a typed record and its absolute expiry within a scope and record kind.</summary>
    void Put<T>(string scope, string kind, string id, T value, DateTimeOffset expiresAt);
    /// <summary>Counts records of a kind within a scope, including unpruned records.</summary>
    int Count(string scope, string kind);
    /// <summary>Atomically replaces a candidate and all of its named vectors; candidates do not expire.</summary>
    void Upsert(string scope, Candidate candidate);
    /// <summary>Returns eligible compatible neighbours, ordered by distance. Approximate providers may trade recall for latency.</summary>
    /// <remarks>Use ordinal candidate-ID order to break returned ties. Match the query's point signature and metric; never drop eligibility constraints.</remarks>
    IReadOnlyList<Neighbor> Nearest(string scope, VectorQuery query, Filter? filter, int limit);
    /// <summary>Returns matching candidates in ordinal ID order before taking the limit; used for cold starts.</summary>
    IReadOnlyList<Candidate> Eligible(string scope, Filter? filter, int limit);
    /// <summary>Deletes expiring records at or before the supplied UTC instant; retains candidates and schema bindings.</summary>
    void Prune(DateTimeOffset now);
}

/// <summary>A versioned candidate representation in one named feature space.</summary>
/// <param name="Signature">Fingerprint of converter semantics and point configuration.</param>
/// <param name="Metric">Distance metric: l2 or cosine.</param>
/// <param name="Values">Finite coordinates of the declared length.</param>
public sealed record EncodedPoint(string Signature, string Metric, float[] Values);
/// <summary>Stored candidate metadata and its independently comparable feature spaces.</summary>
/// <param name="Id">Candidate's stable ID within an engine scope.</param>
/// <param name="Data">Original serialized metadata, including declared filter properties.</param>
/// <param name="Points">Named vectors; arrays must not be shared with mutable provider state.</param>
public sealed record Candidate(string Id, JsonElement Data, Dictionary<string, EncodedPoint> Points);
/// <summary>A query in one specific versioned representation.</summary>
/// <param name="Point">Configured point name.</param>
/// <param name="Signature">Required representation fingerprint.</param>
/// <param name="Metric">Distance metric matching the stored point.</param>
/// <param name="Values">Current entity mean in that point's coordinates.</param>
public sealed record VectorQuery(string Point, string Signature, string Metric, float[] Values);
/// <summary>An eligible candidate and its distance in one compatible representation.</summary>
/// <param name="Candidate">Detached stored candidate.</param>
/// <param name="Distance">Finite distance, lower meaning closer within this representation.</param>
public sealed record Neighbor(Candidate Candidate, double Distance);

/// <summary>Reference JSON path and predicate semantics for converters and storage providers.</summary>
public static class DataValues
{
    // Deliberately small path language: dotted object properties, no expressions or code.
    /// <summary>Reads a case-sensitive dotted object-property path, throwing if any component is missing.</summary>
    public static JsonElement Read(JsonElement data, string path)
    {
        return TryRead(data, path, out var value) ? value : throw new ArgumentException($"Missing input property '{path}'.");
    }

    /// <summary>Attempts a dotted object-property lookup; array indexing and expressions are not supported.</summary>
    public static bool TryRead(JsonElement data, string path, out JsonElement value)
    {
        value = data;
        foreach (var part in path.Split('.'))
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value)) return false;
        return true;
    }

    /// <summary>Evaluates supported scalar predicates using the same typed comparison rules as the SQL provider.</summary>
    public static bool Matches(JsonElement data, Filter? filter) => filter switch
    {
        null => true,
        Conjunction and => and.Filters.All(f => Matches(data, f)),
        Comparison c => TryRead(data, c.Field, out var actual) && Compare(actual, c.Value, c.Operator),
        _ => throw new ArgumentException("Unsupported filter node.")
    };

    private static bool Compare(JsonElement actual, JsonElement expected, ComparisonOperator op)
    {
        if (op == ComparisonOperator.Equal)
            return actual.ValueKind == expected.ValueKind && actual.ValueKind switch
            {
                JsonValueKind.String => actual.GetString() == expected.GetString(),
                JsonValueKind.Number => actual.GetDouble() == expected.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => actual.GetBoolean() == expected.GetBoolean(),
                _ => throw new ArgumentException("Filter values must be strings, finite numbers or booleans.")
            };
        if (expected.ValueKind != JsonValueKind.Number)
            throw new ArgumentException("Range filters require numeric bounds.");
        // Match SQL's type guard: a text property is ineligible for a numeric range, not a provider error.
        if (actual.ValueKind != JsonValueKind.Number) return false;
        return op == ComparisonOperator.LessOrEqual
            ? actual.GetDouble() <= expected.GetDouble()
            : actual.GetDouble() >= expected.GetDouble();
    }
}
