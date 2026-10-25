using System.Globalization;
using System.Text.Json;
using Mostlylucid.Relevance.Learning;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.ExperimentDemo;

/// <summary>Actual parsed email candidates, an existing recommender and the separate learning ledger.</summary>
public sealed class EmailExperiment : IAsyncDisposable
{
    /// <summary>Existing recommendation engine, configured to recommend emails to detected contexts.</summary>
    public RelevanceEngine Relevance { get; }
    /// <summary>Controlled assignment, durable observations and descriptor-conditioned reward learning.</summary>
    public InterventionLearning Learning { get; }
    /// <summary>Exact authored documents and their extracted evidence.</summary>
    public IReadOnlyList<CharacterisedEmail> Emails { get; }

    private EmailExperiment(RelevanceEngine relevance, InterventionLearning learning, IReadOnlyList<CharacterisedEmail> emails)
    { Relevance = relevance; Learning = learning; Emails = emails; }

    /// <summary>Opens persistent stores and registers the actual content versions; optional readings must cover every generated document.</summary>
    public static async Task<EmailExperiment> OpenAsync(string directory, TimeProvider? clock = null, int seed = 814,
        IReadOnlyList<CharacterisedEmail>? emails = null, string version = "literal-v1")
    {
        Directory.CreateDirectory(directory);
        var actual = emails ?? EmailVariants.Generate().Select(d => EmailVariants.Characterise(d)).ToArray();
        var relevance = new RelevanceEngine(EmailVariants.RelevanceDefinition(version),
            new SqliteRelevanceStore(Path.Combine(directory, "email-candidates.db")), clock: clock);
        InterventionLearning? learning = null;
        try
        {
            learning = new InterventionLearning(EmailVariants.LearningDefinition(version),
                new SqliteRelevanceStore(Path.Combine(directory, "email-learning.db")), relevance, clock, new Random(seed));
            foreach (var email in actual) await learning.RegisterAsync(email.Variant);
            return new(relevance, learning, actual);
        }
        catch { if (learning is not null) await learning.DisposeAsync(); await relevance.DisposeAsync(); throw; }
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync() { await Learning.DisposeAsync(); await Relevance.DisposeAsync(); }
}

/// <summary>A separate simulator clock; fixture timestamps are never guessed from animation speed.</summary>
public sealed class ExperimentClock : TimeProvider
{
    /// <summary>Current fixture time.</summary>
    public DateTimeOffset Now { get; set; } = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>One held-out assignment's actual response, including raw components and incomplete feedback.</summary>
public sealed record ExperimentEpisode(string Phase, string Group, string Variant, bool Randomised, double Probability,
    bool Delivered, bool ObservedView, bool Clicked, bool? Purchased, double? Reward, double[] Before, double[] After,
    decimal OfferPrice, int DelayMinutes);

/// <summary>Reproducible content/context experiment. Hidden response rules never enter detector or policy inputs.</summary>
public static class EmailExperimentRunner
{
    /// <summary>Feeds observed context, an assigned email and actual stage/endpoint events through the production APIs.</summary>
    public static async Task<ExperimentEpisode> VisitAsync(EmailExperiment experiment, ExperimentClock? clock, Random population,
        int index, string phase, bool randomised)
    {
        var profile = index % 2; // Simulator only. No segment name or hidden intent is sent to the engine.
        var views = 10;
        var comparisons = profile == 0 ? population.Next(7, 9) : population.Next(1, 3);
        var priceCuts = profile == 0 ? population.Next(3, 5) : population.Next(0, 2);
        var context = new[] { comparisons / (double)views, priceCuts / 5.0 };
        if (clock is not null) clock.Now = clock.Now.AddSeconds(1);
        var observedAt = clock?.Now ?? DateTimeOffset.UtcNow;
        var id = $"{phase}-{index:00000}";
        var decision = await experiment.Learning.ChooseAsync(new(id, id, context, observedAt,
            Eligible: experiment.Emails.Select(e => e.Variant.Id).ToArray(), Randomised: randomised));
        var email = experiment.Emails.Single(e => e.Variant.Id == decision.Variant.Id);
        var delivered = population.NextDouble() < .96;
        var humanView = delivered && population.NextDouble() < .72;
        var observedView = humanView && population.NextDouble() < .75; // Coverage loss, not a fabricated unread label.
        var clicked = humanView && population.NextDouble() < .65;
        var discount = (double)((email.Draft.OriginalPrice - email.Draft.OfferPrice) / email.Draft.OriginalPrice / .1m);
        var early = email.Structure.PrimaryCta!.TextPosition < .3;
        // Ground truth is a simulator-owned authored fact, never the decision model's own answer.
        var explained = email.Draft.Html.Contains("Delivery costs are shown", StringComparison.Ordinal) ? 1.0 : 0.0;
        var repeat = email.Structure.PrimaryDestinationOccurrences > 1;
        var shortDelay = email.Draft.DelayMinutes <= 15;
        var response = profile == 0
            ? .12 + .20 * discount + .16 * explained + (early ? 0 : .08) + (shortDelay ? 0 : .10) + (repeat ? .05 : 0)
            : .24 + .03 * discount - .06 * explained + (early ? .20 : 0) + (shortDelay ? .10 : 0) - (repeat ? .04 : 0);
        var purchased = population.NextDouble() < (humanView ? response : .06);
        var feedbackLost = population.NextDouble() < .05;
        var paid = clicked ? email.Draft.OfferPrice : email.Draft.OriginalPrice;
        var margin = purchased ? paid - email.Draft.OriginalPrice * .55m : 0;
        var reward = (double)(margin / (email.Draft.OriginalPrice * .45m));
        var after = context.Select(v => Math.Clamp(v + (purchased ? -.15 : .08), 0, 1)).ToArray();
        async Task Save(string stage, double? value = null, string? outcome = null, Dictionary<string, double>? measures = null)
        {
            if (clock is not null) clock.Now = clock.Now.AddSeconds(1);
            await experiment.Learning.SaveAsync(new(id + ":" + stage, id, stage, clock?.Now ?? DateTimeOffset.UtcNow, "seeded-email-environment-v1",
                value, outcome, stage is "outcome" or "censored" ? after : null, measures,
                stage == "clicked" ? email.Structure.PrimaryCta.Id : null));
        }
        await Save("attempted"); await Save(delivered ? "delivered" : "failed");
        if (observedView) await Save("viewed");
        if (clicked) await Save("clicked");
        if (feedbackLost) await Save("censored", outcome: "endpoint-telemetry-lost");
        else await Save("outcome", reward, purchased ? "purchase" : "closed-without-purchase", new()
        {
            ["margin_gbp"] = (double)margin, ["paid_gbp"] = purchased ? (double)paid : 0,
            ["contact_cost_gbp"] = .05, ["policy_assignment_probability"] = phase == "evaluation" ? .5 : 1
        });
        return new(phase, decision.Group, decision.Variant.Id, randomised, decision.Probability,
            delivered, observedView, clicked, feedbackLost ? null : purchased, feedbackLost ? null : reward,
            context, after, email.Draft.OfferPrice, email.Draft.DelayMinutes);
    }

