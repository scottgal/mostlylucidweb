using System.Diagnostics;

namespace ResourcePacking;

public enum SolveStatus { Optimal, Feasible, Infeasible, Unknown, InvalidModel }

public sealed record PackingResult(SolveStatus Status, IReadOnlyList<string> Selected, IReadOnlyList<string> Unfulfilled,
    long Utility, bool CountProvenOptimal, bool CandidatesComplete, PlanningSnapshot? Snapshot, TimeSpan Elapsed);

public interface IPackingSolver
{
    PackingResult Solve(PackingProblem problem, TimeSpan budget, CancellationToken cancellationToken = default);
}

public static class PackingVerifier
{
    public static PackingResult Verify(PackingProblem problem, IEnumerable<string> selected,
        SolveStatus status, bool countProvenOptimal, TimeSpan elapsed)
    {
        var ids = selected.Order(StringComparer.Ordinal).ToArray();
        var chosen = ids.ToHashSet(StringComparer.Ordinal);
        if (problem.Conflicts.Any(conflict => chosen.Contains(conflict.First) && chosen.Contains(conflict.Second)))
            throw new InvalidOperationException("The selected candidates conflict.");
        var fulfilled = new HashSet<string>(StringComparer.Ordinal);
        var used = new Dictionary<DimensionId, long>();
        long utility = 0;

        foreach (var id in ids)
        {
            if (!problem.Candidates.TryGetValue(id, out var candidate) || !fulfilled.Add(candidate.RequestId))
                throw new InvalidOperationException($"Invalid or repeated selection '{id}'.");
            utility = checked(utility + problem.Utilities[id]);
            foreach (var demand in candidate.Demands)
            {
                var total = checked(used.GetValueOrDefault(demand.Dimension) + demand.Units);
                if (total > problem.Capacities[demand.Dimension])
                    throw new InvalidOperationException($"Capacity exceeded on '{demand.Dimension.Value}'.");
                used[demand.Dimension] = total;
            }
        }

        var unfulfilled = problem.Requests.Keys.Except(fulfilled, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToArray();
        if (status is SolveStatus.Optimal or SolveStatus.Feasible)
        {
            if (unfulfilled.Any(id => problem.Requests[id].Required))
                throw new InvalidOperationException("A required request was left unfulfilled.");
        }
        else if (ids.Length != 0)
        {
            throw new InvalidOperationException("A result without a feasible plan cannot contain selections.");
        }

        return new PackingResult(status, Array.AsReadOnly(ids), Array.AsReadOnly(unfulfilled), utility,
            countProvenOptimal, problem.CandidatesComplete, problem.Snapshot, elapsed);
    }
}

public sealed class GreedyPackingSolver : IPackingSolver
{
    public PackingResult Solve(PackingProblem problem, TimeSpan budget, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(budget, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();
        var clock = Stopwatch.StartNew();
        var remaining = problem.Capacities.ToDictionary();
        var choices = problem.Candidates.Values.ToLookup(candidate => candidate.RequestId);
        var selected = new List<string>();
        var chosen = new HashSet<string>(StringComparer.Ordinal);
        var conflicts = problem.Conflicts.SelectMany(conflict => new[]
        {
            (Candidate: conflict.First, Other: conflict.Second),
            (Candidate: conflict.Second, Other: conflict.First)
        }).ToLookup(pair => pair.Candidate, pair => pair.Other, StringComparer.Ordinal);

        foreach (var request in problem.Requests.Values.OrderByDescending(request => request.Required)
                     .ThenBy(request => request.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (clock.Elapsed >= budget) break;
            var choice = choices[request.Id].OrderByDescending(candidate => problem.Utilities[candidate.Id])
                .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
                .FirstOrDefault(candidate => candidate.Demands.All(demand => demand.Units <= remaining[demand.Dimension]) &&
                                             !conflicts[candidate.Id].Any(chosen.Contains));
            if (choice is null) continue;
            foreach (var demand in choice.Demands) remaining[demand.Dimension] -= demand.Units;
            selected.Add(choice.Id);
            chosen.Add(choice.Id);
        }

        var fulfilled = selected.Select(id => problem.Candidates[id].RequestId).ToHashSet(StringComparer.Ordinal);
        if (problem.Requests.Values.Any(request => request.Required && !fulfilled.Contains(request.Id)))
            return PackingVerifier.Verify(problem, [], SolveStatus.Unknown, false, clock.Elapsed);
        return PackingVerifier.Verify(problem, selected, SolveStatus.Feasible, false, clock.Elapsed);
    }
}
