using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mostlylucid.Relevance.Learning;

/// <summary>Detects context groups, assigns actual relevance candidates, and learns from attributed observations.</summary>
/// <remarks>Owns the supplied store and a bounded RelevanceWorker. The relevance engine remains caller-owned.
/// Separate queues allow awaited calls to the recommender without recursively awaiting the same worker.
/// Rewards describe completed assignment-level outcomes, including people with no observed view.</remarks>
public sealed class InterventionLearning : IAsyncDisposable
{
    private readonly LearningDefinition definition;
    private readonly IRelevanceStore store;
    private readonly RelevanceEngine relevance;
    private readonly RelevanceWorker worker = new();
    private readonly TimeProvider clock;
    private readonly Random random;
    private readonly string scope;
    private readonly string schema;
    private int disposed;

    /// <summary>Creates a persistent learner over an existing candidate/response-configured relevance engine.</summary>
    /// <param name="definition">Frozen axes, objective version, policy settings and bounds.</param>
    /// <param name="store">Exclusive lifetime transferred after construction succeeds.</param>
    /// <param name="relevance">Caller-owned recommender; its candidates are interventions.</param>
    /// <param name="clock">Optional clock for repeatable event-time experiments.</param>
    /// <param name="random">Independent assignment random stream; not the simulator's outcome generator.</param>
    public InterventionLearning(LearningDefinition definition, IRelevanceStore store, RelevanceEngine relevance,
        TimeProvider? clock = null, Random? random = null)
    {
        this.definition = Copy(definition);
        var d = this.definition;
        if (string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Version)
            || d.ContextAxes.Length is < 1 or > 16 || d.InterventionAxes.Length is < 1 or > 16
            || d.ContextAxes.Distinct().Count() != d.ContextAxes.Length || d.InterventionAxes.Distinct().Count() != d.InterventionAxes.Length
            || d.ContextAxes.Concat(d.InterventionAxes).Any(string.IsNullOrWhiteSpace)
            || d.MaxGroups is < 1 or > 64 || d.MaxVariants is < 2 or > 64 || d.MinOutcomes < 1 || d.MaxRecords < 100
            || !Positive(d.GroupRadius) || !Positive(d.DescriptorRadius) || !Positive(d.RidgePenalty)
            || !Positive(d.HorizonSeconds) || !Positive(d.RetentionHours) || d.HorizonSeconds > d.RetentionHours * 3600
            || !double.IsFinite(d.Exploration) || d.Exploration is <= 0 or > 1)
            throw new ArgumentException("Invalid learning schemas, policy, timescales or capacities.");
        this.store = store; this.relevance = relevance;
        this.clock = clock ?? TimeProvider.System; this.random = random ?? Random.Shared;
        scope = "learning:" + d.Id;
        schema = Hash(new { Contract = 1, Definition = d });
        worker.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>Registers an immutable descriptor and upserts its envelope into the existing recommendation stream.</summary>
    /// <remarks>A content change requires a new ID. Registration is serialised; orphan candidates after a crash are not eligible
    /// until the catalog commit succeeds. Retrying the identical registration completes that commit.</remarks>
    public ValueTask<SaveReceipt> RegisterAsync(InterventionVariant variant, CancellationToken cancellationToken = default)
    {
        var frozen = Copy(variant);
        return Queue(async ct =>
        {
            Id(frozen.Id); Vector(frozen.Features, definition.InterventionAxes.Length);
            if (frozen.Candidate.ValueKind != JsonValueKind.Object || !frozen.Candidate.TryGetProperty("id", out var id)
                || id.GetString() != frozen.Id) throw new ArgumentException("Candidate envelope id must match the variant.");
            var exists = await Transaction(tx =>
            {
                var model = Model(tx);
                var previous = tx.Get<InterventionVariant>(scope, "variant", frozen.Id);
                if (previous is not null && Hash(previous) != Hash(frozen))
                    throw new ArgumentException("Variant identity reused for different content or descriptors.");
                if (model.Variants.TryGetValue(frozen.Id, out var old))
                {
                    if (Hash(tx.Get<InterventionVariant>(scope, "variant", old.Id)) != Hash(frozen)) throw new ArgumentException("Variant identity reused for different content or descriptors.");
                    return true;
                }
                if (model.Variants.Count >= definition.MaxVariants || frozen.Control && model.Variants.Values.Any(v => v.Control))
                    throw new ArgumentException("Variant capacity reached or a control already exists.");
                return false;
            }, ct);
            if (exists) return new SaveReceipt(frozen.Id, true);
            await relevance.SaveAsync(definition.CandidateStream, frozen.Candidate, ct);
            await Transaction(tx =>
            {
                var model = Model(tx);
                // Keep large HTML out of the frequently updated sufficient-statistic aggregate.
                model.Variants.Add(frozen.Id, frozen with { Candidate = JsonSerializer.SerializeToElement(new { id = frozen.Id }) });
                tx.Put(scope, "variant", frozen.Id, frozen, DateTimeOffset.MaxValue);
                PutModel(tx, model); return true;
            }, ct);
            return new SaveReceipt(frozen.Id);
        }, cancellationToken);
    }

