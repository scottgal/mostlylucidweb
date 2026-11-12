using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ResourcePacking;

public sealed record ComponentWork(int Components, int SolvedComponents, int ReusedComponents,
    int SolvedRequests, int SolvedCandidates);

public sealed record IncrementalPackingResult(PackingResult Selection, ComponentWork Work);

// One instance retains proven solutions for the components in one planning session.
public sealed class IncrementalPackingSolver(IPackingSolver solver) : IPackingSolver
{
    private sealed record Component(Request[] Requests, Candidate[] Candidates, DimensionId[] Dimensions,
        CandidateConflict[] Conflicts);

    private readonly object _gate = new();
    private Dictionary<string, PackingResult> _previous = new(StringComparer.Ordinal);

    public PackingResult Solve(PackingProblem problem, TimeSpan budget, CancellationToken cancellationToken = default) =>
        SolveWithStatistics(problem, budget, cancellationToken).Selection;

    public IncrementalPackingResult SolveWithStatistics(PackingProblem problem, TimeSpan budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(budget, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var clock = Stopwatch.StartNew();
            var components = Partition(problem, cancellationToken);
            var retained = new Dictionary<string, PackingResult>(StringComparer.Ordinal);
            var parts = new List<PackingResult>();
            var solved = 0;
            var reused = 0;
            var requests = 0;
            var candidates = 0;

            foreach (var component in components)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fingerprint = Fingerprint(problem, component);
                PackingResult result;
                if (_previous.TryGetValue(fingerprint, out var cached))
                {
                    result = cached;
                    reused++;
                }
                else
                {
                    var part = new PackingProblem(component.Dimensions.Select(dimension =>
                            new Capacity(dimension, problem.Capacities[dimension])), component.Requests,
                        component.Candidates.Select(candidate => candidate with
                        {
                            PreferenceFeatures = new[] { problem.Utilities[candidate.Id] }
                        }), [1], problem.Snapshot, problem.CandidatesComplete, component.Conflicts);
                    var remaining = budget - clock.Elapsed;
                    if (remaining > TimeSpan.Zero)
                    {
                        result = solver.Solve(part, remaining, cancellationToken);
                        ValidateOutcome(part, result);
                        solved++;
                        requests += component.Requests.Length;
                        candidates += component.Candidates.Length;
                    }
                    else
                    {
                        result = PackingVerifier.Verify(part, [], SolveStatus.Unknown, false, TimeSpan.Zero);
                    }
                }

                // Retain only proofs. A feasible incumbent can benefit from another search next time.
                if (result.Status is SolveStatus.Optimal or SolveStatus.Infeasible)
                    retained.Add(fingerprint, result);
                parts.Add(result);
            }

            var status = CombinedStatus(parts);
            var selected = status is SolveStatus.Optimal or SolveStatus.Feasible
                ? parts.SelectMany(part => part.Selected)
                : [];
            var countProven = (status is SolveStatus.Optimal or SolveStatus.Feasible) &&
                              parts.All(part => part.CountProvenOptimal);
            var combined = PackingVerifier.Verify(problem, selected, status, countProven, clock.Elapsed);
            cancellationToken.ThrowIfCancellationRequested();
            _previous = retained;
            return new IncrementalPackingResult(combined,
                new ComponentWork(components.Count, solved, reused, requests, candidates));
        }
    }

    private static List<Component> Partition(PackingProblem problem, CancellationToken cancellationToken)
    {
        var byRequest = problem.Candidates.Values.ToLookup(candidate => candidate.RequestId);
        var dimensions = problem.Requests.Keys.ToDictionary(id => id, id => byRequest[id]
            .SelectMany(candidate => candidate.Demands).Select(demand => demand.Dimension).ToHashSet(),
            StringComparer.Ordinal);
        var neighbours = dimensions.SelectMany(pair => pair.Value.Select(dimension => (dimension, Request: pair.Key)))
            .ToLookup(pair => pair.dimension, pair => pair.Request);
        var conflictNeighbours = problem.Conflicts.SelectMany(conflict => new[]
        {
            (Request: problem.Candidates[conflict.First].RequestId, Other: problem.Candidates[conflict.Second].RequestId),
            (Request: problem.Candidates[conflict.Second].RequestId, Other: problem.Candidates[conflict.First].RequestId)
        }).ToLookup(pair => pair.Request, pair => pair.Other, StringComparer.Ordinal);
        var unvisited = problem.Requests.Keys.ToHashSet(StringComparer.Ordinal);
        var components = new List<Component>();
        foreach (var first in problem.Requests.Keys.Order(StringComparer.Ordinal))
        {
            if (!unvisited.Remove(first)) continue;
            var queue = new Queue<string>();
            queue.Enqueue(first);
            var members = new List<Request>();
            var resources = new HashSet<DimensionId>();
            while (queue.TryDequeue(out var request))
            {
                cancellationToken.ThrowIfCancellationRequested();
                members.Add(problem.Requests[request]);
                foreach (var dimension in dimensions[request])
                {
                    if (!resources.Add(dimension)) continue;
                    foreach (var neighbour in neighbours[dimension])
                        if (unvisited.Remove(neighbour)) queue.Enqueue(neighbour);
                }
                foreach (var neighbour in conflictNeighbours[request])
                    if (unvisited.Remove(neighbour)) queue.Enqueue(neighbour);
            }

            var memberIds = members.Select(member => member.Id).ToHashSet(StringComparer.Ordinal);
            var choices = members.SelectMany(request => byRequest[request.Id]).ToArray();
            components.Add(new Component(members.ToArray(), choices, resources.ToArray(),
                problem.Conflicts.Where(conflict => memberIds.Contains(problem.Candidates[conflict.First].RequestId))
                    .ToArray()));
        }
        return components;
    }

    private static string Fingerprint(PackingProblem problem, Component component)
    {
        // Canonical ordering makes this independent of dictionary order and event version numbers.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var request in component.Requests.OrderBy(request => request.Id, StringComparer.Ordinal))
            {
                writer.WriteStringValue(request.Id);
                writer.WriteBooleanValue(request.Required);
            }
            writer.WriteStartArray();
            foreach (var dimension in component.Dimensions.OrderBy(dimension => dimension.Value, StringComparer.Ordinal))
            {
                writer.WriteStringValue(dimension.Value);
                writer.WriteNumberValue(problem.Capacities[dimension]);
            }
            writer.WriteEndArray();
            writer.WriteStartArray();
            foreach (var candidate in component.Candidates.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
            {
                writer.WriteStartArray();
                writer.WriteStringValue(candidate.Id);
                writer.WriteStringValue(candidate.RequestId);
                writer.WriteNumberValue(problem.Utilities[candidate.Id]);
                foreach (var demand in candidate.Demands.OrderBy(demand => demand.Dimension.Value, StringComparer.Ordinal))
                {
                    writer.WriteStringValue(demand.Dimension.Value);
                    writer.WriteNumberValue(demand.Units);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteStartArray();
            foreach (var conflict in component.Conflicts)
            {
                writer.WriteStringValue(conflict.First);
                writer.WriteStringValue(conflict.Second);
            }
            writer.WriteEndArray();
            writer.WriteEndArray();
        }
        return Convert.ToHexString(SHA256.HashData(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length))));
    }

    private static void ValidateOutcome(PackingProblem component, PackingResult result)
    {
        var verified = PackingVerifier.Verify(component, result.Selected, result.Status, result.CountProvenOptimal,
            result.Elapsed);
        if (result.Utility != verified.Utility || !result.Unfulfilled.Order(StringComparer.Ordinal)
                .SequenceEqual(verified.Unfulfilled, StringComparer.Ordinal))
            throw new InvalidOperationException("The component solver returned inconsistent objective values.");
    }

    private static SolveStatus CombinedStatus(IReadOnlyList<PackingResult> parts)
    {
        if (parts.Any(part => part.Status == SolveStatus.InvalidModel)) return SolveStatus.InvalidModel;
        if (parts.Any(part => part.Status == SolveStatus.Infeasible)) return SolveStatus.Infeasible;
        if (parts.Any(part => part.Status == SolveStatus.Unknown)) return SolveStatus.Unknown;
        return parts.All(part => part.Status == SolveStatus.Optimal) ? SolveStatus.Optimal : SolveStatus.Feasible;
    }
}
