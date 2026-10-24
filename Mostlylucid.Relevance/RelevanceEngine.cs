using System.Text.Json;

namespace Mostlylucid.Relevance;

/// <summary>Maintains live entity summaries and ranks configured candidates from their current evidence.</summary>
/// <remarks>
/// All reads and updates pass through one bounded worker. Awaiting a save establishes visibility for a subsequent read.
/// The engine owns its store; caller-supplied converters, predictors and workers remain caller-owned.
/// Scores rank candidates; only the separate destination predictor estimates outcome probabilities.
/// </remarks>
public sealed class RelevanceEngine : IAsyncDisposable
{
    private readonly RelevanceDefinition definition;
    private readonly IRelevanceStore store;
    private readonly TimeProvider clock;
    private readonly string schema;
    private readonly RelevanceWorker worker;
    private readonly bool ownsWorker;
    private readonly IDestinationPredictor predictor;
    private int disposed;
    private readonly Dictionary<string, (IPointConverter Converter, string Signature, int Dimensions)> points = [];

    /// <summary>Creates an engine, taking a frozen copy of the configuration and ownership of the store.</summary>
    /// <param name="definition">Validated streams, representations, placements and optional destinations.</param>
    /// <param name="store">Exclusive store lifetime owned by this engine after construction succeeds.</param>
    /// <param name="converters">Additional converters with unique names; built-in names cannot be replaced.</param>
    /// <param name="clock">Clock for decay, retention and event-time validation; defaults to UTC system time.</param>
    /// <param name="worker">An already started, externally owned worker, or null to create and start one.</param>
    /// <param name="predictor">Caller-owned strategy; defaults to trajectory prediction when configured, otherwise the Markov baseline.</param>
    /// <exception cref="ArgumentException">Configuration, converter names or dimensions are invalid.</exception>
    public RelevanceEngine(RelevanceDefinition definition, IRelevanceStore store,
        IEnumerable<IPointConverter>? converters = null, TimeProvider? clock = null, RelevanceWorker? worker = null,
        IDestinationPredictor? predictor = null)
    {
        // Freeze caller-owned configuration so runtime mutation cannot bypass version checks.
        this.definition = JsonSerializer.Deserialize<RelevanceDefinition>(JsonSerializer.Serialize(definition))!;
        this.definition.Validate();
        this.store = store;
        this.clock = clock ?? TimeProvider.System;
        this.predictor = predictor ?? (this.definition.Destinations?.Trajectory is null
            ? new MarkovDestinationPredictor() : new TrajectoryDestinationPredictor());
        var registry = new IPointConverter[] { new NumericConverter(), new CategoricalConverter(), new PrecomputedConverter(),
                new FormulaConverter(), new BooleanConverter(), new OklabConverter() }
            .Concat(converters ?? []).ToDictionary(c => c.Name, StringComparer.Ordinal);
        foreach (var (name, point) in this.definition.Points)
        {
            if (!registry.TryGetValue(point.Converter, out var converter)) throw new ArgumentException($"Unknown converter '{point.Converter}'.");
            var dimensions = converter.Dimensions(point);
            if (dimensions is < 1 or > 65_536) throw new ArgumentException("Point dimensions must be between 1 and 65536.");
            points[name] = (converter, RelevanceDefinition.Hash(new { point, converter.Version, dimensions }), dimensions);
        }
        if (this.definition.Destinations?.Trajectory is { } trajectory)
        {
            if (trajectory.Points.Any(p => !points.ContainsKey(p)) || trajectory.Points.Sum(p => points[p].Dimensions) > 64
                || this.definition.Streams.Values.Any(s => s.Kind == "observation" && s.PointSource != "input"))
                throw new ArgumentException("Trajectories require declared points (at most 64 coordinates) and input-sourced observations.");
        }
        // Include storage semantics as well as vector semantics. Old deduplication layouts need explicit replay.
        schema = RelevanceDefinition.Hash(new { StorageContract = 2, this.definition, Points = points.Select(p => new { p.Key, p.Value.Signature }).ToArray() });
        this.worker = worker ?? new RelevanceWorker();
        ownsWorker = worker is null;
        if (ownsWorker) this.worker.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Upserts a candidate, observes an event, or records attributed feedback through a named stream.</summary>
    /// <typeparam name="T">An ordinary JSON-serializable application type; property names are case-sensitive.</typeparam>
    /// <param name="source">A stream name declared in configuration, not an entity ID.</param>
    /// <param name="value">Candidate data or the mapped observation/feedback envelope.</param>
    /// <param name="cancellationToken">Cancels queue admission or work before commit; cannot undo a committed update.</param>
    /// <returns>A receipt after conversion and atomic commit, including whether an event was already accepted.</returns>
    /// <remarks>
    /// The input is serialized immediately, so later caller mutations cannot change queued work.
    /// Candidates upsert by ID. Observations and feedback deduplicate by stream and event ID within retention;
    /// reuse with different content fails. Supply stable IDs when retrying unacknowledged operations.
    /// Observations must be ordered per subject; omitted timestamps use processing time.
    /// Feedback requires a retained recommendation ID, a returned candidate and a reward in [0,1].
    /// The caller owns exposure tracking; one final attributed outcome is accepted per result/candidate.
    /// </remarks>
    /// <exception cref="ArgumentException">The stream, input, event order or feedback attribution is invalid.</exception>
    /// <exception cref="InvalidOperationException">A configured capacity or stored-schema constraint is violated.</exception>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    public ValueTask<SaveReceipt> SaveAsync<T>(string source, T value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        var data = JsonSerializer.SerializeToElement(value);
        return worker.EnqueueAsync(ct => SaveCoreAsync(source, data, ct), cancellationToken);
    }

