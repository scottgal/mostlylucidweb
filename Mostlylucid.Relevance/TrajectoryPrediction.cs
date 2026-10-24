using System.Globalization;

namespace Mostlylucid.Relevance;

/// <summary>Explicit units, smoothing timescales and bounded experience for elapsed-time destination forecasts.</summary>
public sealed class TrajectoryDefinition
{
    /// <summary>Ordered configured points forming the behavioural vector. Converters determine their units.</summary>
    public List<string> Points { get; set; } = [];
    /// <summary>Forecast window in seconds, anchored at the latest observation, never a number of events.</summary>
    public double HorizonSeconds { get; set; } = 60;
    /// <summary>Fast behavioural centroid half-life in seconds.</summary>
    public double HalfLifeSeconds { get; set; } = 5;
    /// <summary>Gaps above this duration reset derivatives and pending landmarks rather than implying continuous observation.</summary>
    public double MaxGapSeconds { get; set; } = 300;
    /// <summary>Scale for centroid coordinates when matching trajectory contexts.</summary>
    public double PositionScale { get; set; } = 1;
    /// <summary>Coordinate units per second corresponding to one matching-feature unit.</summary>
    public double VelocityScale { get; set; } = .01;
    /// <summary>Coordinate units per second squared corresponding to one matching-feature unit.</summary>
    public double AccelerationScale { get; set; } = .001;
    /// <summary>Episode seconds corresponding to one matching-feature unit.</summary>
    public double ElapsedScaleSeconds { get; set; } = 60;
    /// <summary>Width of a context cell in the explicitly scaled feature space.</summary>
    public double BinWidth { get; set; } = .5;
    /// <summary>Largest RMS feature distance for admitting a historical context to the reference population.</summary>
    public double MaxDistance { get; set; } = 1;
    /// <summary>Maximum nearest supported context cells combined in a forecast.</summary>
    public int NearestCohorts { get; set; } = 8;
    /// <summary>Minimum completed episodes in the matched population; otherwise the forecast abstains.</summary>
    public int MinExamples { get; set; } = 20;
    /// <summary>Maximum context cells; the least recently updated cell is evicted when capacity is reached.</summary>
    public int MaxCohorts { get; set; } = 2048;
    /// <summary>Only these explicitly qualified states may update the normal reference. Empty disables reference learning.</summary>
    public List<string> BaselineStates { get; set; } = [];
    /// <summary>Slow normal-reference half-life in seconds, independent of the fast behavioural centroid.</summary>
    public double BaselineHalfLifeSeconds { get; set; } = 3600;
    /// <summary>Minimum effective normal-reference mass before reporting a standardised deviation.</summary>
    public double MinBaselineMass { get; set; } = 10;
    /// <summary>Positive variance floor in squared coordinate units; prevents division by zero for constant healthy signals.</summary>
    public double MinBaselineVariance { get; set; } = .0001;

    internal void Validate(DestinationDefinition destinations)
    {
        var positive = new[] {HorizonSeconds, HalfLifeSeconds, MaxGapSeconds, PositionScale, VelocityScale,
            AccelerationScale, ElapsedScaleSeconds, BinWidth, MaxDistance, BaselineHalfLifeSeconds, MinBaselineMass, MinBaselineVariance};
        if (Points.Count is < 1 or > 16 || Points.Distinct().Count() != Points.Count
            || positive.Any(v => !double.IsFinite(v) || v <= 0) || HorizonSeconds > 86400 * 30
            || NearestCohorts is < 1 or > 64 || MinExamples < 1 || MaxCohorts is < 1 or > 10000
            || NearestCohorts > MaxCohorts || BaselineStates.Any(s => !destinations.States.Contains(s))
            || BaselineStates.Any(destinations.Outcomes.Contains) || destinations.Outcomes.Contains("unresolved"))
            throw new ArgumentException("Invalid trajectory timescales, units, capacities or baseline states.");
    }
}

/// <summary>A timestamped, prefix-only behavioural summary. Derivatives are per second and per second squared.</summary>
public sealed record TrajectoryFrame(DateTimeOffset At, string State, string Group, double[] Position, double[] Centroid,
    double[]? Velocity, double[]? Acceleration, double IntervalSeconds, double ElapsedSeconds,
    NormalReferenceEvidence? Normal = null)
{
    /// <summary>Three observations with usable time intervals are needed for both velocity and acceleration.</summary>
    public bool MotionReady => Velocity is not null && Acceleration is not null;
}

/// <summary>Observed normal reference, its effective support, and RMS standardised displacement; never an incident probability.</summary>
public sealed record NormalReferenceEvidence(double[] Centroid, double[] Variance, double EffectiveMass,
    DateTimeOffset At, double? Deviation);