    /// <summary>Detects the observed group, ranks intervention candidates and durably assigns a variant with its propensity.</summary>
    /// <remarks>Reusing the same request ID/content returns the original assignment. One assignment per Unit within retention.
    /// A positive exploration floor preserves control/action coverage; randomised requests bypass adaptive exploitation.</remarks>
    public ValueTask<InterventionDecision> ChooseAsync(InterventionRequest request, CancellationToken cancellationToken = default)
    {
        var frozen = Copy(request);
        return Queue(async ct =>
        {
            Id(frozen.Id); Id(frozen.Unit); Vector(frozen.Coordinates, definition.ContextAxes.Length);
            CheckTime(frozen.ObservedAt);
            await FlushResponsesAsync(ct);
            var hash = Hash(frozen);
            var existing = await Transaction(tx => tx.Get<DecisionState>(scope, "decision", frozen.Id), ct);
            if (existing is not null)
            {
                if (existing.RequestHash != hash) throw new ArgumentException("Decision ID reused with different context.");
                return existing.Decision;
            }
            var prepared = await Transaction(tx =>
            {
                if (tx.Get<string>(scope, "unit", frozen.Unit) is not null)
                    throw new ArgumentException("Assignment unit already has a decision; retry its ID or start a new episode.");
                var model = Model(tx);
                var group = Detect(model, frozen);
                var variants = model.Variants.Values.Where(v => frozen.Eligible is null || frozen.Eligible.Contains(v.Id)).ToArray();
                if (variants.Length < 2 || variants.Count(v => v.Control) != 1) throw new ArgumentException("Include the control and at least one eligible variant.");
                // Only commit the group observation with the final assignment below.
                return new Prepared(hash, group.Id, group.Count + 1, variants);
            }, ct);
            // This is the existing recommender, operating ON the emails/configuration candidates.
            var ranked = await relevance.RankAsync(prepared.Group, definition.Placement, count: definition.MaxVariants, cancellationToken: ct);
            return await Transaction(tx =>
            {
                var model = Model(tx);
                var group = Detect(model, frozen); group.Observe(frozen.Coordinates);
                if (!model.Groups.Contains(group)) model.Groups.Add(group);
                var ranks = ranked.Select((item, index) => (item.Id, Rank: index + 1)).ToDictionary(p => p.Id, p => p.Rank);
                var variants = prepared.Variants.Where(v => ranks.ContainsKey(v.Id)).ToArray();
                if (variants.Length < 2 || variants.All(v => !v.Control)) throw new ArgumentException("Recommender eligibility removed the control or all alternatives.");
                var coefficients = group.Coefficients(definition.RidgePenalty);
                var scores = variants.Select(v => Score(model, group, v, coefficients, ranks[v.Id])).ToArray();
                var best = scores.OrderByDescending(s => s.Score).ThenBy(s => s.RelevanceRank).ThenBy(s => s.Id, StringComparer.Ordinal).First().Id;
                var uniform = frozen.Randomised || group.Outcomes < definition.MinOutcomes;
                var explore = uniform || random.NextDouble() < definition.Exploration;
                var selected = explore ? variants[random.Next(variants.Length)] : variants.Single(v => v.Id == best);
                var probability = uniform ? 1.0 / variants.Length
                    : definition.Exploration / variants.Length + (selected.Id == best ? 1 - definition.Exploration : 0);
                var now = clock.GetUtcNow();
                var exact = tx.Get<InterventionVariant>(scope, "variant", selected.Id) ?? throw new InvalidOperationException("Missing registered variant.");
                var decision = new InterventionDecision(frozen.Id, frozen.Unit, group.Id, group.Count, frozen.Coordinates,
                    exact, now, now.AddSeconds(definition.HorizonSeconds), probability,
                    uniform ? "uniform-v1" : "epsilon-descriptor-v1", scores);
                tx.Put(scope, "decision", frozen.Id, new DecisionState { RequestHash = hash, Decision = decision }, Expiry());
                tx.Put(scope, "unit", frozen.Unit, frozen.Id, Expiry());
                var arm = Arm(model, group.Id, selected.Id); arm.Assigned++;
                model.Recent.Add(frozen.Id); if (model.Recent.Count > 24) model.Recent.RemoveAt(0);
                PutModel(tx, model);
                if (tx.Count(scope, "decision") > definition.MaxRecords) throw new InvalidOperationException("Decision capacity reached.");
                return decision;
            }, ct);
        }, cancellationToken);
    }

