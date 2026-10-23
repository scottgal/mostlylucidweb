using System.Text.Json;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class EngineTests
{
    private static RelevanceDefinition Definition() => RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml")));
    private static IRelevanceStore Store(bool sqlite) => sqlite
        ? new SqliteRelevanceStore(Path.Combine(Path.GetTempPath(), $"relevance-test-{Guid.NewGuid():N}.db"))
        : new MemoryRelevanceStore();
    private static async Task Products(RelevanceEngine engine)
    {
        await engine.SaveAsync("products", new { id = "a", category = "outdoors", price = 100, available = true });
        await engine.SaveAsync("products", new { id = "b", category = "outdoors", price = 110, available = true });
        await engine.SaveAsync("products", new { id = "c", category = "electronics", price = 105, available = true });
        await engine.SaveAsync("products", new { id = "d", category = "outdoors", price = 99, available = false });
        await engine.SaveAsync("products", new { id = "e", category = "outdoors", price = 250, available = true });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlacementsAndFiltersApplyBeforeEachHeadTruncates(bool sqlite)
    {
        var definition = Definition();
        definition.CandidateLimit = 1;
        await using var engine = new RelevanceEngine(definition, Store(sqlite));
        await Products(engine);
        await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "c", type = "view" });
        var result = await engine.RecommendAsync("s", "outdoors", Filter.Gte("price", 200), 1);
        var item = Assert.Single(result.Items);
        Assert.Equal("e", item.Id);
        Assert.Equal(2, item.Reasons.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveIsImmediatelyVisibleAndMultipleHeadsExplainRank(bool sqlite)
    {
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite));
        await Products(engine);
        await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "a", type = "view" });
        var before = await engine.RecommendAsync("s", "home");
        Assert.Equal("a", before.Items[0].Id);
        Assert.DoesNotContain(before.Items, i => i.Id == "d");
        await engine.SaveAsync("events", new { id = "p", subject = "s", candidate = "c", type = "purchase" });
        var after = await engine.RecommendAsync("s", "home");
        Assert.Equal("c", after.Items[0].Id);
        Assert.Equal(new[] { "category", "price" }, after.Items[0].Reasons.Select(r => r.Head));
        Assert.Equal(after.Items[0].Reasons.Sum(r => r.Weight / (60 + r.Rank)), after.Items[0].Score, 10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateEventsDoNotAccumulateAndConflictsDoNotDamageWorker(bool sqlite)
    {
        var store = Store(sqlite);
        await using var engine = new RelevanceEngine(Definition(), store);
        await Products(engine);
        var observation = new { id = "v", subject = "s", candidate = "a", type = "view" };
        await engine.SaveAsync("events", observation);
        Assert.True((await engine.SaveAsync("events", observation)).Duplicate);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "c", type = "view" }));
        var state = await store.TransactAsync(tx => tx.Get<SubjectState>("shop-demo-v1", "state", "s"));
        Assert.Equal(1, state!.Points["price"].Mass);
        Assert.Equal("a", (await engine.RecommendAsync("s", "home")).Items[0].Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FeedbackAdaptsWeightsAndCannotBeCountedTwice(bool sqlite)
    {
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite));
        await Products(engine);
        await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "a", type = "view" });
        var shown = await engine.RecommendAsync("s", "home");
        await engine.SaveAsync("outcomes", new { id = "f", recommendation = shown.Id, candidate = "b", reward = 1.0 });
        var updated = await engine.RecommendAsync("s", "home");
        Assert.All(updated.Items.Single(i => i.Id == "b").Reasons, r => Assert.True(r.Weight > 1));
        await engine.SaveAsync("outcomes", new { id = "f-again", recommendation = shown.Id, candidate = "b", reward = 1.0 });
        var duplicate = await engine.RecommendAsync("s", "home");
        Assert.Equal(updated.Items.Single(i => i.Id == "b").Reasons, duplicate.Items.Single(i => i.Id == "b").Reasons);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("outcomes", new { id = "bad", recommendation = shown.Id, candidate = "absent", reward = 1.0 }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredEvidenceFallsBackInsteadOfKeepingItsNormalisedDirection(bool sqlite)
    {
        var clock = new TestClock();
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite), clock: clock);
        await Products(engine);
        await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "c", type = "view" });
        Assert.Equal("c", (await engine.RecommendAsync("s", "home")).Items[0].Id);
        clock.Advance(TimeSpan.FromHours(6));
        var result = await engine.RecommendAsync("s", "home");
        Assert.All(result.Items, i => Assert.Empty(i.Reasons));
        Assert.Equal("a", result.Items[0].Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedAndLateUpdatesRollbackTheirDeduplicationEntries(bool sqlite)
    {
        var clock = new TestClock();
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite), clock: clock);
        await Products(engine);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "missing", type = "view" }));
        Assert.False((await engine.SaveAsync("events", new { id = "v", subject = "s", candidate = "a", type = "view" })).Duplicate);
        clock.Advance(TimeSpan.FromMinutes(1));
        await engine.SaveAsync("events", new { id = "next", subject = "s", candidate = "a", type = "view" });
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("events", new { id = "late", subject = "s", candidate = "a", type = "view", at = clock.GetUtcNow().AddSeconds(-1) }));
        Assert.False((await engine.SaveAsync("events", new { id = "late", subject = "s", candidate = "a", type = "view" })).Duplicate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopeIsolationAndConfigurationChangesAreExplicit(bool sqlite)
    {
        var store = Store(sqlite);
        await using var engine = new RelevanceEngine(Definition(), store);
        await Products(engine);
        var other = Definition();
        other.Id = "other-shop";
        await using var otherEngine = new RelevanceEngine(other, new BorrowedStore(store));
        Assert.Empty((await otherEngine.RecommendAsync("s", "home")).Items);
        var changed = Definition();
        changed.Points["price"].Scale = 500;
        await using var incompatible = new RelevanceEngine(changed, new BorrowedStore(store));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await incompatible.RecommendAsync("s", "home"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StateCapacityIsBoundedAndFailedInsertionIsAtomic(bool sqlite)
    {
        var definition = Definition();
        definition.MaxStateRecords = 1;
        await using var engine = new RelevanceEngine(definition, Store(sqlite));
        await Products(engine);
        await engine.SaveAsync("events", new { id = "v1", subject = "s1", candidate = "a", type = "view" });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await engine.SaveAsync("events", new { id = "v2", subject = "s2", candidate = "a", type = "view" }));
        Assert.Empty((await engine.RecommendAsync("s2", "home")).Items.SelectMany(i => i.Reasons));
    }

    [Fact]
    public async Task SqlitePersistsStateFeedbackAndDeduplicationAcrossReopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"relevance-reopen-{Guid.NewGuid():N}.db");
        var observation = new { id = "v", subject = "s", candidate = "c", type = "cart" };
        await using (var engine = new RelevanceEngine(Definition(), new SqliteRelevanceStore(path)))
        {
            await Products(engine);
            await engine.SaveAsync("events", observation);
            var shown = await engine.RecommendAsync("s", "home");
            await engine.SaveAsync("outcomes", new { id = "f", recommendation = shown.Id, candidate = "c", reward = 1.0 });
        }
        await using var reopened = new RelevanceEngine(Definition(), new SqliteRelevanceStore(path));
        Assert.True((await reopened.SaveAsync("events", observation)).Duplicate);
        var result = await reopened.RecommendAsync("s", "home");
        Assert.Equal("c", result.Items[0].Id);
        Assert.All(result.Items[0].Reasons, r => Assert.True(r.Weight > 1));
    }

    [Fact]
    public async Task ConcurrentEventsAreNotLost()
    {
        var store = new SqliteRelevanceStore(Path.Combine(Path.GetTempPath(), $"relevance-concurrent-{Guid.NewGuid():N}.db"));
        await using var engine = new RelevanceEngine(Definition(), store);
        await Products(engine);
        await Task.WhenAll(Enumerable.Range(0, 32).Select(i => engine.SaveAsync("events", new { id = i.ToString(), subject = "s", candidate = "a", type = "view" }).AsTask()));
        var state = await store.TransactAsync(tx => tx.Get<SubjectState>("shop-demo-v1", "state", "s"));
        Assert.InRange(state!.Points["price"].Mass, 31.99, 32);
    }

    [Fact]
    public async Task FilterAndInputValidationFailClosed()
    {
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore());
        await Products(engine);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RecommendAsync("s", "home", Filter.Eq("undeclared", "x")));
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.SaveAsync("products", new { id = "bad", category = "unknown", price = 1, available = true }));
        Assert.Empty((await engine.RecommendAsync("s", "home", Filter.Eq("category", "outdoors' OR 1=1 --"))).Items);
        var compiled = SqliteFilterCompiler.Compile(Filter.Eq("category", "x' OR 1=1 --"));
        Assert.DoesNotContain("OR 1=1", compiled.Sql);
        Assert.Contains("x' OR 1=1 --", compiled.Parameters.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NumericBoundsDoNotCoerceStringMetadataOrFailTheProvider(bool sqlite)
    {
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite));
        await Products(engine);
        // A declared field can contain strings. A numeric bound makes those candidates ineligible.
        Assert.Empty((await engine.RecommendAsync("s", "home", Filter.Lte("category", 10))).Items);
        Assert.Empty((await engine.RecommendAsync("s", "home", Filter.Eq("price", "100"))).Items);
        Assert.NotEmpty((await engine.RecommendAsync("s", "home", Filter.Lte("price", 120))).Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EventIdentityIsAStructuredPairRatherThanAnAmbiguousConcatenation(bool sqlite)
    {
        var definition = Definition();
        definition.Streams["events:part"] = new StreamDefinition
        {
            Kind = "observation",
            Weights = new Dictionary<string, double> { ["view"] = 1 }
        };
        var store = Store(sqlite);
        await using var engine = new RelevanceEngine(definition, store);
        await Products(engine);
        var first = new { id = "part:x", subject = "s", candidate = "a", type = "view" };
        var second = new { id = "x", subject = "s", candidate = "a", type = "view" };
        await engine.SaveAsync("events", first);
        await engine.SaveAsync("events:part", second);
        Assert.True((await engine.SaveAsync("events", first)).Duplicate);
        Assert.True((await engine.SaveAsync("events:part", second)).Duplicate);
        var state = await store.TransactAsync(tx => tx.Get<SubjectState>(definition.Id, "state", "s"));
        Assert.InRange(state!.Points["price"].Mass, 1.99, 2);
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed;
    }

    private sealed class BorrowedStore(IRelevanceStore store) : IRelevanceStore
    {
        public ValueTask<T> TransactAsync<T>(Func<IRelevanceTransaction, T> action, CancellationToken cancellationToken = default) => store.TransactAsync(action, cancellationToken);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
