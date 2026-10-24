using System.Globalization;
using System.Text.Json;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>Versioned playback with actual event timestamps, physical-time forecasts and same-position comparison landmarks.</summary>
public sealed record TemporalReplayData(string Version, int Seed, string Domain, int TrainingVisits, double HorizonSeconds,
    IReadOnlyList<string> Outcomes, IReadOnlyList<ReplayParticipant> Participants);

/// <summary>Reproducible elapsed-time shopping and incident examples using the same engine and converters.</summary>
public static class TemporalPredictionDemo
{
    private static readonly DateTimeOffset Epoch = FixtureGenerator.Epoch;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {WriteIndented = true};

    /// <summary>Builds a two-coordinate definition. Scalars are already normalised; no neural model or hidden intent enters the engine.</summary>
    public static RelevanceDefinition Definition(bool operations = false)
    {
        var definition = RelevanceDefinition.FromYaml("""
            id: predict-time-shop-v1
            points:
              behaviour: {path: values, converter: vector, dimensions: 2, metric: l2}
            streams:
              events:
                kind: observation
                point_source: input
                weights: {view: 1, compare: 1, cart: 1, remove: 1, browse: 1, checkout: 1, purchase: 1, cancel: 1, leave: 1, sample: 1, outage: 1, recovered: 1}
            placements:
              default: {heads: {behaviour: 1}}
            destinations:
              states: [observing, checkout, healthy, degraded, purchase, cancel, leave]
              outcomes: [purchase, cancel, leave]
              trajectory:
                points: [behaviour]
                horizon_seconds: 60
                half_life_seconds: 2
                max_gap_seconds: 600
                position_scale: 0.1
                velocity_scale: 0.01
                acceleration_scale: 0.002
                elapsed_scale_seconds: 60
                max_distance: 0.3
                min_examples: 20
                baseline_half_life_seconds: 86400
                min_baseline_mass: 10
                min_baseline_variance: 0.000025
            """);
        if (operations)
        {
            definition.Id = "predict-time-operations-v1";
            definition.Destinations!.States = ["healthy", "degraded", "outage", "recovered"];
            definition.Destinations.Outcomes = ["outage", "recovered"];
            definition.Destinations.Trajectory!.BaselineStates = ["healthy"];
        }
        return definition;
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = Epoch;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Capture : IDestinationPredictor
    {
        private readonly TrajectoryDestinationPredictor inner = new();
        public TransitionModel Snapshot { get; private set; } = new();
        public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default)
        {
            Snapshot = Clone(context.Transitions);
            return inner.PredictAsync(context, cancellationToken);
        }
    }
    private sealed class Frozen(TransitionModel snapshot) : IDestinationPredictor
    {
        private readonly TransitionModel model = Clone(snapshot);
        private readonly TrajectoryDestinationPredictor inner = new();
        public ValueTask<DestinationPrediction> PredictAsync(PredictionContext context, CancellationToken cancellationToken = default) =>
            inner.PredictAsync(context with {Transitions = model}, cancellationToken);
    }
    private static TransitionModel Clone(TransitionModel source) => JsonSerializer.Deserialize<TransitionModel>(JsonSerializer.Serialize(source))!;
    private sealed record Observation(string Action, string State, double[] Values, DateTimeOffset At);

    // The simulator determines outcomes; the predictor receives only prefix values, actions and their timestamps.
    private static Observation[] Journey(int profile, string outcome, DateTimeOffset at, Random random, bool operations, bool incomplete = false, bool exact = false)
    {
        var forward = profile != 1;
        var interval = (profile == 2 ? 60 : 10) * (exact ? 1 : .94 + random.NextDouble() * .12);
        var x = forward ? new[] {.2, .35, .5} : [.8, .65, .5];
        var y = operations ? (forward ? new[] {.02, .035, .05} : [.08, .065, .05])
            : forward ? new[] {.08, .14, .2} : [.32, .26, .2];
        if (!exact)
            for (var i = 0; i < x.Length; i++)
            {
                x[i] += (random.NextDouble() - .5) * .004;
                y[i] += (random.NextDouble() - .5) * .002;
            }
        var actions = operations ? new[] {"sample", "sample", "sample"} : forward ? ["view", "compare", "cart"] : ["cart", "remove", "browse"];
        var observations = Enumerable.Range(0, 3).Select(i => new Observation(actions[i], operations ? "degraded" : "observing",
            [x[i], y[i]], at.AddSeconds(i * interval))).ToList();
        if (!operations && profile == 0) observations.Add(new("checkout", "checkout", [.75, .12], at.AddSeconds(30)));
        if (!incomplete)
        {
            var end = observations[^1].At.AddSeconds(profile == 2 ? 180 + random.Next(181) : 15 + random.Next(16));
            var values = operations ? outcome == "recovered" ? new[] {.1, .01} : [1.2, .12]
                : outcome == "purchase" ? [1, 0] : outcome == "cancel" ? [1, .5] : new[] {1.0, 1.0};
            observations.Add(new(outcome, outcome, values, end));
        }
        return observations.ToArray();
    }