    /// <summary>Records an observed stage or final outcome, atomically deduplicating its ledger and sufficient-statistic update.</summary>
    /// <remarks>Outcome rewards belong to every assigned unit with an observed endpoint, including no-view cases.
    /// Censored units supply no invented reward. Late non-terminal evidence may be appended after closure.
    /// Positive-response affinity reaches Relevance through a retry-safe persistent outbox.</remarks>
    public ValueTask<SaveReceipt> SaveAsync(InterventionEvent observation, CancellationToken cancellationToken = default)
    {
        var frozen = Copy(observation);
        return Queue(async ct =>
        {
            Id(frozen.Id); CheckTime(frozen.At);
            if (frozen.Stage is not ("attempted" or "delivered" or "applied" or "viewed" or "clicked" or "failed" or "outcome" or "censored")
                || string.IsNullOrWhiteSpace(frozen.Source)) throw new ArgumentException("Unknown stage or missing evidence source.");
            if (frozen.After is not null) Vector(frozen.After, definition.ContextAxes.Length);
            if (frozen.Measures?.Values.Any(v => !double.IsFinite(v)) == true) throw new ArgumentException("Non-finite measurement.");
            if (frozen.Stage == "outcome" ? frozen.Reward is null || !double.IsFinite(frozen.Reward.Value)
                || frozen.Reward is < 0 or > 1 || string.IsNullOrWhiteSpace(frozen.Outcome) : frozen.Reward is not null)
                throw new ArgumentException("Only a defined observed outcome supplies a finite reward in [0,1].");
            var receipt = await Transaction(tx =>
            {
                if (!tx.Accept(scope, "event:" + frozen.Id, Hash(frozen), Expiry())) return new SaveReceipt(frozen.Id, true);
                var row = tx.Get<DecisionState>(scope, "decision", frozen.Decision) ?? throw new ArgumentException("Unknown or expired decision.");
                if (frozen.At < row.Decision.AssignedAt || frozen.Stage == "outcome" && frozen.At > row.Decision.Deadline)
                    throw new ArgumentException("Evidence precedes assignment or its outcome is outside the defined window.");
                if (row.Events.Count >= 64 || row.Closed && frozen.Stage is "outcome" or "censored")
                    throw new ArgumentException("Episode already closed or its event capacity was reached.");
                var model = Model(tx); var arm = Arm(model, row.Decision.Group, row.Decision.Variant.Id);
                if (row.Stages.Add(frozen.Stage))
                    switch (frozen.Stage)
                    {
                        case "applied": case "delivered":
                            if (!row.Events.Any(e => e.Stage is "applied" or "delivered")) arm.Applied++;
                            break;
                        case "viewed": arm.Viewed++; break;
                        case "clicked": arm.Clicked++; break;
                        case "failed": arm.Failed++; break;
                    }
                row.Events.Add(frozen);
                if (frozen.Stage == "outcome")
                {
                    arm.Completed++; arm.Sum += frozen.Reward!.Value; arm.SumSquares += frozen.Reward.Value * frozen.Reward.Value;
                    model.Groups.Single(g => g.Id == row.Decision.Group).Learn(row.Decision.Variant.Features, frozen.Reward.Value);
                    row.Closed = true;
                    if (frozen.Reward > 0) model.PendingResponses.Add(row.Decision.Id);
                }
                if (frozen.Stage == "censored") { arm.Censored++; row.Closed = true; }
                tx.Put(scope, "decision", frozen.Decision, row, Expiry()); PutModel(tx, model);
                if (tx.Count(scope, "dedup") > definition.MaxRecords * 16L) throw new InvalidOperationException("Event capacity reached.");
                return new SaveReceipt(frozen.Id);
            }, ct);
            await FlushResponsesAsync(ct);
            return receipt;
        }, cancellationToken);
    }

