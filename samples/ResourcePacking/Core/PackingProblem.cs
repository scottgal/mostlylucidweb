using System.Collections.Frozen;

namespace ResourcePacking;

public readonly record struct DimensionId(string Value);
public sealed record Capacity(DimensionId Dimension, long Available);
public sealed record Consumption(DimensionId Dimension, long Units);
public sealed record Request(string Id, bool Required = false);
public sealed record Candidate(string Id, string RequestId, IReadOnlyList<Consumption> Demands,
    IReadOnlyList<long> PreferenceFeatures);
public sealed record CandidateConflict(string First, string Second);
public sealed record PlanningSnapshot(string Venue, long PolicyVersion, long AvailabilityVersion, long Start, long End);

public sealed class PackingProblem
{
    // Keep linear expressions comfortably inside CP-SAT's integer range, including intermediate sums.
    private const long MaxMagnitude = long.MaxValue / 4;

    public IReadOnlyDictionary<DimensionId, long> Capacities { get; }
    public IReadOnlyDictionary<string, Request> Requests { get; }
    public IReadOnlyDictionary<string, Candidate> Candidates { get; }
    public IReadOnlyDictionary<string, long> Utilities { get; }
    public IReadOnlyList<CandidateConflict> Conflicts { get; }
    public PlanningSnapshot? Snapshot { get; }
    public bool CandidatesComplete { get; }

    public PackingProblem(IEnumerable<Capacity> capacities, IEnumerable<Request> requests,
        IEnumerable<Candidate> candidates, IReadOnlyList<long>? weights = null,
        PlanningSnapshot? snapshot = null, bool candidatesComplete = true,
        IEnumerable<CandidateConflict>? conflicts = null)
    {
        var coefficients = weights?.ToArray() ?? [];
        Capacities = capacities.ToFrozenDictionary(capacity =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(capacity.Dimension.Value);
            return capacity.Dimension;
        }, capacity => BoundedNonnegative(capacity.Available));
        Requests = requests.ToFrozenDictionary(request =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
            return request.Id;
        }, StringComparer.Ordinal);

        var rowTotals = new Dictionary<DimensionId, long>();
        var utilities = new Dictionary<string, long>(StringComparer.Ordinal);
        long utilityMagnitude = 0;
        Candidates = candidates.Select(candidate =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Id);
            if (!Requests.ContainsKey(candidate.RequestId))
                throw new ArgumentException($"Unknown request '{candidate.RequestId}'.");
            if (candidate.PreferenceFeatures.Count != coefficients.Length)
                throw new ArgumentException($"Candidate '{candidate.Id}' has the wrong number of features.");

            var features = candidate.PreferenceFeatures.ToArray();
            long utility = 0;
            long magnitude = 0;
            for (var index = 0; index < features.Length; index++)
            {
                var term = checked(features[index] * coefficients[index]);
                magnitude = BoundedNonnegative(checked(magnitude + Math.Abs(term)));
                utility = checked(utility + term);
            }

            utilityMagnitude = BoundedNonnegative(checked(utilityMagnitude + magnitude));
            utilities.Add(candidate.Id, utility);
            var demands = candidate.Demands.GroupBy(demand => demand.Dimension).Select(group =>
            {
                if (!Capacities.ContainsKey(group.Key) || group.Any(demand => demand.Units <= 0))
                    throw new ArgumentException($"Invalid demand on '{group.Key.Value}'.");
                var units = BoundedNonnegative(group.Sum(demand => demand.Units));
                rowTotals[group.Key] = BoundedNonnegative(checked(rowTotals.GetValueOrDefault(group.Key) + units));
                return new Consumption(group.Key, units);
            }).ToArray();

            return candidate with { Demands = Array.AsReadOnly(demands), PreferenceFeatures = Array.AsReadOnly(features) };
        }).ToFrozenDictionary(candidate => candidate.Id, StringComparer.Ordinal);
        Utilities = utilities.ToFrozenDictionary(StringComparer.Ordinal);
        Conflicts = Array.AsReadOnly((conflicts ?? []).Select(conflict =>
        {
            if (!Candidates.ContainsKey(conflict.First) || !Candidates.ContainsKey(conflict.Second) ||
                conflict.First == conflict.Second)
                throw new ArgumentException("A conflict must reference two distinct known candidates.");
            return string.CompareOrdinal(conflict.First, conflict.Second) < 0
                ? conflict
                : new CandidateConflict(conflict.Second, conflict.First);
        }).Distinct().OrderBy(conflict => conflict.First, StringComparer.Ordinal)
            .ThenBy(conflict => conflict.Second, StringComparer.Ordinal).ToArray());
        Snapshot = snapshot;
        CandidatesComplete = candidatesComplete;
    }

    private static long BoundedNonnegative(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxMagnitude);
        return value;
    }
}