    private static async Task Save(RelevanceEngine engine, Clock clock, string subject, string id, Observation observation)
    {
        clock.Now = observation.At;
        await engine.SaveAsync("events", new {id, subject, type = observation.Action, state = observation.State,
            at = observation.At, values = observation.Values, baseline_group = "reference-population"});
    }

    /// <summary>Runs both domains; --output writes real replay artifacts and stdout into a new directory.</summary>
    public static async Task RunAsync(string[] args)
    {
        var output = args.Length == 0 ? null : args.Length == 2 && args[0] == "--output" ? args[1]
            : throw new ArgumentException("Use predict-time [--output new-directory].");
        if (output is not null)
        {
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new ArgumentException("Use a new or empty output directory.");
            Directory.CreateDirectory(output);
        }
        var lines = new List<string>();
        void Log(string value) { lines.Add(value); Console.WriteLine(value); }
        foreach (var operations in new[] {false, true})
        {
            var definition = Definition(operations);
            var settings = definition.Destinations!.Trajectory!;
            var clock = new Clock();
            var capture = new Capture();
            var directory = Directory.CreateTempSubdirectory("relevance-time-");
            var database = Path.Combine(directory.FullName, "model.db");
            var random = new Random(814);
            TransitionModel trained;
            NormalReference? normal;
            try
            {
                var store = new SqliteRelevanceStore(database);
                var trainingLabels = new List<string>();
                await using (var engine = new RelevanceEngine(definition, store, clock: clock, predictor: capture))
                {
                    // Engine owns the store; the explicit bootstrap read below happens after awaited operations, before disposal.
                    if (operations)
                        for (var i = 0; i < 40; i++)
                            await Save(engine, clock, "healthy-reference", $"normal-{i}", new("sample", "healthy",
                                [.1 + (i % 3 - 1) * .002, .01 + (i % 3 - 1) * .0002], Epoch.AddHours(9).AddSeconds(i)));
                    for (var i = 0; i < 600; i++)
                    {
                        var profile = i % 3;
                        var purchase = random.NextDouble() < (profile == 1 ? .2 : .8);
                        var outcome = operations ? (purchase ? "outage" : "recovered")
                            : purchase ? "purchase" : random.NextDouble() < .5 ? "cancel" : "leave";
                        var observations = Journey(profile, outcome, Epoch.AddHours(10).AddSeconds(i), random, operations);
                        trainingLabels.Add((observations[^1].At - observations[2].At).TotalSeconds <= settings.HorizonSeconds ? outcome : "unresolved");
                        for (var j = 0; j < observations.Length; j++) await Save(engine, clock, $"train-{i}", $"train-{i}-{j}", observations[j]);
                    }
                    await engine.PredictAsync("model-snapshot");
                    trained = capture.Snapshot;
                    normal = await store.TransactAsync(tx => tx.Get<NormalReference>(definition.Id, "normal", "reference-population"));
                }
                var memory = new MemoryRelevanceStore();
                await using var replayEngine = new RelevanceEngine(definition, memory, clock: clock, predictor: new Frozen(trained));
                // Sample-only bootstrap of the qualified normal reference, kept separate from the frozen outcome model.
                if (normal is not null) await memory.TransactAsync(tx => { tx.Put(definition.Id, "normal", "reference-population", normal, DateTimeOffset.MaxValue); return true; });
                // Predict a later population at the same first motion-ready landmark, before revealing its outcome.
                var classes = definition.Destinations!.Outcomes.Append("unresolved").ToArray();
                var prior = classes.ToDictionary(c => c, c => trainingLabels.Count(x => x == c) / (double)trainingLabels.Count);
                double score = 0, priorScore = 0;
                var scored = 0;
                var evaluationRandom = new Random(129);
                for (var i = 0; i < 300; i++)
                {
                    var profile = i % 3;
                    var positive = evaluationRandom.NextDouble() < (profile == 1 ? .2 : .8);
                    var outcome = operations ? positive ? "outage" : "recovered" : positive ? "purchase" : evaluationRandom.NextDouble() < .5 ? "cancel" : "leave";
                    var incomplete = evaluationRandom.NextDouble() < .1;
                    var observations = Journey(profile, outcome, Epoch.AddHours(12).AddSeconds(i), evaluationRandom, operations, incomplete);
                    for (var j = 0; j < 3; j++) await Save(replayEngine, clock, $"holdout-{i}", $"holdout-{i}-{j}", observations[j]);
                    var prediction = await replayEngine.PredictAsync($"holdout-{i}");
                    if (incomplete) continue;
                    var actual = (observations[^1].At - observations[2].At).TotalSeconds <= settings.HorizonSeconds ? outcome : "unresolved";
                    var probabilities = prediction.Outcomes.ToDictionary(p => p.Destination, p => p.Probability);
                    probabilities["unresolved"] = prediction.RemainingProbability;
                    score += classes.Sum(c => Math.Pow(probabilities[c] - (actual == c ? 1 : 0), 2));
                    priorScore += classes.Sum(c => Math.Pow(prior[c] - (actual == c ? 1 : 0), 2));
                    scored++;
                }
                Log($"TIME HOLDOUT domain={(operations ? "operations" : "shop")} horizon=60_seconds scored={scored} censored={300 - scored} brier={score / scored:F4} training_prior={priorScore / scored:F4}");
                var participants = new List<ReplayParticipant>();
                for (var i = 0; i < 24; i++)
                {
                    var subject = i == 0 ? "a" : i == 1 ? "b" : i == 2 ? "c" : $"visit-{i + 1:00}";
                    var profile = i % 3;
                    var outcome = operations ? (profile == 1 ? "recovered" : "outage") : profile == 1 ? "leave" : "purchase";
                    if (i >= 3)
                    {
                        var firstOutcome = random.NextDouble() < (profile == 1 ? .2 : .8);
                        outcome = operations ? firstOutcome ? "outage" : "recovered"
                            : firstOutcome ? "purchase" : random.NextDouble() < .5 ? "cancel" : "leave";
                    }
                    var observations = Journey(profile, outcome, Epoch.AddHours(13).AddSeconds(i * 2), random, operations, i is 10 or 17, exact: i < 3);
                    var frames = new List<ReplayFrame> { new(null, await replayEngine.PredictAsync(subject), observations[0].At.AddSeconds(-2)) };
                    for (var j = 0; j < observations.Length; j++)
                    {
                        await Save(replayEngine, clock, subject, $"{subject}-{j}", observations[j]);
                        frames.Add(new(observations[j].Action, await replayEngine.PredictAsync(subject), observations[j].At));
                    }
                    participants.Add(new(subject, i < 3 ? new[] {"A / Moving forward", "B / Moving back", "C / Same path, slower"}[i] : $"{(operations ? "Service" : "Shopper")} {i + 1:00}", frames, 3));
                }
                var replay = new TemporalReplayData("temporal-replay-v1", 814, operations ? "operations" : "shop", 600, settings.HorizonSeconds,
                    definition.Destinations!.Outcomes, participants);
                Log($"TIME MODEL domain={replay.Domain} training_episodes=600 horizon=60_seconds centroid_half_life=2_seconds");
                foreach (var person in participants.Take(3))
                {
                    var prediction = person.Frames[3].Prediction;
                    var motion = prediction.Trajectory!;
                    var probabilities = string.Join(" ", prediction.Outcomes.Select(o => $"{o.Destination}={o.Probability.ToString("P1", CultureInfo.InvariantCulture)}"));
                    Log($"{person.Id.ToUpperInvariant()} state={prediction.CurrentState} position={motion.Position[0]:F3} velocity={motion.Velocity![0]:F5}/s acceleration={motion.Acceleration![0]:F6}/s2 elapsed={motion.ElapsedSeconds:0}s {probabilities} unresolved={prediction.RemainingProbability.ToString("P1", CultureInfo.InvariantCulture)} support={prediction.SupportingTransitions:0}");
                    if (motion.Normal is { } reference) Log($"NORMAL {person.Id.ToUpperInvariant()} centroid={reference.Centroid[0]:F4} deviation={reference.Deviation:F2} effective_mass={reference.EffectiveMass:F2} half_life=86400_seconds");
                }
                if (output is not null) await File.WriteAllTextAsync(Path.Combine(output, $"temporal-{replay.Domain}-replay.json"), JsonSerializer.Serialize(replay, Json));
            }
            finally { directory.Delete(recursive: true); }
        }
        if (output is not null) await File.WriteAllTextAsync(Path.Combine(output, "temporal-output.txt"), string.Join('\n', lines) + "\n");
    }
}
