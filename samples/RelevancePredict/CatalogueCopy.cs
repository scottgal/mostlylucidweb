using System.Text.Json;

namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>Optional frozen LLM-written display copy. It cannot alter prices, categories, stock or training labels.</summary>
public static class CatalogueCopy
{
    /// <summary>Validates the complete overlay before applying it. Unknown IDs/fields and duplicate products fail closed.</summary>
    public static IReadOnlyList<FixtureProduct> Apply(IReadOnlyList<FixtureProduct> products, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        RequireFields(root, ["model", "promptVersion", "products"]);
        if (string.IsNullOrWhiteSpace(root.GetProperty("model").GetString()) || root.GetProperty("promptVersion").GetString() != "catalogue-copy-v1")
            throw new ArgumentException("Supply model provenance and promptVersion catalogue-copy-v1.");
        var known = products.ToDictionary(p => p.Id);
        var changes = new Dictionary<string, FixtureProduct>();
        foreach (var item in root.GetProperty("products").EnumerateArray())
        {
            RequireFields(item, ["id", "name", "description"]);
            var id = item.GetProperty("id").GetString() ?? "";
            var name = item.GetProperty("name").GetString() ?? "";
            var description = item.GetProperty("description").GetString() ?? "";
            if (!known.TryGetValue(id, out var original) || changes.ContainsKey(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || name.Length > 120 || description.Length > 600)
                throw new ArgumentException("Unknown/duplicate product ID or invalid copy length.");
            changes.Add(id, original with { Name = name, Description = description });
        }
        return products.Select(p => changes.GetValueOrDefault(p.Id, p)).ToArray();
    }
    private static void RequireFields(JsonElement value, string[] allowed)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected a JSON object.");
        var fields = value.EnumerateObject().Select(p => p.Name).ToArray();
        if (fields.Length != allowed.Length || fields.Distinct().Count() != fields.Length || fields.Except(allowed).Any())
            throw new ArgumentException("The copy overlay has missing, duplicate or unsupported fields.");
    }
}
