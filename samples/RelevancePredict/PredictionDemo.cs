using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.PredictDemo;

/// <summary>Actual engine output from a seeded, fully labelled synthetic fixture; no external model is required.</summary>
public static class PredictionDemo
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Loads the example's versioned definition from its assembly so the console subcommand works from any directory.</summary>
    public static RelevanceDefinition Definition()
    {
        using var stream = typeof(PredictionDemo).Assembly.GetManifestResourceStream("RelevancePredict.predict.yaml")!;
        using var reader = new StreamReader(stream);
        return RelevanceDefinition.FromYaml(reader.ReadToEnd());
    }

    /// <summary>Maps a domain event into the small engine envelope, deriving state exclusively from observed history.</summary>
    public static ValueTask<SaveReceipt> SaveAsync(RelevanceEngine engine, FixtureEvent observation, JourneyState encoder,
        CancellationToken cancellationToken = default) => engine.SaveAsync("events", new
        {
            id = observation.Id, subject = observation.Subject, candidate = observation.Candidate, type = observation.Type,
            at = observation.At, state = encoder.Observe(observation.Type)
        }, cancellationToken);

    /// <summary>Encodes the synthetic catalogue once; product descriptions remain metadata unless a converter is explicitly added.</summary>
    public static async Task SaveProductsAsync(RelevanceEngine engine, IEnumerable<FixtureProduct> products)
    {
        foreach (var p in products)
            await engine.SaveAsync("products", new {id = p.Id, name = p.Name, category = p.Category, price = p.Price,
                available = p.Available, swatch = p.Swatch, description = p.Description});
    }

    /// <summary>Runs a fresh SQLite population, cold-start checks, later users, restart and a frozen held-out evaluation.</summary>
    public static async Task RunAsync(string[] args)
    {
        var options = Options.Parse(args);
        var fixture = FixtureGenerator.Generate(options.Seed, options.Products, options.Training, options.Holdout);
        if (options.Copy is not null) fixture = fixture with { Products = CatalogueCopy.Apply(fixture.Products, await File.ReadAllTextAsync(options.Copy)) };
        if (options.Output is not null)
        {
            if (Directory.Exists(options.Output) && Directory.EnumerateFileSystemEntries(options.Output).Any())
                throw new ArgumentException("Choose a new or empty output directory; fixture files are never silently overwritten.");
            Directory.CreateDirectory(options.Output);
        }
        if (options.Database is not null && File.Exists(options.Database)) throw new ArgumentException("Choose a fresh database for a reproducible run.");
        var temporary = options.Database is null ? Directory.CreateTempSubdirectory("relevance-predict-") : null;
        var database = options.Database ?? Path.Combine(temporary!.FullName, "training.db");
        var definition = Definition();
        var capture = new CaptureTransitions();
        var lines = new List<string>();
        var checkpoints = new List<object>();
        var forecastExamples = new List<DestinationPrediction>();
        var recommendations = new List<object>();
        void Log(string line) { lines.Add(line); Console.WriteLine(line); }
        string Summary(DestinationPrediction p) => string.Join(" ", p.Outcomes.Select(o => $"{o.Destination}={o.Probability.ToString("P1", CultureInfo.InvariantCulture)}"))
            + $" unresolved={p.RemainingProbability.ToString("P1", CultureInfo.InvariantCulture)} support={p.SupportingTransitions:0} status={p.Status}";
        try
        {
            Log($"SYNTHETIC FIXTURE {fixture.Version}; seed={fixture.Seed}; Bogus=35.6.5");
            Log($"products={fixture.Products.Count}; training_visits={fixture.Training.Count}; later_holdout_visits={fixture.Holdout.Count}");
            Log($"missing_descriptions={fixture.Products.Count(p => p.Description is null)}; unavailable={fixture.Products.Count(p => !p.Available)}; neural_inference=none");
            Log("Fresh SQLite model. No preloaded histories. Probabilities are uncalibrated simulator estimates.");
            TransitionModel trained;
            DestinationPrediction beforeRestart;
            await using (var engine = new RelevanceEngine(definition, new SqliteRelevanceStore(database), clock: new FixtureClock(), predictor: capture))
            {
                await SaveProductsAsync(engine, fixture.Products);
                var noState = await engine.PredictAsync("new-shopper");
                Log("UNKNOWN " + Summary(noState));
                await SaveAsync(engine, new("new-shopper-00", "new-shopper", fixture.Products[0].Id, "view", FixtureGenerator.Epoch.AddHours(11)), new());
                var cold = await engine.PredictAsync("new-shopper");
                Log("COLD " + Summary(cold));
                checkpoints.Add(new { journeys = 0, prediction = cold });
                var done = 0;
                foreach (var limit in new[] { 3, Math.Min(100, options.Training), options.Training }.Distinct().Order())
                {
                    foreach (var journey in fixture.Training.Skip(done).Take(limit - done))
                    {
                        var encoder = new JourneyState();
                        foreach (var observation in journey.Events) await SaveAsync(engine, observation, encoder);
                    }
                    done = limit;
                    var current = await engine.PredictAsync("new-shopper");
                    checkpoints.Add(new { journeys = done, prediction = current });
                    Log($"AFTER {done} VISITS " + Summary(current));
                }
                beforeRestart = await engine.PredictAsync("new-shopper");
                trained = CaptureTransitions.Copy(capture.Snapshot);
            }
            // This is a reopen, not a replay: learned counts and the probe's state come back from SQLite.
            await using (var reopened = new RelevanceEngine(definition, new SqliteRelevanceStore(database), clock: new FixtureClock()))
            {
                var after = await reopened.PredictAsync("new-shopper");
                if (!after.Outcomes.SequenceEqual(beforeRestart.Outcomes) || after.RemainingProbability != beforeRestart.RemainingProbability)
                    throw new InvalidOperationException("Restart changed the learned distribution.");
                Log("SQLITE REOPEN probabilities_unchanged=true");
            }
            await using var evaluator = new RelevanceEngine(definition, new MemoryRelevanceStore(), clock: new FixtureClock(), predictor: new FrozenTransitions(trained));
            await SaveProductsAsync(evaluator, fixture.Products);
            var product = fixture.Products.First(p => p.Available && p.Category == "outdoors");
            var budget = Math.Max(120, product.Price);
            foreach (var (id, types) in new[]
            {
                ("considered", new[] {"view", "compare", "cart"}),
                ("impulsive", new[] {"view", "cart"}),
                ("returning", new[] {"view", "compare", "cart", "remove", "compare", "cart"})
            })
            {
                var encoder = new JourneyState();
                for (var index = 0; index < types.Length; index++)
                    await SaveAsync(evaluator, new($"probe-{id}-{index}", $"probe-{id}", product.Id, types[index],
                        FixtureGenerator.Epoch.AddHours(11).AddMinutes(index)), encoder);
                var forecast = await evaluator.PredictAsync($"probe-{id}");
                forecastExamples.Add(forecast);
                Log($"FRESH {id} state={forecast.CurrentState} " + Summary(forecast));
                var picks = await evaluator.RecommendAsync($"probe-{id}", "home",
                    Filter.All(Filter.Eq("category", "outdoors"), Filter.Lte("price", (double)budget)), 3);
                recommendations.Add(picks);
                Log($"PICKS {id} budget=£{budget:0} " + string.Join(", ", picks.Items.Select(p => $"{p.Id}:{p.Score.ToString("F5", CultureInfo.InvariantCulture)}")));
            }
            var evaluation = await OutcomeEvaluation.RunAsync(evaluator, fixture, definition.Destinations!.HorizonEvents);
            var metrics = evaluation.Summary;
            Log($"HOLDOUT landmark=first_cart horizon={metrics.HorizonEvents}_events scored={metrics.Scored} censored={metrics.Censored} without_cart={metrics.WithoutCart}");
            Log($"BRIER lower_is_better model={metrics.ModelBrier.ToString("F4", CultureInfo.InvariantCulture)} training_prior={metrics.PriorBrier.ToString("F4", CultureInfo.InvariantCulture)}");
            foreach (var bin in metrics.PurchaseBins.Where(b => b.Count > 0))
                Log($"PURCHASE BIN {bin.Range} n={bin.Count} predicted={bin.MeanPredicted.ToString("P1", CultureInfo.InvariantCulture)} observed={bin.ObservedFrequency.ToString("P1", CultureInfo.InvariantCulture)}");
            Log("No held-out outcome trained the frozen evaluator. These numbers test the simulator, not a real shop or an intervention.");
            // A new observed cancellation updates the persistent live model, not the frozen benchmark.
            // Forecast the next shopper from the same checkout state, rather than predicting a known result.
            object adaptation;
            await using (var live = new RelevanceEngine(definition, new SqliteRelevanceStore(database), clock: new FixtureClock()))
            {
                await SaveAsync(live, new("adapt-start", "adapt-session", product.Id, "checkout", FixtureGenerator.Epoch.AddHours(11)), new());
                var before = await live.PredictAsync("adapt-session");
                var completed = new FixtureEvent("adapt-end", "adapt-session", product.Id, "cancel", FixtureGenerator.Epoch.AddHours(11).AddMinutes(1));
                await SaveAsync(live, completed, new());
                var retry = await SaveAsync(live, completed, new());
                var observed = await live.PredictAsync("adapt-session");
                await SaveAsync(live, new("adapt-next", "next-checkout", product.Id, "checkout", FixtureGenerator.Epoch.AddHours(11).AddMinutes(2)), new());
                var after = await live.PredictAsync("next-checkout");
                adaptation = new { before, observed, after, retryDuplicate = retry.Duplicate };
                Log($"OBSERVED status={observed.Status} cancel={observed.Outcomes.Single(o => o.Destination == "cancel").Probability.ToString("P1", CultureInfo.InvariantCulture)}");
                Log($"OUTCOME UPDATE checkout_purchase={before.Outcomes.Single(o => o.Destination == "purchase").Probability.ToString("P1", CultureInfo.InvariantCulture)} -> {after.Outcomes.Single(o => o.Destination == "purchase").Probability.ToString("P1", CultureInfo.InvariantCulture)} support={before.SupportingTransitions:0}->{after.SupportingTransitions:0} retry_duplicate={retry.Duplicate.ToString().ToLowerInvariant()}");
            }
            if (options.Output is not null)
            {
                var report = new {fixture.Version, fixture.Seed, origin = "synthetic", isCalibrated = false, checkpoints,
                    forecasts = forecastExamples, recommendations, evaluation = metrics, transitions = trained, adaptation};
                await File.WriteAllTextAsync(Path.Combine(options.Output, "report.json"), JsonSerializer.Serialize(report, Json));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "catalogue.json"), JsonSerializer.Serialize(fixture.Products, Json));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "users.json"), JsonSerializer.Serialize(fixture.Training.Concat(fixture.Holdout).Select(j => j.User), Json));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "journeys.json"), JsonSerializer.Serialize(fixture.Training.Concat(fixture.Holdout), Json));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "holdout.json"), JsonSerializer.Serialize(evaluation.Rows, Json));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "output.txt"), string.Join('\n', lines) + "\n");
                var digest = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(options.Output, "journeys.json"))));
                await File.WriteAllTextAsync(Path.Combine(options.Output, "manifest.json"), JsonSerializer.Serialize(new
                {
                    fixture.Version, fixture.Seed, bogus = "35.6.5", journeysSha256 = digest,
                    trainingVisits = fixture.Training.Count, holdoutVisits = fixture.Holdout.Count,
                    copyOverlay = options.Copy is null ? "none" : Path.GetFileName(options.Copy),
                    clock = new FixtureClock().GetUtcNow(), populationCountsFrozenForEvaluation = true, stateEncoding = "prefix-v2"
                }, Json));
            }
        }
        finally { if (temporary is not null) temporary.Delete(recursive: true); }
    }

    private sealed record Options(int Seed, int Products, int Training, int Holdout, string? Output, string? Database, string? Copy)
    {
        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var allowed = new[] {"--seed", "--products", "--train", "--test", "--output", "--database", "--copy"};
            for (var index = 0; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !allowed.Contains(args[index]) || !values.TryAdd(args[index], args[index + 1]))
                    throw new ArgumentException("Use unique --seed, --products, --train, --test, --output, --database or --copy options with values.");
            }
            int Number(string name, int fallback) => values.TryGetValue(name, out var value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
            return new(Number("--seed", 814), Number("--products", 240), Number("--train", 1000), Number("--test", 400),
                values.GetValueOrDefault("--output"), values.GetValueOrDefault("--database"), values.GetValueOrDefault("--copy"));
        }
    }
}