    private async ValueTask<SaveReceipt> SaveCoreAsync(string source, JsonElement data, CancellationToken cancellationToken)
    {
        if (!definition.Streams.TryGetValue(source, out var stream)) throw new ArgumentException($"Unknown stream '{source}'.");
        var id = RequiredString(data, stream.Path("id"));
        var unmeasuredEndpoint = false;
        if (stream.Kind == "observation" && definition.Destinations?.Trajectory is { } trajectory)
        {
            var next = DataValues.TryRead(data, stream.Path("state"), out var state) ? state.GetString()
                : DataValues.Read(data, stream.Path("type")).GetString();
            unmeasuredEndpoint = next is not null && definition.Destinations.Outcomes.Contains(next)
                && !trajectory.Points.Any(p => DataValues.TryRead(data, definition.Points[p].Path, out _));
        }
        var encoded = stream.Kind == "candidate" || stream.Kind == "observation" && stream.PointSource == "input" && !unmeasuredEndpoint
            ? await EncodeCandidateAsync(id, data, stream.Kind == "candidate", cancellationToken) : null;
        var now = clock.GetUtcNow();
        return await store.TransactAsync(tx =>
        {
            tx.BindSchema(definition.Id, schema);
            tx.Prune(now);
            if (stream.Kind == "candidate")
            {
                tx.Upsert(definition.Id, encoded!);
                if (tx.Count(definition.Id, "candidate") > definition.MaxCandidates) throw new InvalidOperationException("Candidate capacity reached.");
                return new SaveReceipt(id);
            }
            var hash = RelevanceDefinition.Hash(data);
            // Hash the structured identity: source="events:part", id="x" must differ from source="events", id="part:x".
            var eventKey = "event:" + RelevanceDefinition.Hash(new { source, id });
            if (!tx.Accept(definition.Id, eventKey, hash, now.AddHours(definition.StateRetentionHours))) return new SaveReceipt(id, true);
            if (tx.Count(definition.Id, "dedup") > definition.MaxDeduplicationRecords) throw new InvalidOperationException("Deduplication capacity reached.");
            if (stream.Kind == "observation") Observe(tx, stream, data, encoded, now);
            else Feedback(tx, stream, data, now);
            if (tx.Count(definition.Id, "dedup") > definition.MaxDeduplicationRecords) throw new InvalidOperationException("Deduplication capacity reached.");
            return new SaveReceipt(id);
        }, cancellationToken);
    }

