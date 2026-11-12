using Mostlylucid.Packing.OrTools;
using Mostlylucid.Packing.Planning;
using Mostlylucid.Relevance;

namespace AdaptiveAllocation.Tests;

public sealed class WorkRoutingTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task CheapOnlineWorkCanBecomeMoreExpensiveWhenContextMustBeReplayed()
    {
        var fresh = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0));
        Assert.Equal("online", Choose(fresh).Supplier);
        var warm = await BuildAsync(DemoFixture.Suppliers(), DemoFixture.Quotes());
        var local = Choose(warm);
        Assert.Equal("local", local.Supplier);
        Assert.Equal(0, local.ReplayTokens);
        var online = warm.Choices[WorkRouting.Id("conversation", "online")];
        Assert.Equal(8_000, online.ReplayTokens);
        Assert.Equal(9_000, online.PrefillTokens);
        Assert.Equal(11, online.Allocation.Cost);
        Assert.Equal(6, local.Allocation.Cost);
    }

    [Theory]
    [InlineData("evicted")]
    [InlineData("expired")]
    [InlineData("context-changed")]
    [InlineData("tokenizer-changed")]
    public async Task CurrentSupplierNameDoesNotGrantFreeContextReuse(string reason)
    {
        var snapshots = DemoFixture.Suppliers();
        var context = snapshots[0].Contexts.Single();
        snapshots[0] = snapshots[0] with
        {
            Contexts = reason switch
            {
                "evicted" => [],
                "expired" => [context with { ExpiresAt = DemoFixture.Now }],
                "context-changed" => [context with { ContextVersion = "old-context" }],
                _ => [context with { TokenizerVersion = "old-tokenizer" }]
            }
        };
        var input = await BuildAsync(snapshots, DemoFixture.Quotes());
        Assert.Equal(8_000, input.Choices[WorkRouting.Id("conversation", "local")].ReplayTokens);
        Assert.Equal("online", Choose(input).Supplier);
    }

    [Fact]
    public async Task CachedHistoryStillOccupiesTheContextWindow()
    {
        var snapshots = DemoFixture.Suppliers();
        snapshots[0] = snapshots[0] with { Supplier = snapshots[0].Supplier with { MaxContextTokens = 2_000 } };
        var input = await BuildAsync(snapshots, DemoFixture.Quotes());
        Assert.Contains(input.Rejected, reason => reason.Supplier == "local" && reason.Reason == "ContextWindow");
        Assert.Equal("online", Choose(input).Supplier);
    }

    [Fact]
    public async Task RetainedContextCanHaveACacheReadCharge()
    {
        var snapshots = DemoFixture.Suppliers();
        snapshots[0] = snapshots[0] with
        {
            Supplier = snapshots[0].Supplier with { CachedInputPointsPerThousand = 2 }
        };
        var input = await BuildAsync(snapshots, DemoFixture.Quotes());
        var local = input.Choices[WorkRouting.Id("conversation", "local")];
        Assert.Equal(0, local.ReplayTokens);
        Assert.Equal(8_000, local.ReusedContextTokens);
        Assert.Equal(22, local.Allocation.Cost);
        Assert.Equal("online", Choose(input).Supplier);
    }

    [Fact]
    public async Task DifferentTargetTokenizersHaveTheirOwnQuotes()
    {
        var quotes = DemoFixture.Quotes();
        quotes[1] = quotes[1] with { HistoryTokens = 10_000 };
        var input = await BuildAsync(DemoFixture.Suppliers(), quotes);
        Assert.Equal(11_000, input.Choices[WorkRouting.Id("conversation", "online")].PrefillTokens);
        quotes[1] = quotes[1] with { TokenizerVersion = "stale-estimate" };
        var stale = await BuildAsync(DemoFixture.Suppliers(), quotes);
        Assert.Contains(stale.Rejected, reason => reason.Supplier == "online" && reason.Reason == "TokenizerChanged");
    }

    [Fact]
    public async Task ChangedCapacityForcesAFeasibleSwitchDespiteReplayCost()
    {
        var snapshots = DemoFixture.Suppliers();
        snapshots[0] = snapshots[0] with { Supplier = snapshots[0].Supplier with { Slots = 0 } };
        var input = await BuildAsync(snapshots, DemoFixture.Quotes());
        Assert.Equal("online", Choose(input).Supplier);
        Assert.Contains(input.Rejected, reason => reason.Supplier == "local" && reason.Reason == "Capacity");
    }

    [Fact]
    public async Task ASeparateDisruptionPenaltyDoesNotChangeTheTokenQuote()
    {
        var input = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0),
            DemoFixture.Work() with { ChangePenalty = 10 });
        Assert.Equal("local", Choose(input).Supplier);
        var online = input.Choices[WorkRouting.Id("conversation", "online")];
        Assert.Equal(3, online.Allocation.Cost);
        Assert.Equal(10, online.Allocation.ChangeCost);
        Assert.Equal(0, online.ReplayTokens);
    }

    [Fact]
    public async Task MoreCompletedOutcomesCanChangeTheChoiceWithoutChangingCapacity()
    {
        var memory = DemoFixture.Outcomes();
        var before = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0), outcomes: memory);
        Assert.Equal("online", Choose(before).Supplier);
        for (var index = 0; index < 5; index++) memory.Observe(new($"failure-{index}", "extract-json", "online", false));
        var after = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0), outcomes: memory);
        Assert.Equal("local", Choose(after).Supplier);
        Assert.Equal(10, after.Choices[WorkRouting.Id("conversation", "online")].Evidence.Completed);
    }

    [Fact]
    public async Task RelevancePreferencesRemainSeparateFromOutcomeEstimatesAndCapacity()
    {
        await using var relevance = new RelevanceEngine(RelevanceDefinition.ForVectors(2), new MemoryRelevanceStore());
        var cold = await WorkRouting.BuildAsync([DemoFixture.Work()], DemoFixture.Suppliers(), DemoFixture.Quotes(),
            DemoFixture.Outcomes(), DemoFixture.Now, preferences: relevance);
        Assert.All(cold.Choices.Values, choice => Assert.Equal(0, choice.Allocation.Preference));
        await relevance.SaveAsync("items", new RelevanceItem("liked-online", [0, 1]));
        await relevance.SaveAsync("events", new RelevanceObservation("observed-choice", "operator", "liked-online"));
        var learned = await WorkRouting.BuildAsync([DemoFixture.Work()], DemoFixture.Suppliers(), DemoFixture.Quotes(),
            DemoFixture.Outcomes(), DemoFixture.Now, preferences: relevance);
        Assert.True(learned.Choices[WorkRouting.Id("conversation", "online")].Allocation.Preference >
                    learned.Choices[WorkRouting.Id("conversation", "local")].Allocation.Preference);
        Assert.Equal("local", Choose(learned).Supplier);
        Assert.Equal(cold.Choices[WorkRouting.Id("conversation", "online")].Evidence,
            learned.Choices[WorkRouting.Id("conversation", "online")].Evidence);
    }

    [Fact]
    public async Task RemoteRestrictionsAndInteractiveRequirementsExcludeOtherwiseAttractiveOptions()
    {
        var noRemote = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0),
            DemoFixture.Work() with { AllowRemote = false });
        Assert.Equal("local", Choose(noRemote).Supplier);
        Assert.Contains(noRemote.Rejected, reason => reason.Reason == "RemoteNotAllowed");

        var suppliers = DemoFixture.Suppliers(false);
        suppliers[1] = suppliers[1] with { Supplier = suppliers[1].Supplier with { Mode = ExecutionMode.Deferred } };
        var interactive = await BuildAsync(suppliers, DemoFixture.Quotes(0));
        Assert.Equal("local", Choose(interactive).Supplier);
        var deferred = await BuildAsync(suppliers, DemoFixture.Quotes(0), DemoFixture.Work() with { Interactive = false });
        Assert.Equal("online", Choose(deferred).Supplier);
    }

    [Fact]
    public async Task ReplayLatencyAndOutputCapacityAreCheckedIndependently()
    {
        var input = await BuildAsync(DemoFixture.Suppliers(), DemoFixture.Quotes(),
            DemoFixture.Work() with { MaxLatencyMs = 100 });
        Assert.Equal("local", Choose(input).Supplier);
        Assert.Contains(input.Rejected, reason => reason.Supplier == "online" && reason.Reason == "EstimatedLatency");
        var suppliers = DemoFixture.Suppliers();
        suppliers[1] = suppliers[1] with { Supplier = suppliers[1].Supplier with { OutputTokens = 999 } };
        var limited = await BuildAsync(suppliers, DemoFixture.Quotes());
        Assert.Contains(limited.Rejected, reason => reason.Supplier == "online" && reason.Reason == "Capacity");
    }

    [Fact]
    public async Task IndividuallyFeasibleWorkStillCompetesForSharedPrefillBudget()
    {
        var supplier = DemoFixture.Suppliers(false)[1];
        supplier = supplier with { Supplier = supplier.Supplier with { PrefillTokens = 1_600 } };
        WorkRequest[] work = [DemoFixture.Work() with { Id = "a" }, DemoFixture.Work() with { Id = "b" }];
        var quotes = work.Select(request => new TokenQuote(request.Id, "online", "online-tokenizer-v1", 1_000, 0, 1_000)).ToArray();
        var input = await WorkRouting.BuildAsync(work, [supplier], quotes, DemoFixture.Outcomes(), DemoFixture.Now);
        Assert.Equal(2, input.Choices.Count);
        var selected = new CpSatPackingSolver().Solve(input.Problem, Budget);
        Assert.Single(selected.Selected);
        Assert.Single(selected.Unfulfilled);
    }

    [Fact]
    public async Task UnknownTaskPerformanceAbstainsRatherThanBecomingAnInventedZeroOrOne()
    {
        var memory = new OutcomeMemory();
        memory.Observe(new("one", "extract-json", "online", true));
        var input = await BuildAsync(DemoFixture.Suppliers(false), DemoFixture.Quotes(0), outcomes: memory);
        Assert.Empty(input.Choices);
        Assert.All(input.Rejected, reason => Assert.Equal("InsufficientOutcomeEvidence", reason.Reason));
        Assert.Null(memory.Estimate("extract-json", "online").SmoothedSuccess);
    }

    [Fact]
    public async Task InvalidAndUnquotedContextFailsExplicitly()
    {
        var suppliers = DemoFixture.Suppliers();
        suppliers[0] = suppliers[0] with
        {
            Contexts = [suppliers[0].Contexts.Single() with { RetainedTokens = 9_000 }]
        };
        await Assert.ThrowsAsync<ArgumentException>(() => BuildAsync(suppliers, DemoFixture.Quotes()));
        await Assert.ThrowsAsync<ArgumentException>(() => BuildAsync(DemoFixture.Suppliers(),
            [new TokenQuote("unknown-work", "local", "v1", 1, 0, 0)]));
        var missing = await BuildAsync(DemoFixture.Suppliers(), []);
        Assert.All(missing.Rejected, reason => Assert.Equal("MissingTokenQuote", reason.Reason));
    }

    [Fact]
    public void OutcomeRetriesAreIdempotentAndOnlyCompletedObservationsCount()
    {
        var memory = new OutcomeMemory(capacity: 1);
        var outcome = new ObservedOutcome("result", "extract", "local", true);
        memory.Observe(outcome);
        memory.Observe(outcome);
        Assert.Equal(1, memory.Estimate("extract", "local").Completed);
        Assert.Throws<InvalidOperationException>(() => memory.Observe(outcome with { Succeeded = false }));
        Assert.Throws<InvalidOperationException>(() => memory.Observe(outcome with { Id = "another" }));
        Assert.Equal(0, memory.Estimate("unobserved", "local").Completed);
    }

    private static Task<RoutingInput> BuildAsync(SupplierSnapshot[] suppliers, TokenQuote[] quotes,
        WorkRequest? work = null, OutcomeMemory? outcomes = null) =>
        WorkRouting.BuildAsync([work ?? DemoFixture.Work()], suppliers, quotes, outcomes ?? DemoFixture.Outcomes(), DemoFixture.Now);

    private static RoutingChoice Choose(RoutingInput input)
    {
        var selected = new CpSatPackingSolver().Solve(input.Problem, Budget);
        Assert.Contains(selected.Status, new[] { SolveStatus.Optimal, SolveStatus.Feasible });
        return input.Choices[Assert.Single(selected.Selected)];
    }
}
