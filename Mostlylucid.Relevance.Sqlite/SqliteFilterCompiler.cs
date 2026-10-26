using System.Text.Json;

namespace Mostlylucid.Relevance.Sqlite;

/// <summary>A SQLite WHERE fragment and its bound parameters; expects candidate table alias c.</summary>
/// <param name="Sql">Predicate only; uses JSON functions and RELEVANCE_ORDINAL collation registered by the provider.</param>
/// <param name="Parameters">Parameter names and scalar values; never interpolate values into SQL.</param>
public sealed record CompiledFilter(string Sql, IReadOnlyDictionary<string, object> Parameters);

// Ordinary parameterised SQL, independent of any vector extension. Useful to a future SQL/sidecar adapter.
/// <summary>Compiles the portable filter tree to typed SQLite JSON comparisons; not a cross-database SQL dialect.</summary>
public static class SqliteFilterCompiler
{
    /// <summary>Compiles a supported predicate; callers must separately validate their permitted property paths.</summary>
    public static CompiledFilter Compile(Filter? filter)
    {
        var parameters = new Dictionary<string, object>();
        string Bind(object value)
        {
            var key = $"@f{parameters.Count}";
            parameters[key] = value;
            return key;
        }
        var remaining = 64;
        string Visit(Filter? node, int depth = 0)
        {
            if (depth > 16 || --remaining < 0) throw new ArgumentException("Filter complexity exceeds supported bounds.");
            return node switch
        {
            null => "1=1",
            Conjunction and => and.Filters.Count == 0 ? "1=1" : "(" + string.Join(" AND ", and.Filters.Select(child => Visit(child, depth + 1))) + ")",
            Comparison c => Compare(c),
            _ => throw new ArgumentException("Unsupported SQL filter node.")
        };
        }
        string Compare(Comparison c)
        {
            var path = Bind("$." + c.Field);
            var expression = $"json_extract(c.payload, {path})";
            var type = $"json_type(c.payload, {path})";
            var op = c.Operator switch
            {
                ComparisonOperator.Equal => "=",
                ComparisonOperator.LessOrEqual => "<=",
                ComparisonOperator.GreaterOrEqual => ">=",
                _ => throw new ArgumentException("Unsupported comparison.")
            };
            if (c.Operator != ComparisonOperator.Equal && c.Value.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Range filters require numbers.");
            return c.Value.ValueKind switch
            {
                JsonValueKind.String => $"({type} = 'text' AND {expression} COLLATE RELEVANCE_ORDINAL {op} {Bind(c.Value.GetString()!)})",
                JsonValueKind.Number when double.IsFinite(c.Value.GetDouble()) => $"({type} IN ('integer', 'real') AND {expression} {op} {Bind(c.Value.GetDouble())})",
                JsonValueKind.True => $"({type} = 'true')",
                JsonValueKind.False => $"({type} = 'false')",
                _ => throw new ArgumentException("Filter values must be non-null scalars.")
            };
        }
        return new CompiledFilter(Visit(filter), parameters);
    }
}