    /// <summary>Ranks eligible candidates against a subject's current per-point summaries.</summary>
    /// <param name="subject">The entity/session key used by observations.</param>
    /// <param name="placement">A configured recommendation surface with mandatory filters and head weights.</param>
    /// <param name="filter">Additional constraints on declared filter fields; combined with placement rules using AND.</param>
    /// <param name="count">Maximum returned items, from 1 through the configured candidate window.</param>
    /// <param name="cancellationToken">Cancels queue admission or work before the decision record commits.</param>
    /// <returns>A retained decision ID plus ranked items and their per-head contributions.</returns>
    /// <remarks>
    /// Eligibility applies before each head's top-K limit. Weighted RRF scores are not probabilities.
    /// The candidate window bounds the fusion pool, so fusion is not exhaustive over every eligible item.
    /// Without usable evidence, eligible candidates return in ordinal ID order with zero scores and no reasons.
    /// Calling this method records a decision for feedback attribution; it does not record actual exposure.
    /// Filters are snapshotted at submission so caller mutations cannot alter queued decisions.
    /// </remarks>
    /// <exception cref="ArgumentException">The subject, placement or filter is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The requested count is outside the configured bounds.</exception>
    public ValueTask<RecommendationResult> RecommendAsync(string subject, string placement,
        Filter? filter = null, int count = 10, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        var snapshot = Filter.Snapshot(filter);
        return worker.EnqueueAsync(ct => RecommendCoreAsync(subject, placement, snapshot, count, ct), cancellationToken);
    }

    private ValueTask<RecommendationResult> RecommendCoreAsync(string subject, string placement,
        Filter? filter, int count, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.");
        if (!definition.Placements.TryGetValue(placement, out var area)) throw new ArgumentException($"Unknown placement '{placement}'.");
        if (count < 1 || count > definition.CandidateLimit) throw new ArgumentOutOfRangeException(nameof(count));
        definition.ValidateFilter(filter);
        var eligibility = Filter.All(new[] { area.BuildFilter(), filter }.OfType<Filter>().ToArray());
        var now = clock.GetUtcNow();
        return store.TransactAsync(tx =>
        {
            tx.BindSchema(definition.Id, schema);
            tx.Prune(now);
            var state = tx.Get<SubjectState>(definition.Id, "state", subject);
            var weights = tx.Get<Dictionary<string, HeadEvidence>>(definition.Id, "weights", placement) ?? [];
            var merged = new Dictionary<string, (Candidate Candidate, List<Contribution> Reasons)>();
            if (state is not null)
                foreach (var (point, baseWeight) in area.Heads)
                {
                    if (!state.Points.TryGetValue(point, out var summary)) continue;
                    var mass = summary.Mass * Decay(now - state.At);
                    if (mass < definition.MinEvidence) continue;
                    var vector = summary.Sum.Select(v => (float)(v / summary.Mass)).ToArray();
                    if (definition.Points[point].Metric == "cosine" && vector.All(v => v == 0)) continue;
                    var query = new VectorQuery(point, points[point].Signature, definition.Points[point].Metric, vector);
                    var neighbors = tx.Nearest(definition.Id, query, eligibility, definition.CandidateLimit);
                    var evidence = weights.GetValueOrDefault(point) ?? new HeadEvidence();
                    // A bounded, smoothed reliability multiplier. It is not a calibrated probability.
                    var weight = baseWeight * (0.5 + (1 + evidence.Positive) / (2 + evidence.Positive + evidence.Negative));
                    for (var rank = 0; rank < neighbors.Count; rank++)
                    {
                        var neighbor = neighbors[rank];
                        if (!merged.TryGetValue(neighbor.Candidate.Id, out var entry)) entry = (neighbor.Candidate, []);
                        entry.Reasons.Add(new Contribution(point, rank + 1, weight, neighbor.Distance));
                        merged[neighbor.Candidate.Id] = entry;
                    }
                }
            var items = merged.Values.Select(entry => new Recommendation(entry.Candidate.Id,
                    entry.Reasons.Sum(r => r.Weight / (definition.RrfConstant + r.Rank)), entry.Candidate.Data, entry.Reasons))
                .OrderByDescending(r => r.Score).ThenBy(r => r.Id, StringComparer.Ordinal).Take(count).ToArray();
            // Cold start/expired evidence uses deterministic eligible candidates, explicitly without personal scores.
            if (items.Length == 0)
                items = tx.Eligible(definition.Id, eligibility, count)
                    .Select(c => new Recommendation(c.Id, 0, c.Data, Array.Empty<Contribution>())).ToArray();
            var result = new RecommendationResult(Guid.NewGuid().ToString("N"), subject, placement, items);
            tx.Put(definition.Id, "decision", result.Id, result, now.AddHours(definition.DecisionRetentionHours));
            if (tx.Count(definition.Id, "decision") > definition.MaxDecisions) throw new InvalidOperationException("Decision capacity reached.");
            return result;
        }, cancellationToken);
    }

