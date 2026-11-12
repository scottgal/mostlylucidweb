using ResourcePacking.Examples;

namespace ResourcePacking.Tests;

public sealed class PackingSolverTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(2);
    private static readonly DimensionId Cpu = new("cpu");
    private static readonly DimensionId Ram = new("ram");

    [Fact]
    public void EveryDimensionMustFitAndPreferencesOnlyBreakCountTies()
    {
        var problem = new PackingProblem([new(Cpu, 4), new(Ram, 16)], [new("first"), new("second")],
            [new("too-much-cpu", "first", [new(Cpu, 5)], [999]),
                new("expensive", "first", [new(Cpu, 4)], [200]),
                new("low", "first", [new(Cpu, 2)], [-20]),
                new("better", "first", [new(Cpu, 2), new(Ram, 1)], [-10]),
                new("second", "second", [new(Cpu, 2), new(Ram, 15)], [0])], [1]);

        var result = new CpSatPackingSolver().Solve(problem, Budget);
        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(["better", "second"], result.Selected);
        Assert.Equal(-10, result.Utility);
        Assert.True(result.CountProvenOptimal);
    }

    [Fact]
    public void MissingRequiredRequestIsNotAFeasiblePartialPlan()
    {
        var problem = new PackingProblem([], [new("must", Required: true)], []);
        Assert.Equal(SolveStatus.Infeasible, new CpSatPackingSolver().Solve(problem, Budget).Status);
        Assert.Equal(SolveStatus.Unknown, new GreedyPackingSolver().Solve(problem, Budget).Status);
        Assert.Throws<InvalidOperationException>(() =>
            PackingVerifier.Verify(problem, [], SolveStatus.Feasible, false, TimeSpan.Zero));
    }

    [Fact]
    public void CapacityConflictCanMakeTwoRequiredRequestsInfeasible()
    {
        var problem = new PackingProblem([new(Cpu, 1)], [new("a", true), new("b", true)],
            [new("a1", "a", [new(Cpu, 1)], []), new("b1", "b", [new(Cpu, 1)], [])]);
        Assert.Equal(SolveStatus.Infeasible, new CpSatPackingSolver().Solve(problem, Budget).Status);
    }

    [Fact]
    public void EmptyProblemIsOptimalAndDeadlineWithoutSolutionIsUnknown()
    {
        Assert.Equal(SolveStatus.Optimal, new CpSatPackingSolver().Solve(new([], [], []), Budget).Status);
        Assert.Equal(SolveStatus.Unknown, new CpSatPackingSolver().Solve(Scenarios.Servers(), TimeSpan.FromTicks(1)).Status);
    }

    [Fact]
    public void SnapshotAndCandidateLimitArePreservedInTheResult()
    {
        var version = new PlanningSnapshot("demo", 1, 9, 10, 20);
        var problem = new PackingProblem([], [], [], snapshot: version, candidatesComplete: false);
        var result = new CpSatPackingSolver().Solve(problem, Budget);
        Assert.Equal(version, result.Snapshot);
        Assert.False(result.CandidatesComplete);
    }

    [Fact]
    public void ProblemCopiesAndCombinesDemands()
    {
        Consumption[] demands = [new(Cpu, 1), new(Cpu, 2)];
        long[] features = [2];
        var problem = new PackingProblem([new(Cpu, 3)], [new("a")], [new("a1", "a", demands, features)], [4]);
        demands[0] = new(Cpu, 100);
        features[0] = 100;
        Assert.Equal(3, Assert.Single(problem.Candidates["a1"].Demands).Units);
        Assert.Equal(8, problem.Utilities["a1"]);
    }

    [Fact]
    public void InvalidInputsAndInvalidSolutionsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new PackingProblem([], [new("a")], [new("a1", "a", [new(Cpu, 1)], [])]));
        Assert.Throws<ArgumentException>(() => new PackingProblem([], [new("a")], [new("a1", "a", [], [1])]));
        Assert.Throws<OverflowException>(() => new PackingProblem([], [new("a")], [new("a1", "a", [], [long.MaxValue])], [2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarPacking.Scale(double.NaN));
        var problem = Scenarios.Servers();
        Assert.Throws<InvalidOperationException>(() => Verify("api@small", "api@large"));
        Assert.Throws<InvalidOperationException>(() => Verify("jobs@small"));
        Assert.Throws<InvalidOperationException>(() => Verify("missing"));

        void Verify(params string[] ids) => PackingVerifier.Verify(problem, ids, SolveStatus.Feasible, false, TimeSpan.Zero);
    }

    [Fact]
    public void CancellationBeforeModelConstructionIsObserved()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => new CpSatPackingSolver().Solve(Scenarios.Servers(), Budget, source.Token));
    }

    [Fact]
    public void SolverMatchesExhaustiveEnumeration()
    {
        var random = new Random(2049);
        var solver = new CpSatPackingSolver();
        for (var run = 0; run < 100; run++)
        {
            var capacities = new[] { new Capacity(Cpu, random.Next(1, 9)), new Capacity(Ram, random.Next(1, 9)) };
            var requests = Enumerable.Range(0, 4).Select(id => new Request($"r{id}", random.Next(4) == 0)).ToArray();
            var candidates = requests.SelectMany(request => Enumerable.Range(0, random.Next(1, 3)).Select(index =>
                new Candidate($"{request.Id}:{index}", request.Id, [new(Cpu, random.Next(1, 4)), new(Ram, random.Next(1, 4))],
                    [random.Next(-10, 11)]))).ToArray();
            var conflicts = candidates.Length > 1 && run % 2 == 0
                ? new CandidateConflict[] { new(candidates[0].Id, candidates[^1].Id) }
                : [];
            var problem = new PackingProblem(capacities, requests, candidates, [1], conflicts: conflicts);
            var result = solver.Solve(problem, Budget);

            (int Count, long Utility)? best = null;
            for (var mask = 0; mask < (1 << candidates.Length); mask++)
            {
                var picks = candidates.Where((_, index) => (mask & (1 << index)) != 0).ToArray();
                var fulfilled = picks.Select(pick => pick.RequestId).ToHashSet();
                if (fulfilled.Count != picks.Length || requests.Any(request => request.Required && !fulfilled.Contains(request.Id)))
                    continue;
                var chosen = picks.Select(pick => pick.Id).ToHashSet(StringComparer.Ordinal);
                if (conflicts.Any(conflict => chosen.Contains(conflict.First) && chosen.Contains(conflict.Second)))
                    continue;
                if (capacities.Any(capacity => picks.SelectMany(pick => pick.Demands)
                        .Where(demand => demand.Dimension == capacity.Dimension).Sum(demand => demand.Units) > capacity.Available))
                    continue;

                var score = (picks.Length, picks.Sum(pick => pick.PreferenceFeatures[0]));
                if (best is null || score.CompareTo(best.Value) > 0) best = score;
            }

            if (best is null)
                Assert.Equal(SolveStatus.Infeasible, result.Status);
            else
            {
                Assert.Equal(SolveStatus.Optimal, result.Status);
                Assert.Equal(best.Value, (result.Selected.Count, result.Utility));
            }
        }
    }
}