    /// <summary>Returns detached group, arm and recent-journey evidence after earlier queued work, including pending affinity replay.</summary>
    public ValueTask<LearningSnapshot> SnapshotAsync(CancellationToken cancellationToken = default) => Queue(async ct =>
    {
        await FlushResponsesAsync(ct);
        return await Transaction(tx =>
        {
            var model = Model(tx);
            var evidence = model.Groups.SelectMany(g => model.Variants.Keys.Select(id =>
            {
                var arm = model.Arms.GetValueOrDefault(Key(g.Id, id)) ?? new ArmEvidence();
                double? mean = arm.Completed == 0 ? null : arm.Sum / arm.Completed;
                double? se = arm.Completed < 2 ? null : Math.Sqrt(Math.Max(0, (arm.SumSquares - arm.Sum * arm.Sum / arm.Completed)
                    / (arm.Completed - 1)) / arm.Completed);
                return new InterventionEvidence(g.Id, id, arm.Assigned, arm.Completed, arm.Censored, arm.Applied, arm.Viewed, arm.Clicked, arm.Failed, mean, se);
            })).ToArray();
            return new LearningSnapshot(model.Groups.Select(g => new DetectedGroup(g.Id, g.Count, g.Mean, g.Coefficients(definition.RidgePenalty))).ToArray(),
                evidence, model.Recent.AsEnumerable().Reverse().Select(id => tx.Get<DecisionState>(scope, "decision", id)).OfType<DecisionState>()
                    .Select(d => new InterventionJourney(d.Decision, d.Events.OrderBy(e => e.At).ToArray(), d.Closed)).ToArray(),
                model.Variants.Keys.Select(id => tx.Get<InterventionVariant>(scope, "variant", id)!).ToArray());
        }, ct);
    }, cancellationToken);

    /// <summary>Reads a retained assignment by ID, including delayed outcomes after it leaves the recent UI window.</summary>
    public ValueTask<InterventionJourney?> GetAsync(string decisionId, CancellationToken cancellationToken = default) => Queue(ct =>
        Transaction<InterventionJourney?>(tx =>
        {
            var row = tx.Get<DecisionState>(scope, "decision", decisionId);
            return row is null ? null : new(row.Decision, row.Events.OrderBy(e => e.At).ToArray(), row.Closed);
        }, ct), cancellationToken);

    private InterventionScore Score(LearningState model, ContextGroup group, InterventionVariant variant, double[] coefficients, int rank)
    {
        var arm = model.Arms.GetValueOrDefault(Key(group.Id, variant.Id)) ?? new ArmEvidence();
        double? mean = arm.Completed == 0 ? null : arm.Sum / arm.Completed;
        var supported = group.Outcomes >= definition.MinOutcomes && model.Variants.Values.Any(v =>
            (model.Arms.GetValueOrDefault(Key(group.Id, v.Id))?.Completed ?? 0) >= 3 && Distance(v.Features, variant.Features) <= definition.DescriptorRadius);
        var estimate = supported ? Math.Clamp(coefficients[0] + variant.Features.Select((v, i) => v * coefficients[i + 1]).Sum(), 0, 1) : mean ?? 0;
        return new(variant.Id, estimate, arm.Completed, mean, rank, supported ? "descriptor-regression" : mean.HasValue ? "observed-mean" : "relevance-only");
    }

