using System.Text.Json;
using Mostlylucid.Relevance.Learning;
using Mostlylucid.Relevance.ExperimentDemo;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class InterventionLearningTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-10);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static RelevanceDefinition RankingDefinition() => RelevanceDefinition.FromYaml("""
        id: intervention-test
        candidate_limit: 8
        points:
          content: {path: vector, converter: vector, dimensions: 1, metric: l2}
        streams:
          emails: {kind: candidate}
          responses: {kind: observation, weights: {helpful: 1}}
        placements:
          campaign: {heads: {content: 1}}
        """);
    private static LearningDefinition Definition() => new()
    {
        Id = "test-effects", ContextAxes = ["comparison", "narrowing"], InterventionAxes = ["copy_feature"],
        MaxVariants = 8, MinOutcomes = 10, DescriptorRadius = .6, Exploration = .2
    };
    private static InterventionVariant Variant(string id, double value, bool control = false) =>
        new(id, [value], JsonSerializer.SerializeToElement(new { id, vector = new[] { value }, html = "Exact source " + id }), control);
    private static async Task Seed(InterventionLearning learning)
    {
        await learning.RegisterAsync(Variant("control", 0, true));
        await learning.RegisterAsync(Variant("changed", 1));
    }

    [Fact]
    public async Task DetectsGroupsWithoutAudienceLabelsAndKeepsAssignmentsStable()
    {
        var clock = new Clock(); var rankStore = new MemoryRelevanceStore();
        await using var relevance = new RelevanceEngine(RankingDefinition(), rankStore, clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock, new Random(814));
        await Seed(learning);
        var request = new InterventionRequest("one", "visit-1", [.8, .7], clock.Now);
        var first = await learning.ChooseAsync(request);
        var retry = await learning.ChooseAsync(request);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(retry));
        await learning.ChooseAsync(new("two", "visit-2", [.75, .65], clock.Now));
        await learning.ChooseAsync(new("three", "visit-3", [.1, .1], clock.Now));
        var snapshot = await learning.SnapshotAsync();
        Assert.Equal(2, snapshot.Groups.Count);
        Assert.Equal(3, snapshot.Evidence.Sum(e => e.Assigned));
        Assert.All(snapshot.Groups, g => Assert.StartsWith("detected-", g.Id));
        Assert.Equal(0, await rankStore.TransactAsync(tx => tx.Count("intervention-test", "decision")));
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.ChooseAsync(request with { Coordinates = [.1, .1] }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.ChooseAsync(request with { Id = "new-id" }));
    }

    [Fact]
    public async Task ObservedOutcomeWithoutViewStillLearnsAndRetriesDoNotMultiplyIt()
    {
        var clock = new Clock();
        await using var relevance = new RelevanceEngine(RankingDefinition(), new MemoryRelevanceStore(), clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock);
        await Seed(learning);
        var d = await learning.ChooseAsync(new("decision", "unit", [.7, .8], clock.Now));
        var outcome = new InterventionEvent("endpoint", d.Id, "outcome", clock.Now, "observed-window", 0, "closed-without-purchase");
        await learning.SaveAsync(outcome);
        Assert.True((await learning.SaveAsync(outcome)).Duplicate);
        var evidence = (await learning.SnapshotAsync()).Evidence.Single(e => e.Assigned == 1);
        Assert.Equal(1, evidence.Completed); Assert.Equal(0, evidence.Viewed); Assert.Equal(0, evidence.MeanReward);
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.SaveAsync(outcome with { Reward = 1 }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.SaveAsync(outcome with { Id = "another-final" }));
        await learning.SaveAsync(new("late-view-report", d.Id, "viewed", clock.Now, "first-party-view"));
        Assert.Equal(1, (await learning.SnapshotAsync()).Evidence.Single(e => e.Assigned == 1).Viewed);
    }

    [Fact]
    public async Task CensoredAndExpiredPendingFeedbackNeverBecomeFailedRewards()
    {
        var clock = new Clock();
        await using var relevance = new RelevanceEngine(RankingDefinition(), new MemoryRelevanceStore(), clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock);
        await Seed(learning);
        var first = await learning.ChooseAsync(new("one", "one", [.2, .2], clock.Now));
        await learning.SaveAsync(new("lost", first.Id, "censored", clock.Now, "telemetry", Outcome: "unavailable"));
        var second = await learning.ChooseAsync(new("two", "two", [.2, .2], clock.Now));
        clock.Now = clock.Now.AddHours(2);
        var snapshot = await learning.SnapshotAsync();
        Assert.Equal(0, snapshot.Evidence.Sum(e => e.Completed));
        Assert.Equal(1, snapshot.Evidence.Sum(e => e.Censored));
        Assert.False((await learning.GetAsync(second.Id))!.Closed);
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.SaveAsync(new("late", second.Id, "outcome", clock.Now, "telemetry", 1, "purchase")));
    }

    [Fact]
    public async Task ControlCannotBeRemovedAndFailedAssignmentDoesNotCreateAGroup()
    {
        var clock = new Clock();
        await using var relevance = new RelevanceEngine(RankingDefinition(), new MemoryRelevanceStore(), clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock);
        await Seed(learning);
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.ChooseAsync(new("bad", "bad", [.2, .2], clock.Now, ["changed"])));
        Assert.Empty((await learning.SnapshotAsync()).Groups);
        await Assert.ThrowsAsync<ArgumentException>(async () => await learning.RegisterAsync(Variant("changed", .5)));
        Assert.True((await learning.RegisterAsync(Variant("changed", 1))).Duplicate);
    }

    [Fact]
    public async Task DescriptorModelLearnsOppositeContextPreferencesAndScoresANewVariant()
    {
        var clock = new Clock();
        await using var relevance = new RelevanceEngine(RankingDefinition(), new MemoryRelevanceStore(), clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock, new Random(42));
        await Seed(learning);
        for (var i = 0; i < 240; i++)
        {
            var coordinates = i % 2 == 0 ? new[] { .8, .8 } : [.1, .1];
            var d = await learning.ChooseAsync(new("d" + i, "u" + i, coordinates, clock.Now, Randomised: true));
            var x = d.Variant.Features[0]; var reward = i % 2 == 0 ? .1 + .8 * x : .9 - .8 * x;
            await learning.SaveAsync(new("e" + i, d.Id, "outcome", clock.Now, "randomised-environment", reward, "known-result"));
        }
        await learning.RegisterAsync(Variant("new-midpoint", .5));
        var a = await learning.ChooseAsync(new("future-a", "future-a", [.8, .8], clock.Now));
        var b = await learning.ChooseAsync(new("future-b", "future-b", [.1, .1], clock.Now));
        Assert.True(a.Alternatives.Single(s => s.Id == "changed").Score > a.Alternatives.Single(s => s.Id == "control").Score);
        Assert.True(b.Alternatives.Single(s => s.Id == "changed").Score < b.Alternatives.Single(s => s.Id == "control").Score);
        var unseen = a.Alternatives.Single(s => s.Id == "new-midpoint");
        Assert.Equal(0, unseen.Outcomes); Assert.Equal("descriptor-regression", unseen.Source);
        Assert.InRange(unseen.Score, .48, .52);
        Assert.InRange(a.Probability, .2 / 3, 1); // Exact realised propensity, not the score.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestartPreservesExactContentAttributionAndDelayedDecisions(bool sqlite)
    {
        var directory = Directory.CreateTempSubdirectory("learning-tests-");
        var clock = new Clock();
        IRelevanceStore Open(string name) => sqlite ? new SqliteRelevanceStore(Path.Combine(directory.FullName, name + ".db")) : new MemoryRelevanceStore();
        try
        {
            var rankStore = Open("rank"); var learnStore = Open("learn");
            await using var relevance = new RelevanceEngine(RankingDefinition(), rankStore, clock: clock);
            await using var learning = new InterventionLearning(Definition(), learnStore, relevance, clock);
            await Seed(learning);
            var first = await learning.ChooseAsync(new("first", "first", [.2, .2], clock.Now));
            for (var i = 0; i < 30; i++) await learning.ChooseAsync(new("later" + i, "later" + i, [.2, .2], clock.Now));
            Assert.DoesNotContain((await learning.SnapshotAsync()).Recent, r => r.Decision.Id == first.Id);
            Assert.Equal(first.Variant.Candidate.GetRawText(), (await learning.GetAsync(first.Id))!.Decision.Variant.Candidate.GetRawText());
            await learning.SaveAsync(new("final", first.Id, "outcome", clock.Now, "late-observation", 1, "purchase"));
            if (sqlite)
            {
                await learning.DisposeAsync(); await relevance.DisposeAsync();
                await using var nextRelevance = new RelevanceEngine(RankingDefinition(), Open("rank"), clock: clock);
                await using var next = new InterventionLearning(Definition(), Open("learn"), nextRelevance, clock);
                Assert.True((await next.GetAsync(first.Id))!.Closed);
                Assert.Equal(1, (await next.SnapshotAsync()).Evidence.Sum(e => e.Completed));
            }
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task CommittedOutcomeSurvivesAffinityFailureAndPersistentOutboxRetriesOnce()
    {
        var clock = new Clock(); var underlying = new MemoryRelevanceStore(); var failing = new FailOnceStore(underlying);
        await using var relevance = new RelevanceEngine(RankingDefinition(), failing, clock: clock);
        await using var learning = new InterventionLearning(Definition(), new MemoryRelevanceStore(), relevance, clock);
        await Seed(learning);
        var d = await learning.ChooseAsync(new("one", "unit", [.2, .2], clock.Now));
        var outcome = new InterventionEvent("completed", d.Id, "outcome", clock.Now, "observed", 1, "purchase");
        failing.FailNext = true;
        await Assert.ThrowsAsync<IOException>(async () => await learning.SaveAsync(outcome));
        // The authoritative outcome committed before the separate affinity delivery failed.
        Assert.True((await learning.GetAsync(d.Id))!.Closed);
        Assert.True((await learning.SaveAsync(outcome)).Duplicate);
        Assert.Equal(1, (await learning.SnapshotAsync()).Evidence.Sum(e => e.Completed));
        var state = await underlying.TransactAsync(tx => tx.Get<SubjectState>("intervention-test", "state", d.Group));
        Assert.Equal(1, state!.Points["content"].Mass);
    }

    [Fact]
    public async Task GroupCapacityAbstainsAndExplicitAudiencesRemainOptional()
    {
        var clock = new Clock(); var definition = Definition(); definition.MaxGroups = 1;
        await using var relevance = new RelevanceEngine(RankingDefinition(), new MemoryRelevanceStore(), clock: clock);
        await using var learning = new InterventionLearning(definition, new MemoryRelevanceStore(), relevance, clock);
        await Seed(learning);
        var first = await learning.ChooseAsync(new("a", "a", [.1, .1], clock.Now));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await learning.ChooseAsync(new("b", "b", [.9, .9], clock.Now)));
        Assert.Single((await learning.SnapshotAsync()).Groups);
        Assert.Equal("detected-001", first.Group);
    }

    private sealed class FailOnceStore(IRelevanceStore inner) : IRelevanceStore
    {
        public bool FailNext { get; set; }
        public ValueTask<T> TransactAsync<T>(Func<IRelevanceTransaction,T> operation, CancellationToken cancellationToken=default)
        {
            if (FailNext) { FailNext=false; throw new IOException("Simulated failure before the affinity transaction."); }
            return inner.TransactAsync(operation,cancellationToken);
        }
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