/// <summary>Bounded per-episode state: current kinematics and the first motion-ready landmark for each observed state.</summary>
public sealed class TrajectoryState
{
    /// <summary>First observation in the currently continuous segment.</summary>
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>Latest observed frame, including physical-time derivatives.</summary>
    public TrajectoryFrame? Current { get; set; }
    /// <summary>One first eligible landmark per state prevents repeated observations from multiplying one episode's votes.</summary>
    public Dictionary<string, TrajectoryFrame> Landmarks { get; set; } = [];
    /// <summary>An observed destination closes the episode; further observations require a new subject/episode ID.</summary>
    public bool Closed { get; set; }
}

/// <summary>A learned context centroid with empirical within-window outcomes from completed episodes.</summary>
public sealed class TrajectoryCohort
{
    /// <summary>Observed categorical context; cohorts from different states are never mixed.</summary>
    public string State { get; set; } = "";
    /// <summary>Application-supplied population/reference group.</summary>
    public string Group { get; set; } = "";
    /// <summary>Incremental mean of the explicitly scaled motion features admitted to this cell.</summary>
    public double[] Mean { get; set; } = [];
    /// <summary>Completed episodes contributing one landmark each.</summary>
    public long Examples { get; set; }
    /// <summary>Within-window destination counts, including explicit unresolved-window observations.</summary>
    public Dictionary<string, long> Outcomes { get; set; } = [];
    /// <summary>Latest completed-episode timestamp; used for bounded-cell eviction.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Bounded adaptive reference populations; each cell receives at most one vote per episode and observed state.</summary>
public sealed class TrajectoryModel
{
    /// <summary>Versioned feature-cell identities and their learned population summaries.</summary>
    public Dictionary<string, TrajectoryCohort> Cohorts { get; set; } = [];
}

/// <summary>Decayed weighted Welford accumulator for explicitly admitted normal observations.</summary>
public sealed class NormalReference
{
    /// <summary>Latest qualified normal observation time.</summary>
    public DateTimeOffset At { get; set; }
    /// <summary>Effective observation weight after elapsed-time decay.</summary>
    public double Mass { get; set; }
    /// <summary>Slow normal centroid in converter-defined coordinate units.</summary>
    public double[] Mean { get; set; } = [];
    /// <summary>Decayed weighted squared deviations used to estimate reference variability.</summary>
    public double[] M2 { get; set; } = [];

    internal void Observe(double[] values, DateTimeOffset at, double halfLife)
    {
        if (Mean.Length == 0) { Mean = new double[values.Length]; M2 = new double[values.Length]; At = at; }
        var target = at > At ? at : At;
        var decay = Math.Pow(.5, (target - At).TotalSeconds / halfLife);
        var weight = Math.Pow(.5, (target - at).TotalSeconds / halfLife);
        Mass *= decay;
        for (var i = 0; i < values.Length; i++)
        {
            M2[i] *= decay;
            var delta = values[i] - Mean[i];
            Mean[i] += weight * delta / (Mass + weight);
            M2[i] += weight * delta * (values[i] - Mean[i]);
        }
        Mass += weight; At = target;
    }

    internal NormalReferenceEvidence? Evidence(double[] values, DateTimeOffset at, TrajectoryDefinition definition)
    {
        // A late event must not receive a reference that contains observations from its future.
        if (Mean.Length == 0 || At > at) return null;
        var mass = Mass * Math.Pow(.5, (at - At).TotalSeconds / definition.BaselineHalfLifeSeconds);
        var variance = M2.Select(v => Math.Max(0, v / Mass)).ToArray();
        double? deviation = mass >= definition.MinBaselineMass ? Math.Sqrt(values.Select((v, i) =>
            (v - Mean[i]) * (v - Mean[i]) / Math.Max(variance[i], definition.MinBaselineVariance)).Average()) : null;
        return new(Mean.ToArray(), variance, mass, At, deviation);
    }
}

internal static class TrajectoryTracking
{
    internal static TrajectoryFrame Advance(TrajectoryState state, double[] values, string next, string group,
        DateTimeOffset at, TrajectoryDefinition definition)
    {
        var previous = state.Current;
        var interval = previous is null ? 0 : (at - previous.At).TotalSeconds;
        if (previous is not null && interval <= 0) throw new ArgumentException("Trajectory observations need strictly increasing timestamps.");
        if (previous is not null && previous.Group != group) throw new ArgumentException("An episode cannot change its reference group.");
        if (previous is null || interval > definition.MaxGapSeconds)
        {
            state.StartedAt = at; state.Landmarks.Clear();
            return new(at, next, group, values, values.ToArray(), null, null, 0, 0);
        }
        var alpha = 1 - Math.Pow(.5, interval / definition.HalfLifeSeconds);
        var centroid = values.Select((value, i) => previous.Centroid[i] + alpha * (value - previous.Centroid[i])).ToArray();
        var velocity = centroid.Select((value, i) => (value - previous.Centroid[i]) / interval).ToArray();
        // Successive velocities estimate motion at interval midpoints; irregular sampling needs their midpoint separation.
        var acceleration = previous.Velocity is null ? null : velocity.Select((value, i) =>
            (value - previous.Velocity[i]) / ((interval + previous.IntervalSeconds) / 2)).ToArray();
        return new(at, next, group, values, centroid, velocity, acceleration, interval, (at - state.StartedAt).TotalSeconds);
    }

