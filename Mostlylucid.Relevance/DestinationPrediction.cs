namespace Mostlylucid.Relevance;

/// <summary>An estimated probability of reaching one configured destination within the prediction horizon.</summary>
/// <param name="Destination">Configured absorbing outcome state.</param>
/// <param name="Probability">Mass assigned to this outcome, from zero to one.</param>
public sealed record DestinationProbability(string Destination, double Probability);
/// <summary>Finite-horizon outcome estimates plus explicit unresolved probability and evidence support.</summary>
/// <param name="Subject">Entity/session key.</param>
/// <param name="CurrentState">Latest observed state, or null with no retained state.</param>
/// <param name="HorizonEvents">Maximum future event transitions considered, not elapsed time.</param>
/// <param name="Outcomes">Estimated outcome masses; do not renormalize away unresolved probability.</param>
/// <param name="RemainingProbability">Mass not assigned to destinations, including unsupported transitions.</param>
/// <param name="SupportingTransitions">Outgoing transitions for Markov prediction, or matched episode landmarks for trajectory prediction; not calibrated confidence.</param>
/// <param name="Status">Markov: no_state, insufficient_evidence or estimated. Temporal: no_trajectory, insufficient_motion,
/// insufficient_trajectory_evidence, stale_trajectory or estimated_trajectory. Both report observed_destination for a known endpoint.</param>
/// <param name="ObservedAt">When the entity's starting state was observed; allows callers to assess staleness.</param>
/// <param name="IsCalibrated">False for the baseline; external strategies may declare independently validated calibration.</param>
/// <param name="HorizonSeconds">Elapsed-time horizon when trajectory prediction is enabled; HorizonEvents is then zero.</param>
/// <param name="Trajectory">Actual timestamped motion and normal-reference evidence when configured.</param>
public sealed record DestinationPrediction(string Subject, string? CurrentState, int HorizonEvents,
    IReadOnlyList<DestinationProbability> Outcomes, double RemainingProbability, double SupportingTransitions, string Status,
    DateTimeOffset? ObservedAt = null, bool IsCalibrated = false, double? HorizonSeconds = null, TrajectoryFrame? Trajectory = null);

/// <summary>Pooled empirical first-order counts, cumulative while the engine model record remains active.</summary>
public sealed class TransitionModel
{
    /// <summary>Optional bounded populations for elapsed-time trajectory forecasts.</summary>
    public TrajectoryModel? Trajectories { get; set; }
    /// <summary>Source-state to target-state observation counts; destinations have no learned outgoing transitions.</summary>
    public Dictionary<string, Dictionary<string, double>> Counts { get; set; } = [];
}

/// <summary>Detached evidence snapshot passed to an optional predictor; treat supplied configuration as read-only.</summary>
/// <param name="Subject">Entity being predicted.</param>
/// <param name="State">Retained state, or null for an unknown/expired entity.</param>
/// <param name="Transitions">Transition counts pooled over this engine identity.</param>
/// <param name="Definition">Configured states, absorbing destinations and prediction horizon.</param>
/// <param name="RequestedAt">Query clock used to reject an elapsed-time forecast whose observation window has already expired.</param>
public sealed record PredictionContext(string Subject, SubjectState? State, TransitionModel Transitions, DestinationDefinition Definition,
    DateTimeOffset? RequestedAt = null);

/// <summary>Replaceable outcome inference over a snapshot; caller/DI owns its lifetime.</summary>
public interface IDestinationPredictor
{
    /// <summary>Estimates outcomes, preserving unresolved mass and identifying insufficient evidence explicitly.</summary>
    /// <remarks>Runs outside the storage transaction but on the engine worker. Do not call back into that engine.</remarks>
    ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default);
}

// Finite-horizon absorbing Markov baseline. Estimates probabilities from observed transitions;
// does not infer calibrated outcome probabilities from geometric distances.
/// <summary>Propagates a first-order Markov distribution with absorbing outcomes and sparse-row abstention.</summary>
public sealed class MarkovDestinationPredictor : IDestinationPredictor
{
    /// <inheritdoc />
    public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
    {
        var definition = context.Definition;
        var current = context.State?.LastState;
        var support = current is not null && context.Transitions.Counts.TryGetValue(current, out var source) ? source.Values.Sum() : 0;
        var distribution = definition.States.ToDictionary(s => s, _ => 0.0);
        var remaining = current is null ? 1.0 : 0.0;
        if (current is not null) distribution[current] = 1;
        for (var step = 0; step < definition.HorizonEvents; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = definition.States.ToDictionary(s => s, _ => 0.0);
            foreach (var (state, mass) in distribution)
            {
                if (mass == 0) continue;
                if (definition.Outcomes.Contains(state)) { next[state] += mass; continue; }
                var row = context.Transitions.Counts.GetValueOrDefault(state) ?? [];
                var count = row.Values.Sum();
                if (count < definition.MinTransitions) { remaining += mass; continue; }
                var total = count + definition.Smoothing * definition.States.Count;
                foreach (var target in definition.States)
                    next[target] += mass * (row.GetValueOrDefault(target) + definition.Smoothing) / total;
            }
            distribution = next;
        }
        remaining += distribution.Where(p => !definition.Outcomes.Contains(p.Key)).Sum(p => p.Value);
        var outcomes = definition.Outcomes.Select(d => new DestinationProbability(d, distribution[d])).ToArray();
        var status = current is null ? "no_state" : definition.Outcomes.Contains(current) ? "observed_destination"
            : support < definition.MinTransitions ? "insufficient_evidence" : "estimated";
        return ValueTask.FromResult(new DestinationPrediction(context.Subject, current, definition.HorizonEvents,
            outcomes, Math.Clamp(remaining, 0, 1), support, status, context.State?.At));
    }
}
