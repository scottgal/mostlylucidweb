using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RELEVANCE_SQLITE_VEC")))
            Skip = "Set RELEVANCE_SQLITE_VEC to a pinned sqlite-vec extension to exercise the native path.";
    }
}

public sealed class NativeVectorTests
{
    [NativeFact]
    public async Task NativeSqliteVecFiltersBeforeTopKAndMatchesManagedDistances()
    {
        var definition = RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml")));
        var store = new SqliteRelevanceStore(Path.Combine(Path.GetTempPath(), $"relevance-native-{Guid.NewGuid():N}.db"),
            Environment.GetEnvironmentVariable("RELEVANCE_SQLITE_VEC"));
        await using var engine = new RelevanceEngine(definition, store);
        Assert.Equal("sqlite-vec exact SQL", store.SearchMode);
        await engine.SaveAsync("products", new { id = "a", category = "outdoors", price = 100, available = true });
        await engine.SaveAsync("products", new { id = "b", category = "electronics", price = 110, available = true });
        await engine.SaveAsync("products", new { id = "c", category = "outdoors", price = 250, available = true });
        await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "a", type = "view" });
        var result = await engine.RecommendAsync("s", "outdoors", Filter.Gte("price", 200), 1);
        Assert.Equal("c", Assert.Single(result.Items).Id);
        foreach (var pointName in new[] { "category", "price" })
        {
            await store.TransactAsync(tx =>
            {
                var candidate = tx.Get<Candidate>(definition.Id, "candidate", "a")!;
                var point = candidate.Points[pointName];
                var query = new VectorQuery(pointName, point.Signature, point.Metric, point.Values);
                var matches = tx.Nearest(definition.Id, query, null, 10);
                Assert.Equal(3, matches.Count);
                foreach (var match in matches)
                    Assert.Equal(VectorMath.Distance(point.Values, match.Candidate.Points[pointName].Values, point.Metric), match.Distance, 5);
                return true;
            });
        }
    }
}
