namespace AdaptiveAllocation;

public sealed record ObservedOutcome(string Id, string Task, string Supplier, bool Succeeded);
public sealed record OutcomeEstimate(int Completed, int Successes, double? SmoothedSuccess, string Source);

// An application-side baseline. Relevance ranking is deliberately not converted to a success probability.
public sealed class OutcomeMemory(int minimumSupport = 3, int capacity = 10_000)
{
    private readonly Dictionary<string, ObservedOutcome> _events = new(StringComparer.Ordinal);
    private readonly int _minimumSupport = minimumSupport > 0 ? minimumSupport
        : throw new ArgumentOutOfRangeException(nameof(minimumSupport));
    private readonly int _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));

    public void Observe(ObservedOutcome outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome.Task);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome.Supplier);
        if (_events.TryGetValue(outcome.Id, out var previous))
        {
            if (previous != outcome) throw new InvalidOperationException("Conflicting outcome retry.");
            return;
        }
        if (_events.Count == _capacity) throw new InvalidOperationException("The bounded outcome fixture is full.");
        _events.Add(outcome.Id, outcome);
    }

    public OutcomeEstimate Estimate(string task, string supplier)
    {
        var completed = _events.Values.Where(outcome => outcome.Task == task && outcome.Supplier == supplier).ToArray();
        var successes = completed.Count(outcome => outcome.Succeeded);
        return new(completed.Length, successes,
            completed.Length >= _minimumSupport ? (successes + 1.0) / (completed.Length + 2.0) : null,
            completed.Length >= _minimumSupport ? "observed-task-supplier" : "insufficient-evidence");
    }
}
