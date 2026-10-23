using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class PredictionTests
{
    private static RelevanceDefinition Definition()
    {
        var d = RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml")));
        d.Destinations!.MinTransitions = 1;
        d.Destinations.Smoothing = 0;
        return d;
    }
    private static IRelevanceStore Store(bool sqlite) => sqlite ? new SqliteRelevanceStore(Path.Combine(Path.GetTempPath(), $"relevance-predict-{Guid.NewGuid():N}.db")) : new MemoryRelevanceStore();
    private static object Product => new { id = "p", category = "outdoors", price = 100, available = true };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DestinationLikelihoodComesFromObservedOutcomesNotGeometricSimilarity(bool sqlite)
    {
        await using var engine = new RelevanceEngine(Definition(), Store(sqlite));
        await engine.SaveAsync("products", Product);
        for (var i = 0; i < 4; i++)
        {
            await engine.SaveAsync("events", new { id = $"v{i}", subject = $"history{i}", candidate = "p", type = "view" });
            await engine.SaveAsync("events", new { id = $"o{i}", subject = $"history{i}", candidate = "p", type = i < 3 ? "purchase" : "cancel" });
        }
        await engine.SaveAsync("events", new { id = "current", subject = "now", candidate = "p", type = "view" });
        var prediction = await engine.PredictAsync("now");
        Assert.Equal("estimated", prediction.Status);
        Assert.Equal(4, prediction.SupportingTransitions);
        Assert.Equal(0.75, prediction.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
        Assert.Equal(0.25, prediction.Outcomes.Single(o => o.Destination == "cancel").Probability, 10);
        Assert.Equal(0, prediction.RemainingProbability);
        var observed = await engine.PredictAsync("history0");
        Assert.Equal("observed_destination", observed.Status);
        Assert.Equal(1, observed.Outcomes.Single(o => o.Destination == "purchase").Probability);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SparseOrUnseenStatesAbstainAndRepeatedEventsDoNotTrainTwice(bool sqlite)
    {
        var definition = Definition();
        definition.Destinations!.MinTransitions = 3;
        await using var engine = new RelevanceEngine(definition, Store(sqlite));
        await engine.SaveAsync("products", Product);
        var first = new { id = "v", subject = "s", candidate = "p", type = "view" };
        var second = new { id = "p", subject = "s", candidate = "p", type = "purchase" };
        await engine.SaveAsync("events", first);
        await engine.SaveAsync("events", second);
        await engine.SaveAsync("events", second);
        await engine.SaveAsync("events", new { id = "new", subject = "now", candidate = "p", type = "view" });
        var prediction = await engine.PredictAsync("now");
        Assert.Equal("insufficient_evidence", prediction.Status);
        Assert.Equal(1, prediction.SupportingTransitions);
        Assert.Equal(1, prediction.RemainingProbability);
        Assert.All(prediction.Outcomes, o => Assert.Equal(0, o.Probability));
        Assert.Equal("no_state", (await engine.PredictAsync("unknown")).Status);
    }

    [Fact]
    public async Task FiniteHorizonRetainsUnresolvedMassAndSmoothingKeepsTotalProbabilityOne()
    {
        var definition = Definition();
        definition.Destinations!.HorizonEvents = 1;
        definition.Destinations.Smoothing = 0.5;
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore());
        await engine.SaveAsync("products", Product);
        await engine.SaveAsync("events", new { id = "v", subject = "history", candidate = "p", type = "view" });
        await engine.SaveAsync("events", new { id = "c", subject = "history", candidate = "p", type = "cart" });
        await engine.SaveAsync("events", new { id = "new", subject = "now", candidate = "p", type = "view" });
        var prediction = await engine.PredictAsync("now");
        Assert.True(prediction.RemainingProbability > 0);
        Assert.Equal(1, prediction.Outcomes.Sum(o => o.Probability) + prediction.RemainingProbability, 10);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawSystemStateUsesTheSameIngestionAndPredictionApi(bool sqlite)
    {
        var d = Definition();
        d.Id = "system-example";
        d.Streams["events"].PointSource = "input";
        d.Streams["events"].Weights = new() { ["sample"] = 1 };
        d.Points = new() { ["latency"] = new() { Path = "latency", Converter = "formula", Expression = "x / 1000", Metric = "l2" } };
        d.Placements["home"].Heads = new() { ["latency"] = 1 };
        d.Placements.Remove("outdoors");
        d.Filters.Clear();
        d.Placements["home"].Where.Clear();
        d.Destinations!.States = ["healthy", "degraded", "outage", "recovered"];
        d.Destinations.Outcomes = ["outage", "recovered"];
        await using var engine = new RelevanceEngine(d, Store(sqlite));
        await engine.SaveAsync("events", new { id = "d1", subject = "history", type = "sample", state = "degraded", latency = 500 });
        await engine.SaveAsync("events", new { id = "d2", subject = "history", type = "sample", state = "outage", latency = 2000 });
        await engine.SaveAsync("events", new { id = "now", subject = "current", type = "sample", state = "degraded", latency = 450 });
        var prediction = await engine.PredictAsync("current");
        Assert.Equal(1, prediction.Outcomes.Single(o => o.Destination == "outage").Probability);
    }
}