    /// <summary>Estimates configured destination outcomes from a consistent snapshot of retained evidence.</summary>
    /// <param name="subject">The entity/session key whose last observed state starts the forecast.</param>
    /// <param name="cancellationToken">Cancellation for queue admission, snapshot acquisition and prediction.</param>
    /// <returns>Outcome estimates, unresolved mass, support and the observation timestamp.</returns>
    /// <remarks>
    /// The Markov baseline uses an event-count horizon. Configured trajectory prediction uses seconds from ObservedAt,
    /// measured centroid motion, and completed-episode reference populations. Expired trajectory windows abstain.
    /// Sparse evidence abstains. Estimates are not calibrated by the engine, and distance is never renamed probability.
    /// The injected predictor runs outside the store transaction but still occupies the single worker.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Destination states have not been configured.</exception>
    public ValueTask<DestinationPrediction> PredictAsync(string subject, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.");
        if (definition.Destinations is null) throw new InvalidOperationException("Configure destinations before requesting probabilities.");
        return worker.EnqueueAsync(async ct =>
        {
            var snapshot = await store.TransactAsync(tx =>
            {
                tx.BindSchema(definition.Id, schema);
                tx.Prune(clock.GetUtcNow());
                return new PredictionContext(subject, tx.Get<SubjectState>(definition.Id, "state", subject),
                    tx.Get<TransitionModel>(definition.Id, "transitions", "model") ?? new TransitionModel(), definition.Destinations, clock.GetUtcNow());
            }, ct);
            return await predictor.PredictAsync(snapshot, ct);
        }, cancellationToken);
    }

