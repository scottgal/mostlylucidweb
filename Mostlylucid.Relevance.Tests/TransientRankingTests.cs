using System.Text.Json;

namespace Mostlylucid.Relevance.Tests;

public sealed class TransientRankingTests
{
    [Fact]
    public async Task TransientCandidatesUseLearnedEvidenceWithoutBeingSaved()
    {
        var definition = RelevanceDefinition.ForVectors(2);
        var store = new MemoryRelevanceStore();
        await using var engine = new RelevanceEngine(definition, store);
        await engine.SaveAsync("items", new RelevanceItem("past", [1, 0]));
        await engine.SaveAsync("events", new RelevanceObservation("visit", "alice", "past"));

        var result = await engine.RankCandidatesAsync("alice", "next",
            new RelevanceItem[] { new("morning", [0, 1]), new("afternoon", [1, 0]) }, count: 2);
        Assert.Equal("afternoon", result[0].Id);
        Assert.Single(result[0].Reasons);
        Assert.Equal(1, await store.TransactAsync(tx => tx.Count(definition.Id, "candidate")));
        Assert.Equal(0, await store.TransactAsync(tx => tx.Count(definition.Id, "decision")));
        Assert.Equal("past", Assert.Single(await engine.RankAsync("alice", "next")).Id);
    }

    [Fact]
    public async Task StoredAndTransientPoolsHaveTheSameExactRankingAndHeadContributions()
    {
        var definition = RelevanceDefinition.ForVectors(2);
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore());
        RelevanceItem[] candidates = [new("a", [0, 0]), new("b", [1, 0]), new("c", [0, 1])];
        foreach (var candidate in candidates) await engine.SaveAsync("items", candidate);
        await engine.SaveAsync("events", new RelevanceObservation("visit", "alice", "b"));
        var stored = await engine.RankAsync("alice", "next", count: 3);
        var transient = await engine.RankCandidatesAsync("alice", "next", candidates, count: 3);
        Assert.Equal(stored.Select(item => item.Id), transient.Select(item => item.Id));
        for (var index = 0; index < stored.Count; index++)
        {
            Assert.Equal(stored[index].Score, transient[index].Score);
            Assert.Equal(stored[index].Reasons, transient[index].Reasons);
        }
    }

    [Fact]
    public async Task EligibilityAppliesBeforeTransientTopKAndColdStartIsDeterministic()
    {
        var definition = RelevanceDefinition.ForVectors(1, filters: ["metadata.allowed", "metadata.cost"]);
        definition.Placements["next"].Where["metadata.allowed"] = JsonSerializer.SerializeToElement(true);
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore());
        RelevanceItem Item(string id, float coordinate, bool allowed, int cost) =>
            new(id, [coordinate], JsonSerializer.SerializeToElement(new { allowed, cost }));
        await engine.SaveAsync("items", Item("past", 1, true, 0));
        await engine.SaveAsync("events", new RelevanceObservation("visit", "alice", "past"));
        RelevanceItem[] pool = [Item("bad", 1, false, 20), Item("cheap", 1, true, 5), Item("valid", 2, true, 20)];
        var result = await engine.RankCandidatesAsync("alice", "next", pool, Filter.Gte("metadata.cost", 10), 1);
        Assert.Equal("valid", Assert.Single(result).Id);
        var cold = await engine.RankCandidatesAsync("unknown", "next", pool, count: 3);
        Assert.Equal(new[] { "cheap", "valid" }, cold.Select(item => item.Id));
        Assert.All(cold, item => Assert.Equal(0, item.Score));
    }

    [Fact]
    public async Task DuplicateIdsInvalidVectorsAndQueueInputMutationsAreHandled()
    {
        await using var engine = new RelevanceEngine(RelevanceDefinition.ForVectors(2), new MemoryRelevanceStore());
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RankCandidatesAsync("alice", "next",
            new RelevanceItem[] { new("a", [1, 0]), new("a", [0, 1]) }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RankCandidatesAsync("alice", "next",
            new RelevanceItem[] { new("a", [float.NaN, 0]) }));
        float[] vector = [1, 0];
        var pending = engine.RankCandidatesAsync("unknown", "next", new RelevanceItem[] { new("a", vector) });
        vector[0] = float.NaN;
        Assert.Single(await pending);
    }

    [Fact]
    public async Task TransientRankingCoversTheFullPoolIndependentlyOfTheStoredRetrievalWindow()
    {
        var definition = RelevanceDefinition.ForVectors(1);
        definition.CandidateLimit = 1;
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore());
        await engine.SaveAsync("items", new RelevanceItem("past", [1]));
        await engine.SaveAsync("events", new RelevanceObservation("visit", "alice", "past"));
        var result = await engine.RankCandidatesAsync("alice", "next",
            new RelevanceItem[] { new("nearest", [1]), new("other", [2]) }, count: 2);
        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Select(item => item.Reasons[0].Rank));
    }

    [Fact]
    public async Task PoolLimitAndCancellationAreExplicit()
    {
        var definition = RelevanceDefinition.ForVectors(1);
        definition.CandidateLimit = 1;
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await engine.RankCandidatesAsync("alice", "next",
            Enumerable.Range(0, 10_001).Select(index => new RelevanceItem(index.ToString(), [1])).ToArray(), count: 1));
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await engine.RankCandidatesAsync("alice", "next",
            new RelevanceItem[] { new("a", [1]) }, count: 1, cancellationToken: source.Token));
    }
}
