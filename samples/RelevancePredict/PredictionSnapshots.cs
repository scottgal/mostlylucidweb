namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>Sample-only inspection adapter: the core supplies a detached context; no new engine read API is required.</summary>
public sealed class CaptureTransitions : IDestinationPredictor
{
    private readonly MarkovDestinationPredictor baseline = new();
    public TransitionModel Snapshot { get; private set; } = new();
    public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
    {
        Snapshot = Copy(context.Transitions);
        return baseline.PredictAsync(context, cancellationToken);
    }
    public static TransitionModel Copy(TransitionModel model) => new()
    {
        Counts = model.Counts.ToDictionary(row => row.Key, row => new Dictionary<string, double>(row.Value))
    };
}

/// <summary>Freezes only population counts for a held-out evaluation. New subjects still use the ordinary save/predict API.</summary>
public sealed class FrozenTransitions(TransitionModel training) : IDestinationPredictor
{
    private readonly TransitionModel frozen = CaptureTransitions.Copy(training);
    private readonly MarkovDestinationPredictor baseline = new();
    public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default) =>
        baseline.PredictAsync(context with { Transitions = frozen }, cancellationToken);
}

/// <summary>A fixed clock keeps retention, decay and generated output repeatable; this is not the production clock.</summary>
public sealed class FixtureClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => FixtureGenerator.Epoch.AddHours(12);
}