    private async ValueTask<Candidate> EncodeCandidateAsync(string id, JsonElement data, bool validateMetadata, CancellationToken cancellationToken)
    {
        foreach (var field in validateMetadata ? definition.Filters : [])
        {
            var v = DataValues.Read(data, field);
            if (v.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
                throw new ArgumentException($"Filter property '{field}' must be a non-null scalar.");
            if (v.ValueKind == JsonValueKind.Number && !double.IsFinite(v.GetDouble()))
                throw new ArgumentException($"Filter property '{field}' must be finite.");
        }
        var encoded = new Dictionary<string, EncodedPoint>();
        foreach (var (name, point) in definition.Points)
        {
            var (converter, signature, dimensions) = points[name];
            var vector = await converter.ConvertAsync(DataValues.Read(data, point.Path), point, cancellationToken);
            VectorMath.Validate(vector, dimensions);
            if (point.Metric == "cosine" && vector.All(v => v == 0)) throw new ArgumentException("Cosine points cannot be zero vectors.");
            encoded[name] = new EncodedPoint(signature, point.Metric, vector);
        }
        return new Candidate(id, data, encoded);
    }

    private void Observe(IRelevanceTransaction tx, StreamDefinition stream, JsonElement data, Candidate? encoded, DateTimeOffset now)
    {
        var subject = RequiredString(data, stream.Path("subject"));
        var type = RequiredString(data, stream.Path("type"));
        if (!stream.Weights.TryGetValue(type, out var weight)) throw new ArgumentException($"Unknown event type '{type}'.");
        var at = DataValues.TryRead(data, stream.Path("at"), out var timestamp) ? timestamp.GetDateTimeOffset() : now;
        if (at > now || at < now.AddHours(-definition.StateRetentionHours)) throw new ArgumentException("Observation time is future-dated or outside retention.");
        var state = tx.Get<SubjectState>(definition.Id, "state", subject) ?? new SubjectState { At = at };
        if (at < state.At) throw new ArgumentException("Out-of-order events are rejected; order a subject's stream before ingestion.");
        var decay = Decay(at - state.At);
        foreach (var summary in state.Points.Values)
        {
            for (var i = 0; i < summary.Sum.Length; i++) summary.Sum[i] *= decay;
            summary.Mass *= decay;
        }
        if (weight > 0 && !(stream.PointSource == "input" && encoded is null))
        {
            var candidate = encoded ?? tx.Get<Candidate>(definition.Id, "candidate", RequiredString(data, stream.Path("candidate"))) ?? throw new ArgumentException("Unknown candidate.");
            foreach (var (point, vector) in candidate.Points)
            {
                if (!state.Points.TryGetValue(point, out var summary)) summary = new PointSummary { Sum = new double[vector.Values.Length] };
                for (var i = 0; i < vector.Values.Length; i++) summary.Sum[i] += weight * vector.Values[i];
                summary.Mass += weight;
                if (!double.IsFinite(summary.Mass) || summary.Sum.Any(v => !double.IsFinite(v))) throw new ArgumentException("Evidence overflow.");
                state.Points[point] = summary;
            }
        }
        if (definition.Destinations is { } destinations)
        {
            var next = DataValues.TryRead(data, stream.Path("state"), out var explicitState) ? explicitState.GetString() : type;
            if (next is null || !destinations.States.Contains(next)) throw new ArgumentException("Unknown transition state.");
            if (destinations.Trajectory is { } trajectory)
                ObserveTrajectory(tx, state, stream, data, encoded, next, at, now, trajectory, destinations);
            var model = tx.Get<TransitionModel>(definition.Id, "transitions", "model") ?? new TransitionModel();
            if (state.LastState is { } previous && !destinations.Outcomes.Contains(previous))
            {
                var row = model.Counts.GetValueOrDefault(previous) ?? [];
                row[next] = row.GetValueOrDefault(next) + 1;
                model.Counts[previous] = row;
                tx.Put(definition.Id, "transitions", "model", model, now.AddHours(definition.StateRetentionHours));
            }
            state.LastState = next;
        }
        state.At = at;
        tx.Put(definition.Id, "state", subject, state, at.AddHours(definition.StateRetentionHours));
        if (tx.Count(definition.Id, "state") > definition.MaxStateRecords) throw new InvalidOperationException("Active state capacity reached.");
    }

    private void ObserveTrajectory(IRelevanceTransaction tx, SubjectState state, StreamDefinition stream, JsonElement data,
        Candidate? input, string next, DateTimeOffset at, DateTimeOffset now, TrajectoryDefinition trajectory, DestinationDefinition destinations)
    {
        state.Trajectory ??= new TrajectoryState();
        if (state.Trajectory.Closed) throw new ArgumentException("The trajectory episode is closed; use a new subject ID for a new episode.");
        if (input is null)
        {
            if (!destinations.Outcomes.Contains(next)) throw new ArgumentException("Measurements are required for a non-terminal trajectory observation.");
            // An outcome closes the known journey. It is not a new measurement and must not invent zero velocity.
            LearnTrajectoryOutcome(tx, state.Trajectory, next, at, now, trajectory);
            return;
        }
        var group = DataValues.TryRead(data, stream.Path("baseline_group"), out var groupValue)
            ? groupValue.GetString() : trajectory.BaselineStates.Count > 0
                ? throw new ArgumentException("Supply baseline_group to keep unrelated normal populations separate.") : "population";
        if (string.IsNullOrWhiteSpace(group)) throw new ArgumentException("Reference group cannot be blank.");
        var vector = trajectory.Points.SelectMany(p => input.Points[p].Values.Select(v => (double)v)).ToArray();
        var frame = TrajectoryTracking.Advance(state.Trajectory, vector, next, group, at, trajectory);
        if (trajectory.BaselineStates.Count > 0)
        {
            var reference = tx.Get<NormalReference>(definition.Id, "normal", group) ?? new NormalReference();
            if (trajectory.BaselineStates.Contains(next))
            {
                reference.Observe(vector, at, trajectory.BaselineHalfLifeSeconds);
                tx.Put(definition.Id, "normal", group, reference, now.AddHours(definition.StateRetentionHours));
                if (tx.Count(definition.Id, "normal") > definition.MaxStateRecords) throw new InvalidOperationException("Normal reference capacity reached.");
            }
            frame = frame with { Normal = reference.Evidence(frame.Centroid, at, trajectory) };
        }
        state.Trajectory.Current = frame;
        if (!destinations.Outcomes.Contains(next))
        {
            if (frame.MotionReady)
            {
                _ = TrajectoryTracking.Features(frame, trajectory); // Reject invalid unit scales before retaining a landmark.
                state.Trajectory.Landmarks.TryAdd(next, frame);
            }
            return;
        }
        LearnTrajectoryOutcome(tx, state.Trajectory, next, at, now, trajectory);
    }

    private void LearnTrajectoryOutcome(IRelevanceTransaction tx, TrajectoryState state, string outcome,
        DateTimeOffset at, DateTimeOffset now, TrajectoryDefinition trajectory)
    {
        var model = tx.Get<TransitionModel>(definition.Id, "transitions", "model") ?? new TransitionModel();
        model.Trajectories ??= new TrajectoryModel();
        TrajectoryTracking.Learn(model.Trajectories, state, outcome, at, trajectory);
        tx.Put(definition.Id, "transitions", "model", model, now.AddHours(definition.StateRetentionHours));
        state.Closed = true;
    }

    private void Feedback(IRelevanceTransaction tx, StreamDefinition stream, JsonElement data, DateTimeOffset now)
    {
        var decisionId = RequiredString(data, stream.Path("recommendation"));
        var candidateId = RequiredString(data, stream.Path("candidate"));
        var reward = DataValues.Read(data, stream.Path("reward")).GetDouble();
        if (!double.IsFinite(reward) || reward is < 0 or > 1) throw new ArgumentException("Reward must be between 0 and 1.");
        var decision = tx.Get<RecommendationResult>(definition.Id, "decision", decisionId) ?? throw new ArgumentException("Recommendation is unknown or expired.");
        var item = decision.Items.SingleOrDefault(r => r.Id == candidateId) ?? throw new ArgumentException("Feedback candidate was not returned in this recommendation.");
        // Exactly one attributed outcome per displayed candidate, even with different transport event IDs.
        var outcomeKey = "outcome:" + RelevanceDefinition.Hash(new { decisionId, candidateId });
        if (!tx.Accept(definition.Id, outcomeKey, RelevanceDefinition.Hash(reward), now.AddHours(definition.DecisionRetentionHours))) return;
        var weights = tx.Get<Dictionary<string, HeadEvidence>>(definition.Id, "weights", decision.Placement) ?? [];
        var total = item.Reasons.Sum(r => 1.0 / r.Rank);
        foreach (var reason in item.Reasons)
        {
            var evidence = weights.GetValueOrDefault(reason.Head) ?? new HeadEvidence();
            // Bound old outcome evidence and allocate credit among heads supporting this item.
            var credit = (1.0 / reason.Rank) / total;
            evidence.Positive = 0.99 * evidence.Positive + reward * credit;
            evidence.Negative = 0.99 * evidence.Negative + (1 - reward) * credit;
            weights[reason.Head] = evidence;
        }
        tx.Put(definition.Id, "weights", decision.Placement, weights, now.AddHours(definition.StateRetentionHours));
    }

    private double Decay(TimeSpan elapsed) => Math.Pow(0.5, Math.Max(0, elapsed.TotalMinutes) / definition.HalfLifeMinutes);
    private static string RequiredString(JsonElement data, string path)
    {
        var result = DataValues.Read(data, path).GetString();
        return !string.IsNullOrWhiteSpace(result) ? result : throw new ArgumentException($"'{path}' must be a non-empty string.");
    }
    /// <summary>Stops and drains an owned worker, then disposes the owned store.</summary>
    /// <remarks>
    /// An external worker is not stopped. Its owner must finish accepted operations before disposing engines that use it.
    /// Converter and predictor lifetimes remain with the caller or dependency-injection container.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        if (ownsWorker)
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
        await store.DisposeAsync();
    }
}