    internal static double[] Features(TrajectoryFrame frame, TrajectoryDefinition definition)
    {
        var features = frame.Centroid.Select(v => v / definition.PositionScale)
            .Concat(frame.Velocity!.Select(v => v / definition.VelocityScale))
            .Concat(frame.Acceleration!.Select(v => v / definition.AccelerationScale))
            .Append(frame.ElapsedSeconds / definition.ElapsedScaleSeconds).ToArray();
        if (features.Any(v => !double.IsFinite(v) || Math.Abs(v) > 1e100))
            throw new ArgumentException("Trajectory features exceed supported bounds; check units and configured scales.");
        return features;
    }

    internal static void Learn(TrajectoryModel model, TrajectoryState state, string outcome, DateTimeOffset at, TrajectoryDefinition definition)
    {
        foreach (var frame in state.Landmarks.Values)
        {
            var features = Features(frame, definition);
            var cell = RelevanceDefinition.Hash(new {frame.State, frame.Group,
                Bins = features.Select(v => Math.Floor(v / definition.BinWidth).ToString("R", CultureInfo.InvariantCulture)).ToArray()});
            if (!model.Cohorts.TryGetValue(cell, out var cohort))
            {
                if (model.Cohorts.Count >= definition.MaxCohorts)
                    model.Cohorts.Remove(model.Cohorts.MinBy(p => p.Value.UpdatedAt).Key);
                cohort = new() {State = frame.State, Group = frame.Group, Mean = new double[features.Length]};
                model.Cohorts[cell] = cohort;
            }
            cohort.Examples++;
            for (var i = 0; i < features.Length; i++) cohort.Mean[i] += (features[i] - cohort.Mean[i]) / cohort.Examples;
            var label = (at - frame.At).TotalSeconds <= definition.HorizonSeconds ? outcome : "unresolved";
            cohort.Outcomes[label] = cohort.Outcomes.GetValueOrDefault(label) + 1;
            cohort.UpdatedAt = at;
        }
    }
}

/// <summary>Matches position, velocity, acceleration and elapsed time to learned context populations, then reports outcome frequencies.</summary>
/// <remarks>Training uses first eligible per-state landmarks from completed episodes. Unfinished episodes do not become failures.
/// These are uncalibrated completed-episode estimates; informative loss of feedback can bias them.</remarks>
public sealed class TrajectoryDestinationPredictor : IDestinationPredictor
{
    /// <inheritdoc />
    public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
    {
        var definition = context.Definition.Trajectory ?? throw new ArgumentException("Configure a trajectory estimator.");
        var frame = context.State?.Trajectory?.Current;
        cancellationToken.ThrowIfCancellationRequested();
        DestinationPrediction Result(string status, IReadOnlyList<DestinationProbability>? outcomes = null, double remaining = 1, long support = 0) =>
            new(context.Subject, context.State?.LastState, 0,
                outcomes ?? context.Definition.Outcomes.Select(o => new DestinationProbability(o, 0)).ToArray(), remaining, support,
                status, context.State?.At, false, definition.HorizonSeconds, frame);
        if (context.State?.LastState is { } observed && context.Definition.Outcomes.Contains(observed))
            return ValueTask.FromResult(Result("observed_destination",
                context.Definition.Outcomes.Select(o => new DestinationProbability(o, o == observed ? 1 : 0)).ToArray(), 0));
        if (frame is null) return ValueTask.FromResult(Result("no_trajectory"));
        if (!frame.MotionReady) return ValueTask.FromResult(Result("insufficient_motion"));
        if (context.RequestedAt is { } now && now > frame.At.AddSeconds(definition.HorizonSeconds))
            return ValueTask.FromResult(Result("stale_trajectory"));
        var features = TrajectoryTracking.Features(frame, definition);
        var nearest = (context.Transitions.Trajectories?.Cohorts.Values.AsEnumerable() ?? Enumerable.Empty<TrajectoryCohort>())
            .Where(c => c.State == frame.State && c.Group == frame.Group)
            .Select(c => (Cohort: c, Distance: Math.Sqrt(features.Select((v, i) => (v - c.Mean[i]) * (v - c.Mean[i])).Average())))
            .Where(c => c.Distance <= definition.MaxDistance).OrderBy(c => c.Distance).Take(definition.NearestCohorts).ToArray();
        var support = nearest.Sum(c => c.Cohort.Examples);
        if (support < definition.MinExamples) return ValueTask.FromResult(Result("insufficient_trajectory_evidence", support: support));
        var probabilities = context.Definition.Outcomes.Select(outcome => new DestinationProbability(outcome,
            nearest.Sum(c => c.Cohort.Outcomes.GetValueOrDefault(outcome)) / (double)support)).ToArray();
        return ValueTask.FromResult(Result("estimated_trajectory", probabilities,
            nearest.Sum(c => c.Cohort.Outcomes.GetValueOrDefault("unresolved")) / (double)support, support));
    }
}
