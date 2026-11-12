using Mostlylucid.Packing.Planning;

namespace AdaptiveAllocation;

// Values and costs use explicit fixture utility points. They are not provider prices or calibrated probabilities.
public sealed record AllocationOption(string Id, string RequestId, IReadOnlyList<Consumption> Demands,
    long Value, long Cost = 0, long ChangeCost = 0, long Preference = 0);

public static class AllocationModel
{
    public static PackingProblem Build(IEnumerable<Capacity> capacities, IEnumerable<Request> requests,
        IEnumerable<AllocationOption> options, long revision = 0) =>
        new(capacities, requests, options.Select(option =>
        {
            ArgumentOutOfRangeException.ThrowIfNegative(option.Value);
            ArgumentOutOfRangeException.ThrowIfNegative(option.Cost);
            ArgumentOutOfRangeException.ThrowIfNegative(option.ChangeCost);
            ArgumentOutOfRangeException.ThrowIfNegative(option.Preference);
            return new Candidate(option.Id, option.RequestId, option.Demands,
                [option.Value, -option.Cost, -option.ChangeCost, option.Preference]);
        }), [1, 1, 1, 1], new PlanningSnapshot("adaptive-example", 1, revision, 0, 1));

    public static DimensionId Dimension(string id) => new(id);
}
