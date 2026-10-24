using System.Text.Json;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class TrajectoryPredictionTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = Epoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static RelevanceDefinition Definition() => RelevanceDefinition.FromYaml("""
        id: trajectory-tests-v1
        points:
          x: {path: x, converter: numeric}
        streams:
          events: {kind: observation, point_source: input, weights: {sample: 1}}
        placements:
          default: {heads: {x: 1}}
        destinations:
          states: [shopping, healthy, degraded, purchase, cancel, leave]
          outcomes: [purchase, cancel, leave]
          trajectory:
            points: [x]
            horizon_seconds: 60
            half_life_seconds: 0.000001
            max_gap_seconds: 600
            max_distance: 0.4
            min_examples: 6
        """);
    private static async Task Save(RelevanceEngine engine, Clock clock, string subject, string id, double x,
        DateTimeOffset at, string state = "shopping", string group = "shop")
    {
        clock.Now = at;
        await engine.SaveAsync("events", new {id, subject, type = "sample", state, x, at, baseline_group = group});
    }
    private static async Task Prefix(RelevanceEngine engine, Clock clock, string subject, double[] values, double interval, DateTimeOffset at)
    {
        for (var i = 0; i < values.Length; i++) await Save(engine, clock, subject, $"{subject}-{i}", values[i], at.AddSeconds(i * interval));
    }
    private static async Task Train(RelevanceEngine engine, Clock clock)
    {
        for (var i = 0; i < 20; i++)
        {
            var start = Epoch.AddSeconds(i * 1000);
            var up = i % 2 == 0;
            await Prefix(engine, clock, $"train-{i}", up ? [.2, .35, .5] : [.8, .65, .5], 10, start);
            // Each direction has a distinct empirical population: 80% versus 20% purchase.
            var purchase = up ? i % 10 != 0 : i % 10 == 1;
            await Save(engine, clock, $"train-{i}", $"end-{i}", .5, start.AddSeconds(30), purchase ? "purchase" : "cancel");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SamePositionAndStatePredictDifferentDestinationsFromMeasuredDirection(bool sqlite)
    {
        var clock = new Clock();
        var definition = Definition();
        IRelevanceStore store = sqlite ? new SqliteRelevanceStore(Path.Combine(Path.GetTempPath(), $"motion-{Guid.NewGuid():N}.db")) : new MemoryRelevanceStore();
        await using var engine = new RelevanceEngine(definition, store, clock: clock);
        await Train(engine, clock);
        await Prefix(engine, clock, "up", [.2, .35, .5], 10, Epoch.AddHours(8));
        var up = await engine.PredictAsync("up");
        await Prefix(engine, clock, "down", [.8, .65, .5], 10, Epoch.AddHours(8).AddMinutes(1));
        var down = await engine.PredictAsync("down");
        Assert.Equal(up.CurrentState, down.CurrentState);
        Assert.Equal(up.Trajectory!.Centroid, down.Trajectory!.Centroid);
        Assert.True(up.Trajectory.Velocity![0] > 0);
        Assert.True(down.Trajectory.Velocity![0] < 0);
        Assert.Equal(.8, up.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
        Assert.Equal(.2, down.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
        Assert.Equal(60, up.HorizonSeconds);
        Assert.Equal(0, up.HorizonEvents);
        Assert.Equal("estimated_trajectory", up.Status);
        Assert.All(new[] {up, down}, p => Assert.Equal(1, p.Outcomes.Sum(o => o.Probability) + p.RemainingProbability, 10));
        Assert.False(up.IsCalibrated);
    }

    [Fact]
    public async Task SamePathAtDifferentPacesUsesARealTimeWindow()
    {
        var definition = Definition();
        var clock = new Clock();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: clock);
        for (var i = 0; i < 20; i++)
        {
            var start = Epoch.AddSeconds(i * 1000);
            var interval = i % 2 == 0 ? 10 : 60;
            await Prefix(engine, clock, $"train-{i}", [.2, .35, .5], interval, start);
            await Save(engine, clock, $"train-{i}", $"end-{i}", 1, start.AddSeconds(interval * 2 + (interval == 10 ? 20 : 300)), "purchase");
        }
        await Prefix(engine, clock, "fast", [.2, .35, .5], 10, Epoch.AddHours(8));
        var fast = await engine.PredictAsync("fast");
        await Prefix(engine, clock, "slow", [.2, .35, .5], 60, Epoch.AddHours(8).AddMinutes(1));
        var slow = await engine.PredictAsync("slow");
        Assert.Equal(1, fast.Outcomes.Single(o => o.Destination == "purchase").Probability);
        Assert.Equal(0, slow.Outcomes.Single(o => o.Destination == "purchase").Probability);
        Assert.Equal(1, slow.RemainingProbability); // Observed later purchase, outside the 60-second target.
        Assert.Equal("estimated_trajectory", slow.Status);
        Assert.True(fast.Trajectory!.Velocity![0] > slow.Trajectory!.Velocity![0]);
        Assert.Equal(120, slow.Trajectory.ElapsedSeconds);
    }

    [Fact]
    public async Task IrregularSamplingUsesVelocityMidpointsForAcceleration()
    {
        var clock = new Clock();
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore(), clock: clock);
        await Save(engine, clock, "curve", "a", 0, Epoch);
        Assert.Equal("insufficient_motion", (await engine.PredictAsync("curve")).Status);
        await Save(engine, clock, "curve", "b", 4, Epoch.AddSeconds(2));
        Assert.Null((await engine.PredictAsync("curve")).Trajectory!.Acceleration);
        await Save(engine, clock, "curve", "c", 25, Epoch.AddSeconds(5));
        var frame = (await engine.PredictAsync("curve")).Trajectory!;
        Assert.Equal(7, frame.Velocity![0]);
        Assert.Equal(2, frame.Acceleration![0]);
    }

    [Fact]
    public async Task AccelerationSeparatesPopulationsWithTheSameCurrentPositionAndVelocity()
    {
        var definition = Definition();
        definition.Destinations!.Trajectory!.AccelerationScale = .0001;
        var clock = new Clock();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: clock);
        for (var i = 0; i < 20; i++)
        {
            var start = Epoch.AddSeconds(i * 1000);
            var accelerating = i % 2 == 0;
            await Prefix(engine, clock, $"train-{i}", accelerating ? [.35, .4, .5] : [.2, .4, .5], 10, start);
            var purchase = accelerating ? i % 10 != 0 : i % 10 == 1;
            await Save(engine, clock, $"train-{i}", $"end-{i}", .5, start.AddSeconds(30), purchase ? "purchase" : "cancel");
        }
        await Prefix(engine, clock, "accelerating", [.35, .4, .5], 10, Epoch.AddHours(8));
        var a = await engine.PredictAsync("accelerating");
        await Prefix(engine, clock, "decelerating", [.2, .4, .5], 10, Epoch.AddHours(8).AddMinutes(1));
        var b = await engine.PredictAsync("decelerating");
        Assert.Equal(a.Trajectory!.Centroid, b.Trajectory!.Centroid);
        Assert.Equal(a.Trajectory.Velocity, b.Trajectory.Velocity);
        Assert.True(a.Trajectory.Acceleration![0] > 0);
        Assert.True(b.Trajectory.Acceleration![0] < 0);
        Assert.Equal(.8, a.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
        Assert.Equal(.2, b.Outcomes.Single(o => o.Destination == "purchase").Probability, 10);
    }

    [Fact]
    public async Task BaselineLearnsOnlyQualifiedNormalDataAndKeepsGroupsSeparate()
    {
        var definition = Definition();
        definition.Destinations!.Trajectory!.BaselineStates = ["healthy"];
        var clock = new Clock();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: clock);
        for (var i = 0; i < 20; i++)
        {
            await Save(engine, clock, "normal-a", $"a-{i}", 1, Epoch.AddSeconds(i), "healthy", "api-a");
            await Save(engine, clock, "normal-b", $"b-{i}", 10, Epoch.AddSeconds(i), "healthy", "api-b");
        }
        var original = (await engine.PredictAsync("normal-a")).Trajectory!.Normal!;
        for (var i = 0; i < 3; i++) await Save(engine, clock, "normal-a", $"bad-{i}", 4 + i, Epoch.AddSeconds(20 + i), "degraded", "api-a");
        var incident = (await engine.PredictAsync("normal-a")).Trajectory!.Normal!;
        Assert.Equal(original.Centroid, incident.Centroid);
        Assert.Equal(1, incident.Centroid[0]);
        Assert.True(incident.Deviation > 100);
        Assert.Equal(original.At, incident.At);
        Assert.Equal(10, (await engine.PredictAsync("normal-b")).Trajectory!.Normal!.Centroid[0]);
    }

    [Fact]
    public async Task DuplicateRetriesDoNotCreateMotionOrExtraTrainingAndEqualTimeRollsBack()
    {
        var clock = new Clock();
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore(), clock: clock);
        await Prefix(engine, clock, "one", [.2, .35, .5], 10, Epoch);
        var before = JsonSerializer.Serialize(await engine.PredictAsync("one"));
        var duplicate = await engine.SaveAsync("events", new {id = "one-2", subject = "one", type = "sample", state = "shopping", x = .5, at = clock.Now, baseline_group = "shop"});
        Assert.True(duplicate.Duplicate);
        await Assert.ThrowsAsync<ArgumentException>(async () => await Save(engine, clock, "one", "same-time", .6, clock.Now));
        Assert.Equal(before, JsonSerializer.Serialize(await engine.PredictAsync("one")));
    }

    [Fact]
    public async Task LongGapsAndExpiredForecastWindowsAbstain()
    {
        var clock = new Clock();
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore(), clock: clock);
        await Prefix(engine, clock, "one", [.2, .35, .5], 10, Epoch);
        clock.Now = clock.Now.AddSeconds(61);
        Assert.Equal("stale_trajectory", (await engine.PredictAsync("one")).Status);
        await Save(engine, clock, "one", "gap", .6, Epoch.AddSeconds(621));
        var reset = await engine.PredictAsync("one");
        Assert.Equal("insufficient_motion", reset.Status);
        Assert.Null(reset.Trajectory!.Velocity);
        Assert.Equal(0, reset.Trajectory.ElapsedSeconds);
    }

    [Fact]
    public async Task UnfinishedEpisodesNeverBecomeFailedOutcomesAndOneStateContributesOneVote()
    {
        var definition = Definition();
        definition.Destinations!.Trajectory!.MinExamples = 1;
        var clock = new Clock();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: clock);
        await Prefix(engine, clock, "history", [.2, .35, .5, .65, .8], 10, Epoch);
        await Prefix(engine, clock, "query", [.2, .35, .5], 10, Epoch.AddSeconds(41));
        Assert.Equal(0, (await engine.PredictAsync("query")).SupportingTransitions);
        await Save(engine, clock, "history", "outcome", .9, Epoch.AddSeconds(62), "purchase");
        var prediction = await engine.PredictAsync("query");
        Assert.Equal(1, prediction.SupportingTransitions);
        Assert.Equal(1, prediction.Outcomes.Single(o => o.Destination == "purchase").Probability);
        await Assert.ThrowsAsync<ArgumentException>(async () => await Save(engine, clock, "history", "second-outcome", .9, Epoch.AddSeconds(63), "purchase"));
        Assert.Equal(1, (await engine.PredictAsync("query")).SupportingTransitions);
    }

    [Fact]
    public async Task OldNormalEvidenceLosesEffectiveSupportWithoutBeingRelabelledHealthy()
    {
        var definition = Definition();
        definition.Destinations!.Trajectory!.BaselineStates = ["healthy"];
        definition.Destinations.Trajectory.BaselineHalfLifeSeconds = 100;
        var clock = new Clock();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: clock);
        for (var i = 0; i < 40; i++) await Save(engine, clock, "healthy", $"h-{i}", 1, Epoch.AddSeconds(i), "healthy");
        var normal = (await engine.PredictAsync("healthy")).Trajectory!.Normal!;
        Assert.NotNull(normal.Deviation);
        await Save(engine, clock, "incident", "incident", 5, Epoch.AddSeconds(340), "degraded");
        var aged = (await engine.PredictAsync("incident")).Trajectory!.Normal!;
        Assert.Equal(normal.Centroid, aged.Centroid);
        Assert.Null(aged.Deviation);
        Assert.True(aged.EffectiveMass < definition.Destinations.Trajectory.MinBaselineMass);
    }

    [Fact]
    public async Task ContextCapacityEvictsOldCellsAndKeepsTheNewestContextUsable()
    {
        var definition = Definition();
        definition.Destinations!.Trajectory!.MaxCohorts = 2;
        definition.Destinations.Trajectory.NearestCohorts = 2;
        definition.Destinations.Trajectory.MinExamples = 1;
        var clock = new Clock();
        var store = new MemoryRelevanceStore();
        await using var engine = new RelevanceEngine(definition, store, clock: clock);
        for (var i = 0; i < 8; i++)
        {
            await Prefix(engine, clock, $"history-{i}", [i + .2, i + .35, i + .5], 10, Epoch.AddSeconds(i * 100));
            await Save(engine, clock, $"history-{i}", $"end-{i}", i + .6, Epoch.AddSeconds(i * 100 + 30), "purchase");
        }
        var cells = await store.TransactAsync(tx => tx.Get<TransitionModel>(definition.Id, "transitions", "model")!.Trajectories!.Cohorts.Count);
        Assert.Equal(2, cells);
        await Prefix(engine, clock, "recent", [7.2, 7.35, 7.5], 10, Epoch.AddSeconds(800));
        Assert.Equal("estimated_trajectory", (await engine.PredictAsync("recent")).Status);
    }

    [Fact]
    public async Task SqliteReopenPreservesMotionAndLearnedProbabilities()
    {
        var path = Path.Combine(Path.GetTempPath(), $"motion-reopen-{Guid.NewGuid():N}.db");
        var clock = new Clock();
        string before;
        await using (var engine = new RelevanceEngine(Definition(), new SqliteRelevanceStore(path), clock: clock))
        {
            await Train(engine, clock);
            await Prefix(engine, clock, "fresh", [.2, .35, .5], 10, Epoch.AddHours(8));
            before = JsonSerializer.Serialize(await engine.PredictAsync("fresh"));
        }
        await using var reopened = new RelevanceEngine(Definition(), new SqliteRelevanceStore(path), clock: clock);
        Assert.Equal(before, JsonSerializer.Serialize(await reopened.PredictAsync("fresh")));
    }

    [Fact]
    public async Task AnEndpointCanCloseTheSavedTrajectoryWithoutInventingANewMeasurement()
    {
        var clock = new Clock();
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore(), clock: clock);
        await Prefix(engine, clock, "one", [.2, .35, .5], 10, Epoch);
        var before = (await engine.PredictAsync("one")).Trajectory!;
        clock.Now = Epoch.AddSeconds(30);
        await engine.SaveAsync("events", new {id = "end", subject = "one", type = "sample", state = "purchase", at = clock.Now});
        var after = await engine.PredictAsync("one");
        Assert.Equal("observed_destination", after.Status);
        Assert.Equal(clock.Now, after.ObservedAt);
        Assert.Equal(before.At, after.Trajectory!.At);
        Assert.Equal(before.Velocity, after.Trajectory.Velocity);
        Assert.Equal(1, after.Outcomes.Single(o => o.Destination == "purchase").Probability);
    }

    [Fact]
    public async Task AnObservedEndpointIsKnownEvenWithoutPriorMotionEvidence()
    {
        var clock = new Clock();
        await using var engine = new RelevanceEngine(Definition(), new MemoryRelevanceStore(), clock: clock);
        await engine.SaveAsync("events", new {id = "end", subject = "one", type = "sample", state = "cancel", at = clock.Now});
        var prediction = await engine.PredictAsync("one");
        Assert.Equal("observed_destination", prediction.Status);
        Assert.Null(prediction.Trajectory);
        Assert.Equal(1, prediction.Outcomes.Single(o => o.Destination == "cancel").Probability);
    }
}