    /// <summary>Runs training then independent randomized policy evaluation; writes measured JSON/stdout rather than illustrative percentages.</summary>
    public static async Task RunAsync(string[] args)
    {
        var output = args.Length is 2 or 4 && args[0] == "--output" ? Path.GetFullPath(args[1])
            : throw new ArgumentException("Use --output new-directory [--characterisations file].");
        IReadOnlyList<CharacterisedEmail>? input = null;
        var version = "literal-v1";
        if (args.Length == 4)
        {
            if (args[2] != "--characterisations") throw new ArgumentException("Unknown option.");
            var frozen = JsonSerializer.Deserialize<CharacterisedEmail[]>(await File.ReadAllTextAsync(args[3]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            if (frozen.Length != 48 || frozen.Select(e => e.Semantics.Producer + e.Semantics.Version).Distinct().Count() != 1)
                throw new ArgumentException("Supply all 48 same-producer frozen characterisations.");
            input = frozen.Select(e => EmailVariants.Characterise(e.Draft, e.Semantics)).ToArray();
            if (input.Select(e => e.Variant.Id).SequenceEqual(frozen.Select(e => e.Variant.Id)) == false)
                throw new ArgumentException("Characterisation content or descriptor was changed.");
            version = "decision-" + frozen[0].Variant.Id[5..];
        }
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new ArgumentException("Use a new or empty output directory.");
        Directory.CreateDirectory(output);
        var clock = new ExperimentClock();
        var lines = new List<string>();
        void Log(string text) { lines.Add(text); Console.WriteLine(text); }
        var episodes = new List<ExperimentEpisode>();
        await using (var experiment = await EmailExperiment.OpenAsync(Path.Combine(output, "database"), clock, emails: input, version: version))
        {
            Log($"EMAIL CANDIDATES {experiment.Emails.Count}; producer={experiment.Emails[0].Semantics.Producer}:{experiment.Emails[0].Semantics.Version}; hidden_audience_labels=none");
            var control = experiment.Emails.Single(e => e.Variant.Control);
            Log($"STRUCTURE control={control.Variant.Id} words={control.Structure.Words} links={control.Structure.Links.Count} primary={control.Structure.PrimaryCta!.Id} position={control.Structure.PrimaryCta.TextPosition:F3}");
            foreach (var link in control.Structure.Links) Log($"LINK {link.Id} region={link.Region} position={link.TextPosition:F3} button={link.ButtonLike} unsubscribe={link.Unsubscribe} text={link.Text}");
            var training = new Random(213);
            for (var i = 0; i < 2400; i++) episodes.Add(await VisitAsync(experiment, clock, training, i, "training", true));
            var evaluation = new Random(719);
            var policyAssignment = new Random(127);
            for (var i = 0; i < 1200; i++) episodes.Add(await VisitAsync(experiment, clock, evaluation, i, "evaluation", policyAssignment.NextDouble() < .5));
            var snapshot = await experiment.Learning.SnapshotAsync();
            foreach (var group in snapshot.Groups)
                Log($"DETECTED {group.Id} support={group.Support} centroid=[{string.Join(',', group.Centroid.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)))}] coefficients=[{string.Join(',', group.RewardCoefficients.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)))}]");
            foreach (var group in episodes.Where(e => e.Phase == "evaluation").GroupBy(e => e.Randomised))
            {
                var scored = group.Where(e => e.Reward.HasValue).ToArray();
                Log($"EVALUATION policy={(group.Key ? "uniform" : "adaptive")} assigned={group.Count()} scored={scored.Length} censored={group.Count()-scored.Length} purchase={scored.Average(e=>e.Purchased==true?1d:0):P1} reward={scored.Average(e=>e.Reward!.Value):F4} no_observed_view={group.Count(e=>!e.ObservedView)}");
            }
            var noView = episodes.Count(e => !e.ObservedView && e.Reward.HasValue);
            Log($"NO OBSERVED VIEW scored={noView}; retained_in_assignment_analysis=true");
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
            await File.WriteAllTextAsync(Path.Combine(output, "emails.json"), JsonSerializer.Serialize(experiment.Emails, json));
            await File.WriteAllTextAsync(Path.Combine(output, "episodes.json"), JsonSerializer.Serialize(episodes, json));
            await File.WriteAllTextAsync(Path.Combine(output, "snapshot.json"), JsonSerializer.Serialize(snapshot, json));
        }
        await using (var reopened = await EmailExperiment.OpenAsync(Path.Combine(output, "database"), clock, emails: input, version: version))
        {
            var snapshot = await reopened.Learning.SnapshotAsync();
            Log($"SQLITE REOPEN groups={snapshot.Groups.Count} outcomes={snapshot.Evidence.Sum(e=>e.Completed)} variants={snapshot.Variants.Count}");
        }
        await File.WriteAllLinesAsync(Path.Combine(output, "output.txt"), lines);
    }
}
