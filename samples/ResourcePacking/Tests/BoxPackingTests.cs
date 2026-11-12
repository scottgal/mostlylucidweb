using PackingCalendars;
using ResourcePacking.Examples;

namespace ResourcePacking.Tests;

public sealed class BoxPackingTests
{
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

    [Fact]
    public void TwoCubesCanTouchButCannotOccupyTheSamePosition()
    {
        var container = new Box3D(4, 2, 2);
        var choices = BoxPacking.Generate(container,
            [new("a", new Box3D(2, 2, 2)), new("b", new Box3D(2, 2, 2))], [new(0, 0, 0), new(2, 0, 0)]);
        Assert.Equal(2, choices.Problem.Conflicts.Count);
        var result = new CpSatPackingSolver().Solve(choices.Problem, Budget);
        Assert.Equal(SolveStatus.Optimal, result.Status);
        Assert.Equal(2, result.Selected.Count);
        Assert.False(result.CandidatesComplete);
        BoxPacking.Verify(container, choices, result);
    }

    [Fact]
    public void EnoughTotalVolumeDoesNotGuaranteeThatBoxesFit()
    {
        var container = new Box3D(3, 3, 1);
        var choices = BoxPacking.Generate(container,
            [new("a", new Box3D(2, 2, 1), true), new("b", new Box3D(2, 2, 1), true)],
            [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1, 0)]);
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        Assert.Equal(SolveStatus.Infeasible, solver.Solve(choices.Problem, Budget).Status);
    }

    [Fact]
    public void RotationsAreGeneratedAndCandidateLimitsAreExplicit()
    {
        var container = new Box3D(2, 3, 1);
        var choices = BoxPacking.Generate(container, [new("a", new Box3D(3, 1, 2))], [new(0, 0, 0)]);
        Assert.Equal(new Box3D(2, 3, 1), Assert.Single(choices.Placements.Values).Size);
        Assert.Throws<ArgumentException>(() => BoxPacking.Generate(new Box3D(4, 2, 2),
            [new("a", new Box3D(2, 2, 2))], [new(0, 0, 0), new(2, 0, 0)], candidateLimit: 1));
    }

    [Fact]
    public void ExplicitConflictsConnectComponentsAndAreCheckedByEverySolver()
    {
        var choices = new Candidate[] { new("a", "a", [], []), new("b", "b", [], []), new("c", "c", [], []) };
        var requests = new Request[] { new("a"), new("b"), new("c") };
        var initial = new PackingProblem([], requests, choices);
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        var first = solver.SolveWithStatistics(initial, Budget);
        Assert.Equal(SolveStatus.Optimal, first.Selection.Status);
        Assert.Equal(3, first.Work.Components);
        var joined = new PackingProblem([], requests, choices, conflicts: [new("a", "b"), new("b", "a")]);
        Assert.Single(joined.Conflicts);
        var next = solver.SolveWithStatistics(joined, Budget);
        Assert.Equal(new ComponentWork(2, 1, 1, 2, 2), next.Work);
        Assert.Equal(2, next.Selection.Selected.Count);
        Assert.Equal(2, new GreedyPackingSolver().Solve(joined, Budget).Selected.Count);
        Assert.Throws<InvalidOperationException>(() =>
            PackingVerifier.Verify(joined, ["a", "b"], SolveStatus.Feasible, false, TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => new PackingProblem([], requests, choices, conflicts: [new("a", "missing")]));
        Assert.Throws<ArgumentException>(() => new PackingProblem([], requests, choices, conflicts: [new("a", "a")]));
        Assert.Equal(3, solver.SolveWithStatistics(initial, Budget).Selection.Selected.Count);
    }

    [Fact]
    public void AddingAConflictInsideAnExistingComponentInvalidatesTheCachedSolution()
    {
        var requests = new Request[] { new("a"), new("b"), new("c") };
        var choices = new Candidate[] { new("a", "a", [], []), new("b", "b", [], []), new("c", "c", [], []) };
        var initial = new PackingProblem([], requests, choices, conflicts: [new("a", "b"), new("b", "c")]);
        var solver = new IncrementalPackingSolver(new CpSatPackingSolver());
        Assert.Equal(2, solver.Solve(initial, Budget).Selected.Count);
        var triangle = new PackingProblem([], requests, choices,
            conflicts: [.. initial.Conflicts, new("a", "c")]);
        var changed = solver.SolveWithStatistics(triangle, Budget);
        Assert.Single(changed.Selection.Selected);
        Assert.Equal(1, changed.Work.SolvedComponents);
    }
}