    private ContextGroup Detect(LearningState model, InterventionRequest request)
    {
        var manual = request.ExplicitGroup is null ? null : "explicit-" + Hash(request.ExplicitGroup)[..12];
        var nearest = manual is null ? model.Groups.Where(g => !g.Id.StartsWith("explicit-", StringComparison.Ordinal))
            .OrderBy(g => Distance(g.Mean, request.Coordinates)).FirstOrDefault() : model.Groups.FirstOrDefault(g => g.Id == manual);
        if (nearest is not null && (manual is not null || Distance(nearest.Mean, request.Coordinates) <= definition.GroupRadius)) return nearest;
        if (model.Groups.Count >= definition.MaxGroups) throw new InvalidOperationException("No supported group within radius; group capacity reached.");
        return new ContextGroup { Id = manual ?? $"detected-{model.Groups.Count + 1:000}", Mean = request.Coordinates.ToArray() };
    }
    private async ValueTask FlushResponsesAsync(CancellationToken ct)
    {
        while (true)
        {
            var next = await Transaction(tx => { var m = Model(tx); return m.PendingResponses.Count == 0 ? null : m.PendingResponses[0]; }, ct);
            if (next is null) return;
            var row = await Transaction(tx => tx.Get<DecisionState>(scope, "decision", next), ct);
            if (row is not null)
                await relevance.SaveAsync(definition.ResponseStream, new { id = "observed:" + next, subject = row.Decision.Group,
                    candidate = row.Decision.Variant.Id, type = definition.ResponseType }, ct);
            await Transaction(tx => { var m = Model(tx); m.PendingResponses.Remove(next); PutModel(tx, m); return true; }, ct);
        }
    }
    private ValueTask<T> Transaction<T>(Func<IRelevanceTransaction, T> operation, CancellationToken ct) => store.TransactAsync(tx =>
    { tx.BindSchema(scope, schema); tx.Prune(clock.GetUtcNow()); return operation(tx); }, ct);
    private LearningState Model(IRelevanceTransaction tx) => tx.Get<LearningState>(scope, "model", "current") ?? new();
    private void PutModel(IRelevanceTransaction tx, LearningState model) => tx.Put(scope, "model", "current", model, Expiry());
    private static ArmEvidence Arm(LearningState model, string group, string variant)
    { var key = Key(group, variant); if (!model.Arms.TryGetValue(key, out var arm)) model.Arms[key] = arm = new(); return arm; }
    private static string Key(string group, string variant) => Hash(new[] { group, variant });
    private DateTimeOffset Expiry() => clock.GetUtcNow().AddHours(definition.RetentionHours);
    private void CheckTime(DateTimeOffset at)
    { if (at > clock.GetUtcNow() || at < clock.GetUtcNow().AddHours(-definition.RetentionHours)) throw new ArgumentException("Evidence time is future-dated or expired."); }
    private static bool Positive(double v) => double.IsFinite(v) && v > 0;
    private static double Distance(double[] a, double[] b) => Math.Sqrt(a.Select((value, i) => (value - b[i]) * (value - b[i])).Average());
    private static void Id(string id) { if (string.IsNullOrWhiteSpace(id) || id.Length > 160) throw new ArgumentException("Supply a bounded non-empty ID."); }
    private static void Vector(double[] vector, int dimensions)
    { if (vector.Length != dimensions || vector.Any(v => !double.IsFinite(v) || v is < 0 or > 1)) throw new ArgumentException("Use finite [0,1] coordinates in the declared order."); }
    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private ValueTask<T> Queue<T>(Func<CancellationToken, ValueTask<T>> operation, CancellationToken ct)
    { ObjectDisposedException.ThrowIf(disposed != 0, this); return worker.EnqueueAsync(operation, ct); }
    /// <summary>Drains accepted work, then closes the owned store. Does not dispose the external relevance engine.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        await worker.StopAsync(CancellationToken.None); worker.Dispose(); await store.DisposeAsync();
    }
}
