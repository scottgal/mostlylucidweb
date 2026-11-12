using System.Diagnostics;
using System.Globalization;
using Google.OrTools.Sat;

namespace ResourcePacking;

public sealed class CpSatPackingSolver : IPackingSolver
{
    public PackingResult Solve(PackingProblem problem, TimeSpan budget, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(budget, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();
        var clock = Stopwatch.StartNew();
        var model = new CpModel();
        var variables = problem.Candidates.Keys.ToDictionary(id => id, id => model.NewBoolVar(id), StringComparer.Ordinal);
        var byRequest = problem.Candidates.Values.ToLookup(candidate => candidate.RequestId);
        foreach (var request in problem.Requests.Values)
        {
            var choices = byRequest[request.Id].Select(candidate => variables[candidate.Id]).ToArray();
            if (request.Required && choices.Length == 0)
                return Result(SolveStatus.Infeasible);
            if (request.Required)
                model.AddExactlyOne(choices);
            else if (choices.Length > 1)
                model.AddAtMostOne(choices);
        }

        var rows = problem.Candidates.Values.SelectMany(candidate => candidate.Demands.Select(demand =>
            (demand.Dimension, demand.Units, Variable: variables[candidate.Id]))).GroupBy(entry => entry.Dimension);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expression = LinearExpr.WeightedSum(row.Select(entry => entry.Variable), row.Select(entry => entry.Units));
            model.Add(expression <= problem.Capacities[row.Key]);
        }

        foreach (var conflict in problem.Conflicts)
            model.AddAtMostOne(new[] { variables[conflict.First], variables[conflict.Second] });

        var count = LinearExpr.Sum(variables.Values);
        model.Maximize(count);
        using var primary = new CpSolver();
        var first = Run(primary);
        if (first is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            return Result(Map(first));

        var incumbent = Read(primary);
        if (first == CpSolverStatus.Feasible)
            return Result(SolveStatus.Feasible, incumbent);
        if (incumbent.Length == 0 || problem.Utilities.Values.All(utility => utility == 0))
            return Result(SolveStatus.Optimal, incumbent, true);

        var firstPlan = Result(SolveStatus.Feasible, incumbent, true);
        model.Add(count == incumbent.Length);
        var utility = LinearExpr.WeightedSum(variables.Values, variables.Keys.Select(id => problem.Utilities[id]));
        model.Maximize(utility);
        var chosen = incumbent.ToHashSet(StringComparer.Ordinal);
        foreach (var (id, variable) in variables) model.AddHint(variable, chosen.Contains(id));

        using var secondary = new CpSolver();
        var second = Run(secondary);
        if (second is CpSolverStatus.ModelInvalid or CpSolverStatus.Infeasible)
            throw new InvalidOperationException($"Preference pass rejected the verified count: {second}.");
        if (second == CpSolverStatus.Optimal)
            return Result(SolveStatus.Optimal, Read(secondary), true);
        if (second == CpSolverStatus.Feasible)
        {
            var alternative = Read(secondary);
            var alternativePlan = Result(SolveStatus.Feasible, alternative, true);
            if (alternativePlan.Utility > firstPlan.Utility) return alternativePlan;
        }
        return firstPlan with { Elapsed = clock.Elapsed };

        CpSolverStatus Run(CpSolver solver)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = budget - clock.Elapsed;
            if (remaining <= TimeSpan.Zero) return CpSolverStatus.Unknown;
            solver.StringParameters = string.Create(CultureInfo.InvariantCulture,
                $"max_time_in_seconds:{remaining.TotalSeconds:R} num_search_workers:1");
            // Cancellation is observed around each bounded native solve.
            var status = solver.Solve(model);
            cancellationToken.ThrowIfCancellationRequested();
            return status;
        }

        string[] Read(CpSolver solver) => variables.Where(pair => solver.BooleanValue(pair.Value))
            .Select(pair => pair.Key).ToArray();

        PackingResult Result(SolveStatus status, string[]? selected = null, bool countProven = false) =>
            PackingVerifier.Verify(problem, selected ?? [], status, countProven, clock.Elapsed);
    }

    private static SolveStatus Map(CpSolverStatus status) => status switch
    {
        CpSolverStatus.Infeasible => SolveStatus.Infeasible,
        CpSolverStatus.ModelInvalid => SolveStatus.InvalidModel,
        _ => SolveStatus.Unknown
    };
}
