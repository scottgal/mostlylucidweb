using System.Text.Json;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class PackageTests
{
    internal static RelevanceDefinition Definition() => new()
    {
        Id = "package-test", Filters = ["available"],
        Streams = new() { ["items"] = new() { Kind = "candidate" }, ["events"] = new() { Kind = "observation", Weights = new() { ["view"] = 1 } } },
        Points = new() { ["p"] = new() { Path = "vector", Converter = "vector", Dimensions = 2 } },
        Placements = new() { ["next"] = new() { Heads = new() { ["p"] = 1 } } }
    };

    [Fact]
    public async Task SpacesIsolateTheSameCandidateAndSubjectIdentitiesAndSurviveReopen()
    {
        var directory = Path.Combine(Path.GetTempPath(), "relevance-hub-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteRelevanceFactory(directory, ["one", "two"]);
            var first = new RelevanceSpace("tenant-a", "products");
            var second = new RelevanceSpace("tenant-b", "products");
            var email = new RelevanceSpace("tenant-a", "emails");
            await using (var hub = new RelevanceHub(factory))
            {
                foreach (var (space, value) in new[] { (first, .1), (second, .9), (email, .5) })
                {
                    var engine = await hub.OpenAsync(space, Definition());
                    await engine.SaveAsync("items", new { id = "same-id", vector = new[] { value, 0.0 }, available = true });
                    await engine.SaveAsync("events", new { id = "same-event", subject = "same-subject", candidate = "same-id", type = "view" });
                    Assert.Same(engine, await hub.OpenAsync(space, Definition()));
                }
                Assert.Equal(2, Enumerable.Range(0, 64).Select(i => hub.ShardFor(new("tenant-" + i, "products"))).Distinct().Count());
                var changed = Definition(); changed.Version++;
                await Assert.ThrowsAsync<ArgumentException>(async () => await hub.OpenAsync(first, changed));
            }
            await using var reopened = new RelevanceHub(factory);
            foreach (var (space, value) in new[] { (first, .1), (second, .9), (email, .5) })
            {
                var result = await (await reopened.OpenAsync(space, Definition())).RecommendAsync("same-subject", "next", count: 1);
                Assert.Equal(value, Assert.Single(result.Items).Data.GetProperty("vector")[0].GetDouble());
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void TopologyIsOrderIndependentAndChangesRequireMigration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "relevance-topology-" + Guid.NewGuid().ToString("N"));
        try
        {
            var a = new SqliteRelevanceFactory(directory, ["one", "two"]);
            var b = new SqliteRelevanceFactory(directory, ["two", "one"]);
            Assert.Equal(a.Shards, b.Shards);
            Assert.Throws<InvalidOperationException>(() => new SqliteRelevanceFactory(directory, ["one", "two", "three"]));
            Assert.Throws<ArgumentException>(() => new SqliteRelevanceFactory(directory, ["../escape"]));
            Assert.NotEqual(new RelevanceSpace("a:b", "c").Key, new RelevanceSpace("a", "b:c").Key);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task RelatedUsesOneCompatibleSpaceAndExcludesSeed()
    {
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore());
        await engine.SaveAsync("items", new { id = "a", vector = new[] { 0.0, 0.0 }, available = true });
        await engine.SaveAsync("items", new { id = "b", vector = new[] { .1, 0.0 }, available = false });
        await engine.SaveAsync("items", new { id = "c", vector = new[] { .9, 0.0 }, available = true });
        Assert.Equal("b", Assert.Single(await engine.RelatedAsync("a", "p", count: 1)).Candidate.Id);
        Assert.Equal("c", Assert.Single(await engine.RelatedAsync("a", "p", Filter.Eq("available", true), 1)).Candidate.Id);
    }
}

public sealed class HnswFactAttribute : FactAttribute
{
    public HnswFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RELEVANCE_SQLITE_HNSW")))
            Skip = "Set RELEVANCE_SQLITE_HNSW to sqlite-vector-rs v0.3.1 to test the SQLite-owned graph.";
    }
}

public sealed class SqliteHnswTests
{
    private static SqliteRelevanceStore Open(string path) => new(path, vectorOptions: new()
        { ExtensionPath = Environment.GetEnvironmentVariable("RELEVANCE_SQLITE_HNSW")! });
    private static Candidate Item(string id, float[] vector, bool available = true) => new(id,
        JsonSerializer.SerializeToElement(new { id, available }), new() { ["p"] = new("v1", "l2", vector) });

    [HnswFact]
    public async Task GraphUpdatesRollBackAndPersistWithTheAuthoritativeRows()
    {
        var directory = Directory.CreateTempSubdirectory("relevance-hnsw-");
        var path = Path.Combine(directory.FullName, "test.db");
        var query = new VectorQuery("p", "v1", "l2", [0, 0]);
        try
        {
            await using (var store = Open(path))
            {
                await store.TransactAsync(tx => { tx.Upsert("one", Item("a", [.1f, 0])); tx.Upsert("one", Item("b", [.9f, 0])); tx.Upsert("two", Item("a", [0, 0])); return true; });
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.TransactAsync<bool>(tx =>
                { tx.Upsert("one", Item("a", [1, 1])); tx.Upsert("one", Item("ghost", [0, 0])); tx.Upsert("aborted-space", Item("new-table", [0, 0])); throw new InvalidOperationException("rollback"); }));
                await store.TransactAsync(tx => { tx.Upsert("aborted-space", Item("recovered-table", [0, 0])); return true; });
                Assert.Equal("recovered-table", Assert.Single(await store.TransactAsync(tx => tx.Nearest("aborted-space", query, null, 10))).Candidate.Id);
                await using (var competing = Open(path))
                    await Assert.ThrowsAsync<InvalidOperationException>(async () => await competing.TransactAsync(tx => tx.Nearest("one", query, null, 1)));
                var first = await store.TransactAsync(tx => tx.Nearest("one", query, null, 10));
                Assert.Equal(new[] { "a", "b" }, first.Select(n => n.Candidate.Id));
                Assert.Equal(.1, first[0].Distance, 5); // L2, not squared L2.
                await store.TransactAsync(tx => { tx.Upsert("one", Item("a", [1, 1])); return true; });
                Assert.Equal("b", (await store.TransactAsync(tx => tx.Nearest("one", query, null, 1))).Single().Candidate.Id);
            }
            await using var reopened = Open(path);
            var results = await reopened.TransactAsync(tx => tx.Nearest("one", query, null, 10));
            Assert.Equal(new[] { "b", "a" }, results.Select(n => n.Candidate.Id));
        }
        finally { directory.Delete(true); }
    }

    [HnswFact]
    public async Task ApproximateRecallIsCheckedAgainstExactSqlAndNarrowFiltersRemainEligible()
    {
        var directory = Directory.CreateTempSubdirectory("relevance-hnsw-recall-");
        try
        {
            await using var store = Open(Path.Combine(directory.FullName, "test.db"));
            var random = new Random(814);
            await store.TransactAsync(tx =>
            {
                for (var i = 0; i < 300; i++) tx.Upsert("one", Item(i.ToString("D3"), Enumerable.Range(0, 8).Select(_ => (float)random.NextDouble()).ToArray(), i == 299));
                return true;
            });
            var overlap = 0;
            for (var i = 0; i < 20; i++)
            {
                var query = new VectorQuery("p", "v1", "l2", Enumerable.Range(0, 8).Select(_ => (float)random.NextDouble()).ToArray());
                var approx = await store.TransactAsync(tx => tx.Nearest("one", query, null, 10));
                var exact = await store.TransactAsync(tx => tx.Nearest("one", query, Filter.Eq("available", true), 1));
                Assert.Equal("299", Assert.Single(exact).Candidate.Id);
                var truth = await store.TransactAsync(tx => tx.Eligible("one", null, 1000).Select(c =>
                    new Neighbor(c, VectorMath.Distance(c.Points["p"].Values, query.Values, "l2"))).OrderBy(n => n.Distance).Take(10).Select(n => n.Candidate.Id).ToArray());
                overlap += approx.Count(n => truth.Contains(n.Candidate.Id));
            }
            Assert.True(overlap >= 190, $"Recall@10 was {overlap / 200.0:P1}, below 95% for this fixture.");
        }
        finally { directory.Delete(true); }
    }
}

