using System.Text.Json;
using Mostlylucid.Relevance.ExperimentDemo;
using Mostlylucid.Relevance.Learning;

namespace Mostlylucid.Relevance.Demo;

/// <summary>Measured pre-assignment visit features, extracted from the shop's actual event and filter requests.</summary>
public sealed record EmailVisit(string Session, int Views, int Comparisons, int PriceCuts, DateTimeOffset At)
{
    /// <summary>Stable named axes: comparison share and price narrowing, both bounded to [0,1].</summary>
    public double[] Coordinates => [Math.Min(1, Comparisons / (double)Math.Max(1, Views)), Math.Min(1, PriceCuts / 5.0)];
}
/// <summary>HTTP assignment request; no caller-supplied segment or future outcome label.</summary>
public sealed record EmailAssignmentRequest(string Id, string Session, bool Randomised = false);
/// <summary>A reviewed human/model edit. Content is parsed again before it enters the candidate catalogue.</summary>
public sealed record EmailEditRequest(string Subject, string Html, decimal OriginalPrice, decimal OfferPrice, int DelayMinutes,
    bool Reviewed = false, string Author = "human");
/// <summary>First-party demo interaction. Purchase reward is computed on the server, never accepted from the browser.</summary>
public sealed record EmailStageRequest(string Id, string Decision, string Stage, string? Link = null);
/// <summary>Complete selection explanation and the exact document/analysis to inspect.</summary>
public sealed record EmailChoice(InterventionDecision Decision, CharacterisedEmail Email);
/// <summary>A seeded policy comparison; counts and reward are observed outputs, not hard-coded display percentages.</summary>
public sealed record EmailPolicyResult(string Policy, int Assigned, int Scored, int Censored, int NoObservedView,
    double PurchaseRate, double MeanReward);

