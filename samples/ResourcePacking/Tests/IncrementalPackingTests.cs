namespace ResourcePacking.Tests;

public sealed class IncrementalPackingTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

    [Fact]
    public void CapacityChangeResolvesOneComponentAndRefreshesSnapshotMetadata()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var first = solver.SolveWithStatistics(Independent(1), Budget);
        Assert.Equal(new ComponentWork(3, 3, 0, 3, 3), first.Work);
        var nextProblem = Independent(0, new PlanningSnapshot("demo", 2, 5, 10, 20));
        var next = solver.SolveWithStatistics(nextProblem, Budget);
        Assert.Equal(new ComponentWork(3, 1, 2, 1, 1), next.Work);
        Assert.Equal(["b", "c"], next.Selection.Selected);
        Assert.Equal(nextProblem.Snapshot, next.Selection.Snapshot);
        Assert.Equal(SolveStatus.Optimal, next.Selection.Status);
        Assert.Equal(new ComponentWork(3, 0, 3, 0, 0), solver.SolveWithStatistics(nextProblem, Budget).Work);
    }

    [Fact]
    public void AlternativePlacementsConnectRequestsTransitively()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var problem = new PackingProblem([Capacity("a", 1), Capacity("b", 1), Capacity("c", 1)],
            [new("first"), new("middle"), new("last")],
            [Choice("first-a", "first", "a"), Choice("first-b", "first", "b"),
                Choice("middle-b", "middle", "b"), Choice("middle-c", "middle", "c"),
                Choice("last-c", "last", "c")]);
        Assert.Equal(1, solver.SolveWithStatistics(problem, Budget).Work.Components);
        var changed = new PackingProblem([Capacity("a", 0), Capacity("b", 1), Capacity("c", 1)],
            problem.Requests.Values, problem.Candidates.Values);
        var next = solver.SolveWithStatistics(changed, Budget);
        Assert.Equal(new ComponentWork(1, 1, 0, 3, 5), next.Work);
        Assert.Equal(2, next.Selection.Selected.Count);
    }

    [Fact]
    public void ANewSharedResourceMergesComponentsAndRemovingItSplitsThem()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var problem = Independent(1);
        Assert.Equal(SolveStatus.Optimal, solver.Solve(problem, Budget).Status);
        var choices = problem.Candidates.Values.Select(candidate => candidate.Id == "b"
            ? candidate with { Demands = [.. candidate.Demands, new(new DimensionId("a"), 1)] }
            : candidate);
        var joined = new PackingProblem(problem.Capacities.Select(pair => new Capacity(pair.Key, pair.Value)),
            problem.Requests.Values, choices);
        var next = solver.SolveWithStatistics(joined, Budget);
        Assert.Equal(new ComponentWork(2, 1, 1, 2, 2), next.Work);
        Assert.Equal(2, next.Selection.Selected.Count);
        Assert.Equal(new ComponentWork(3, 2, 1, 2, 2), solver.SolveWithStatistics(problem, Budget).Work);
    }

    [Fact]
    public void ChangedUtilityInvalidatesItsComponentAndRequiredRequestsCanInvalidateTheWholePlan()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var problem = Independent(1);
        Assert.Equal(SolveStatus.Optimal, solver.Solve(problem, Budget).Status);
        var choices = problem.Candidates.Values.Select(candidate => candidate with
        {
            PreferenceFeatures = new long[] { candidate.Id == "a" ? 50 : 0 }
        });
        var scored = new PackingProblem(problem.Capacities.Select(pair => new Capacity(pair.Key, pair.Value)),
            problem.Requests.Values, choices, [1]);
        var preferred = solver.SolveWithStatistics(scored, Budget);
        Assert.Equal(1, preferred.Work.SolvedComponents);
        Assert.Equal(50, preferred.Selection.Utility);

        var required = new PackingProblem([Capacity("a", 0), Capacity("b", 1), Capacity("c", 1)],
            [new("a", true), new("b"), new("c")], problem.Candidates.Values);
        var impossible = solver.SolveWithStatistics(required, Budget);
        Assert.Equal(SolveStatus.Infeasible, impossible.Selection.Status);
        Assert.Empty(impossible.Selection.Selected);
        Assert.Equal(3, solver.SolveWithStatistics(required, Budget).Work.ReusedComponents);
    }

    [Fact]
    public void FeasibleAndUnknownResultsAreSearchedAgain()
    {
        var solver = new IncrementalPackingSolver(new GreedyPackingSolver());
        var problem = Independent(1);
        Assert.Equal(SolveStatus.Feasible, solver.Solve(problem, Budget).Status);
        Assert.Equal(3, solver.SolveWithStatistics(problem, Budget).Work.SolvedComponents);
        var expired = new IncrementalPackingSolver(new CpSatPackingSolver())
            .SolveWithStatistics(problem, TimeSpan.FromTicks(1));
        Assert.Equal(SolveStatus.Unknown, expired.Selection.Status);
        Assert.Empty(expired.Selection.Selected);
    }

    [Fact]
    public void CandidatePoolCompletenessAndInputOrderingDoNotReuseStaleMetadata()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var problem = Independent(1);
        solver.Solve(problem, Budget);
        var reordered = new PackingProblem(problem.Capacities.Reverse().Select(pair => new Capacity(pair.Key, pair.Value)),
            problem.Requests.Values.Reverse(), problem.Candidates.Values.Reverse(), candidatesComplete: false);
        var next = solver.SolveWithStatistics(reordered, Budget);
        Assert.Equal(3, next.Work.ReusedComponents);
        Assert.False(next.Selection.CandidatesComplete);
    }

    [Fact]
    public void EmptyAndZeroDemandComponentsAreValidAndRemovedRequestsAreEvicted()
    {
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var noResource = new PackingProblem([], [new("first")], [new("first", "first", [], [])]);
        Assert.Single(solver.Solve(noResource, Budget).Selected);
        var empty = solver.SolveWithStatistics(new([], [], []), Budget);
        Assert.Equal(SolveStatus.Optimal, empty.Selection.Status);
        Assert.Equal(0, empty.Work.Components);
        Assert.Equal(1, solver.SolveWithStatistics(noResource, Budget).Work.SolvedComponents);
    }

    [Fact]
    public void ChangesIncludingNewEdgesAndDeletedChoicesMatchFullSolves()
    {
        var random = new Random(451);
        var full = new CpSatPackingSolver();
        var incremental = new IncrementalPackingSolver(full);
        for (var step = 0; step < 100; step++)
        {
            var requests = Enumerable.Range(0, 8).Select(index => new Request($"r{index}", index == 0 && step % 7 == 0))
                .ToArray();
            var capacities = Enumerable.Range(0, 5).Select(index => Capacity($"d{index}", random.Next(0, 5))).ToArray();
            var choices = requests.SelectMany((request, index) => Enumerable.Range(0, random.Next(0, 3))
                .Select(choice => new Candidate($"{request.Id}:{choice}", request.Id,
                    [new Consumption(new DimensionId($"d{(index + choice + step % 3) % 5}"), random.Next(1, 3))],
                    [random.Next(-10, 11)]))).ToArray();
            var problem = new PackingProblem(capacities, requests, choices, [1]);
            var expected = full.Solve(problem, Budget);
            var actual = incremental.Solve(problem, Budget);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.Selected.Count, actual.Selected.Count);
            Assert.Equal(expected.Utility, actual.Utility);
        }
    }

    private static PackingProblem Independent(int firstCapacity, PlanningSnapshot? snapshot = null) =>
        new([Capacity("a", firstCapacity), Capacity("b", 1), Capacity("c", 1)],
            [new("a"), new("b"), new("c")], [Choice("a", "a", "a"), Choice("b", "b", "b"), Choice("c", "c", "c")],
            snapshot: snapshot);

    private static Capacity Capacity(string name, long available) => new(new DimensionId(name), available);

    private static Candidate Choice(string id, string request, string dimension) =>
        new(id, request, [new Consumption(new DimensionId(dimension), 1)], []);
}