public sealed class PackageBoundaryTests
{
    [Fact]
    public void JsonConfigurationRejectsTyposAndAcceptsConventionalCasing()
    {
        Assert.Equal("tenant", RelevanceDefinition.FromJson("{\"id\":\"tenant\"}").Id);
        Assert.Throws<JsonException>(() => RelevanceDefinition.FromJson("{\"unexpected_option\":true}"));
    }

    [Fact]
    public async Task ExcessivePayloadAndFilterDepthRejectWithoutStoppingTheWorker()
    {
        await using var engine = new RelevanceEngine(PackageTests.Definition(), new MemoryRelevanceStore());
        using var oversized = JsonDocument.Parse(JsonSerializer.Serialize(new { id = "large", text = new string('x', 1_048_576) }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("items", oversized.RootElement));
        Filter nested = Filter.Eq("available", true);
        for (var i = 0; i < 20; i++) nested = Filter.All(nested);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RecommendAsync("s", "next", nested, 1));
        Assert.Throws<ArgumentException>(() => SqliteFilterCompiler.Compile(nested));
        await engine.SaveAsync("items", new { id = "safe", vector = new[] { .1, .2 }, available = true });
        Assert.Equal("safe", Assert.Single((await engine.RecommendAsync("s", "next", count: 1)).Items).Id);
    }

    [Fact]
    public async Task MandatoryRulesCannotBeWeakenedAndSqlTextRemainsData()
    {
        var directory = Directory.CreateTempSubdirectory("relevance-boundary-");
        try
        {
            var definition = PackageTests.Definition();
            definition.Placements["next"].Where["available"] = true;
            await using var engine = new RelevanceEngine(definition, new SqliteRelevanceStore(Path.Combine(directory.FullName, "data.db")));
            await engine.SaveAsync("items", new { id = "hidden", vector = new[] { .1, .2 }, available = false });
            Assert.Empty((await engine.RecommendAsync("s", "next", Filter.Eq("available", false), 1)).Items);
            await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RecommendAsync("s", "next", Filter.Eq("available') OR 1=1 --", true), 1));
            var compiled = SqliteFilterCompiler.Compile(Filter.Eq("label", "' OR 1=1; DROP TABLE relevance_candidates; --"));
            Assert.DoesNotContain("DROP TABLE", compiled.Sql);
            Assert.Contains(compiled.Parameters.Values, value => value is string text && text.Contains("DROP TABLE"));
        }
        finally { directory.Delete(true); }
    }
}