/// <summary>Hosts the email experiment in the real demo, with bounded recipient features, persistent learning and optional local producers.</summary>
public sealed class EmailLearningDemo(IConfiguration configuration, IWebHostEnvironment environment) : IHostedService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object profileLock = new();
    private readonly Dictionary<string, EmailVisit> visits = [];
    private readonly Dictionary<string, double> budgets = [];
    private EmailExperiment experiment = null!;
    private int progress, total;
    private string directory = "";
    private IReadOnlyList<EmailPolicyResult> results = [];

    /// <summary>Creates only local stores and catalogue records; never sends mail or downloads a model.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var root = configuration["Demo:DataPath"] ?? Path.Combine(environment.ContentRootPath, "App_Data");
        directory = Path.Combine(root, "email-experiments");
        experiment = await EmailExperiment.OpenAsync(directory);
    }
    /// <summary>Drains both workers and closes persistent stores on host shutdown.</summary>
    public async Task StopAsync(CancellationToken cancellationToken) => await experiment.DisposeAsync();

    /// <summary>Records successful shop events before intervention selection; names are added after grouping, not used as detector inputs.</summary>
    public void Observe(string session, string type)
    {
        lock (profileLock)
        {
            if (!visits.ContainsKey(session) && visits.Count >= 10000) throw new InvalidOperationException("Demo visit capacity reached.");
            var visit = visits.GetValueOrDefault(session) ?? new(session, 0, 0, 0, DateTimeOffset.UtcNow);
            visits[session] = visit with { Views = visit.Views + (type == "view" ? 1 : 0),
                Comparisons = visit.Comparisons + (type == "compare" ? 1 : 0), At = DateTimeOffset.UtcNow };
        }
    }
    /// <summary>Counts an actual downward filter change once, rather than treating every recommendation read as a new observation.</summary>
    public void ObserveBudget(string session, double? budget)
    {
        if (budget is null) return;
        lock (profileLock)
        {
            if (budgets.TryGetValue(session, out var previous) && budget < previous && visits.TryGetValue(session, out var visit))
                visits[session] = visit with { PriceCuts = visit.PriceCuts + 1, At = DateTimeOffset.UtcNow };
            if (visits.ContainsKey(session)) budgets[session] = budget.Value;
        }
    }
    /// <summary>Returns only actually observed counts for this session.</summary>
    public EmailVisit Visit(string session)
    {
        lock (profileLock) return visits.GetValueOrDefault(session) ?? new(session, 0, 0, 0, DateTimeOffset.UtcNow);
    }
    /// <summary>Obtains a detected-context assignment and preserves its exact HTML/source/settings.</summary>
    public async Task<EmailChoice> ChooseAsync(EmailAssignmentRequest request, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var visit = Visit(request.Session);
            if (visit.Views + visit.Comparisons < 3) throw new ArgumentException("Browse/compare first, or replay a sample visit.");
            var decision = await experiment.Learning.ChooseAsync(new(request.Id, request.Session, visit.Coordinates, visit.At, Randomised: request.Randomised), ct);
            return new(decision, AnalyseDecision(decision));
        }
        finally { gate.Release(); }
    }
    /// <summary>Parses and characterises an edit without registering or delivering it.</summary>
    public CharacterisedEmail Analyse(EmailEditRequest request) => EmailVariants.Characterise(Draft(request));
    /// <summary>Registers a human-reviewed version as a new recommendation candidate; it does not overwrite its parent.</summary>
    public async Task<CharacterisedEmail> RegisterAsync(EmailEditRequest request, CancellationToken ct)
    {
        if (!request.Reviewed) throw new ArgumentException("Review the actual content, destinations and offer before registration.");
        var email = Analyse(request);
        await gate.WaitAsync(ct);
        try { await experiment.Learning.RegisterAsync(email.Variant, ct); return email; }
        finally { gate.Release(); }
    }
    /// <summary>Optional small-model rewrite proposal. A later explicit review/register action is still required.</summary>
    public async Task<EmailDraft> RewriteAsync(EmailEditRequest request, CancellationToken ct)
    {
        using var client = EmailModels.Client();
        return await EmailModels.RewriteAsync(client, configuration["Demo:RewriteModel"] ?? "qwen3.5:0.8b", Draft(request), ct);
    }
    /// <summary>Inspects real optional Noul readings. Default deterministic experiments retain their own producer/version.</summary>
    public async Task<EmailSemantics> ModelReadingsAsync(EmailEditRequest request, CancellationToken ct)
    {
        using var client = EmailModels.Client();
        var structure = EmailCharacterisation.Analyse(request.Subject, request.Html);
        return await EmailCharacterisation.DecideAsync(client, configuration["Demo:DecisionModel"] ?? "nimble", "interactive-inspection-rubric-v1", structure, ct);
    }
    /// <summary>Records a real demo UI interaction. No endpoint for inventing a read from email HTML exists.</summary>
    public async Task<SaveReceipt> RecordAsync(EmailStageRequest request, CancellationToken ct)
    {
        if (request.Stage is not ("delivered" or "viewed" or "clicked" or "purchase" or "closed" or "censored")) throw new ArgumentException("Unknown demo stage.");
        await gate.WaitAsync(ct);
        try
        {
            var row = await experiment.Learning.GetAsync(request.Decision, ct)
                ?? throw new ArgumentException("Choose a recent email before recording a response.");
            var email = AnalyseDecision(row.Decision);
            if (request.Stage == "clicked" && email.Structure.Links.All(l => l.Id != request.Link)) throw new ArgumentException("Unknown link occurrence.");
            var outcome = request.Stage is "purchase" or "closed";
            var reward = request.Stage == "purchase" ? (double)((email.Draft.OfferPrice - email.Draft.OriginalPrice * .55m) / (email.Draft.OriginalPrice * .45m)) : 0;
            var measures = outcome ? new Dictionary<string, double> { ["paid_gbp"] = request.Stage == "purchase" ? (double)email.Draft.OfferPrice : 0,
                ["margin_gbp"] = request.Stage == "purchase" ? (double)(email.Draft.OfferPrice - email.Draft.OriginalPrice * .55m) : 0 } : null;
            return await experiment.Learning.SaveAsync(new(request.Id, request.Decision, outcome ? "outcome" : request.Stage,
                DateTimeOffset.UtcNow, "first-party-demo-preview", outcome ? reward : null,
                outcome ? request.Stage : request.Stage == "censored" ? "feedback-unavailable" : null,
                Measures: measures, Link: request.Link), ct);
        }
        finally { gate.Release(); }
    }
    /// <summary>Snapshot plus independently sampled policy-evaluation totals and raw visit extraction.</summary>
    public async Task<object> SnapshotAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try { return new { snapshot = await experiment.Learning.SnapshotAsync(ct), results, progress, total }; }
        finally { gate.Release(); }
    }
    /// <summary>Non-blocking progress while each simulated recipient still updates the ordinary streaming API.</summary>
    public object Progress => new { completed = Volatile.Read(ref progress), total = Volatile.Read(ref total) };

    /// <summary>Runs local synthetic recipients, never real email. Each assignment and observed response passes through the library.</summary>
    public async Task<IReadOnlyList<EmailPolicyResult>> RunAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (total > 0) throw new ArgumentException("Reset the lab before replaying the same experiment IDs.");
            total = 3600; progress = 0;
            var train = new Random(213);
            for (var i = 0; i < 2400; i++)
            {
                ct.ThrowIfCancellationRequested();
                await EmailExperimentRunner.VisitAsync(experiment, null, train, i, "training", true);
                Interlocked.Increment(ref progress);
            }
            var population = new Random(719); var policies = new Random(127);
            var observed = new List<ExperimentEpisode>();
            for (var i = 0; i < 1200; i++)
            {
                ct.ThrowIfCancellationRequested();
                observed.Add(await EmailExperimentRunner.VisitAsync(experiment, null, population, i, "evaluation", policies.NextDouble() < .5));
                Interlocked.Increment(ref progress);
            }
            results = observed.GroupBy(e => e.Randomised).Select(g =>
            {
                var scored = g.Where(e => e.Reward.HasValue).ToArray();
                return new EmailPolicyResult(g.Key ? "uniform" : "adaptive", g.Count(), scored.Length, g.Count()-scored.Length,
                    g.Count(e=>!e.ObservedView), scored.Average(e=>e.Purchased==true?1d:0), scored.Average(e=>e.Reward!.Value));
            }).ToArray();
            return results;
        }
        finally { gate.Release(); }
    }
    /// <summary>Clears only this local lab's two stores after draining them; the shop and diagnostic stores are preserved.</summary>
    public async Task ResetAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            await experiment.DisposeAsync();
            foreach (var file in new[] { "email-candidates.db", "email-learning.db" })
                foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(Path.Combine(directory, file + suffix));
            experiment = await EmailExperiment.OpenAsync(directory); progress = 0; total = 0; results = [];
        }
        finally { gate.Release(); }
    }
    private static EmailDraft Draft(EmailEditRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Subject) || string.IsNullOrWhiteSpace(r.Html)) throw new ArgumentException("Subject and HTML are required.");
        return new(r.Subject, r.Html, r.OriginalPrice, r.OfferPrice, r.DelayMinutes, r.Author);
    }
    private static CharacterisedEmail AnalyseDecision(InterventionDecision decision)
    {
        var data = decision.Variant.Candidate;
        return EmailVariants.Characterise(new(data.GetProperty("Subject").GetString()!, data.GetProperty("Html").GetString()!,
            data.GetProperty("OriginalPrice").GetDecimal(), data.GetProperty("OfferPrice").GetDecimal(), data.GetProperty("DelayMinutes").GetInt32(),
            data.GetProperty("Author").GetString()!, decision.Variant.Control));
    }
}