/// <summary>Persisted per-entity evidence supplied to destination strategies as a detached snapshot.</summary>
public sealed class SubjectState
{
    /// <summary>Optional timestamped kinematics and pending first-state landmarks for the current episode.</summary>
    public TrajectoryState? Trajectory { get; set; }
    /// <summary>Timestamp of the latest accepted observation; decay is measured from this instant.</summary>
    public DateTimeOffset At { get; set; }
    /// <summary>Last explicit transition state, or event type when no state was supplied.</summary>
    public string? LastState { get; set; }
    /// <summary>Weighted summaries keyed by configured point name.</summary>
    public Dictionary<string, PointSummary> Points { get; set; } = [];
}
/// <summary>A positive weighted vector sum and its supporting mass; the mean is Sum / Mass.</summary>
public sealed class PointSummary
{
    /// <summary>Decayed weighted coordinate sums, without unit-length normalization.</summary>
    public double[] Sum { get; set; } = [];
    /// <summary>Decayed event weight; an expiry signal rather than calibrated confidence.</summary>
    public double Mass { get; set; }
}
/// <summary>Bounded heuristic outcome evidence for one placement/head; not a causal estimator.</summary>
public sealed class HeadEvidence
{
    /// <summary>Decayed positive credit allocated from attributed outcomes.</summary>
    public double Positive { get; set; }
    /// <summary>Decayed negative credit allocated from attributed outcomes.</summary>
    public double Negative { get; set; }
}
