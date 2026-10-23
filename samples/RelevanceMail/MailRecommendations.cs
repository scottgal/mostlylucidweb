using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;
using StyloMail.Core;

namespace RelevanceMail;

/// <summary>A host-defined review task, associated with semantic conditions and an existing policy action.</summary>
/// <param name="Id">Stable playbook ID; this is never interpreted as a MailAction.</param>
/// <param name="Name">Human-readable task name, not instructions for a model.</param>
/// <param name="After">The trusted policy action after which this candidate is eligible.</param>
/// <param name="When">Prototype values keyed by the question IDs configured for this engine.</param>
public sealed record ReviewPlaybook(string Id, string Name, MailAction After, IReadOnlyDictionary<string, double> When);

/// <summary>An explicit advisory result; abstention leaves the original StyloMail policy action intact.</summary>
/// <param name="Status">recommended, no_eligible_playbooks, insufficient_evidence or incompatible_questions.</param>
/// <param name="Schema">Fingerprint of tenant, ordered questions, preprocessing and producer identity.</param>
/// <param name="Issues">Missing/incompatible dimension IDs; never message content.</param>
/// <param name="Recommendations">Ranked eligible playbooks, or null on abstention.</param>
public sealed record MailRecommendation(string Status, string Schema, IReadOnlyList<string> Issues, RecommendationResult? Recommendations);

/// <summary>Optional StyloMail composition component: semantic evidence in, eligible review playbooks out.</summary>
/// <remarks>
/// Uses the real StyloMail.Core contracts. It does not classify mail, choose MailAction, send a message,
/// or update a trusted sender baseline. Each assessment gets its own subject, avoiding cross-message blending.
/// Required dimensions must be complete: Relevance's dense-vector baseline does not implement StyloMail's masks.
/// The host owns classification, versioned policy, actual exposure and any later intervention evaluation.
/// </remarks>
public sealed class MailRecommendations : IAsyncDisposable
{
    private readonly RelevanceEngine engine;
    private readonly SemanticDimension[] questions;
    private readonly string sourceVersion;
    private readonly string questionSchemaVersion;

    /// <summary>Identity of this tenant's representation, including the precise ordered question rubrics.</summary>
    public string Schema { get; }

    private MailRecommendations(RelevanceEngine engine, SemanticDimension[] questions, string sourceVersion,
        string questionSchemaVersion, string schema)
    {
        this.engine = engine;
        this.questions = questions;
        this.sourceVersion = sourceVersion;
        this.questionSchemaVersion = questionSchemaVersion;
        Schema = schema;
    }

    /// <summary>Creates a tenant-scoped advisory engine over a caller-chosen ordered Noul question set.</summary>
    /// <param name="databasePath">SQLite file owned by this component; parent directories must exist.</param>
    /// <param name="tenantId">Trusted tenant identity; included in storage scope, never supplied by mail content.</param>
    /// <param name="questions">Ordered semantic questions; the complete rubric participates in the fingerprint.</param>
    /// <param name="questionSchemaVersion">Version asserted by the classifier's question configuration/cache contract.</param>
    /// <param name="sourceVersion">Exact Evidence.SourceVersion expected from the configured producer.</param>
    /// <param name="preprocessingVersion">Identity of extraction/context preparation, not just the model weights.</param>
    /// <returns>An owned component to dispose after callers finish; it starts its own bounded worker.</returns>
    /// <remarks>No model calls or implicit model downloads occur. Creating another schema gives it a separate scope.</remarks>
    public static MailRecommendations Open(string databasePath, string tenantId,
        IReadOnlyList<SemanticDimension> questions, string questionSchemaVersion, string sourceVersion, string preprocessingVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(questionSchemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(preprocessingVersion);
        ArgumentNullException.ThrowIfNull(questions);
        var ordered = questions.ToArray(); // SemanticDimension is an immutable record; preserve caller order.
        if (ordered.Length == 0 || ordered.Any(q => q is null || string.IsNullOrWhiteSpace(q.Id)
            || string.IsNullOrWhiteSpace(q.Instructions) || string.IsNullOrWhiteSpace(q.CriteriaTrue)
            || string.IsNullOrWhiteSpace(q.CriteriaFalse)) || ordered.Select(q => q.Id).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            throw new ArgumentException("Supply distinct, complete Noul question definitions.", nameof(questions));
        // Changing a rubric or order changes coordinates even when the output dimension stays the same.
        var schema = Hash(new { AdapterVersion = 1, tenantId, questions = ordered, questionSchemaVersion, sourceVersion, preprocessingVersion });
        var definition = new RelevanceDefinition
        {
            Id = "mail-review-" + schema,
            Filters = ["after"],
            Streams = new()
            {
                ["playbooks"] = new() { Kind = "candidate" },
                ["assessments"] = new() { Kind = "observation", PointSource = "input", Weights = new() { ["assessed"] = 1 } }
            },
            Points = new() { ["semantic"] = new() { Path = "semantic", Converter = "vector", Dimensions = ordered.Length,
                Metric = "l2", Displays = "Ordered, versioned semantic question answers; magnitude is preserved." } },
            Placements = new() { ["review"] = new() { Heads = new() { ["semantic"] = 1 } } }
        };
        var store = new SqliteRelevanceStore(databasePath);
        try { return new(new RelevanceEngine(definition, store), ordered, sourceVersion, questionSchemaVersion, schema); }
        catch { store.DisposeAsync().GetAwaiter().GetResult(); throw; }
    }

    /// <summary>Upserts a host-owned prototype; all configured questions need finite values in [0,1].</summary>
    /// <param name="playbook">Allowed review task and the condition in which it is relevant.</param>
    /// <param name="cancellationToken">Cancellation before the candidate update commits.</param>
    /// <returns>A receipt after conversion and durable update.</returns>
    public ValueTask<SaveReceipt> SaveAsync(ReviewPlaybook playbook, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playbook);
        ArgumentNullException.ThrowIfNull(playbook.When);
        ArgumentException.ThrowIfNullOrWhiteSpace(playbook.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(playbook.Name);
        if (!Enum.IsDefined(playbook.After)) throw new ArgumentException("Unknown policy action.");
        if (playbook.When.Count != questions.Length || questions.Any(q => !playbook.When.ContainsKey(q.Id)))
            throw new ArgumentException("A playbook must supply exactly the configured dimensions.");
        var values = questions.Select(q => playbook.When[q.Id]).ToArray();
        if (values.Any(v => !double.IsFinite(v) || v is < 0 or > 1)) throw new ArgumentException("Invalid prototype values.");
        return engine.SaveAsync("playbooks", new { id = playbook.Id, name = playbook.Name, after = playbook.After.ToString(), semantic = values }, cancellationToken);
    }

    /// <summary>Projects an assessment and ranks only playbooks eligible after an independently authorised policy action.</summary>
    /// <param name="assessmentId">Stable ID for one immutable assessment; revised evidence needs a new ID.</param>
    /// <param name="policyAction">Action already selected by StyloMail policy, never inferred by this component.</param>
    /// <param name="assessment">Existing classifier result; original evidence remains in StyloMail's ledger.</param>
    /// <param name="actualQuestionSchemaVersion">Classifier's actual schema version, checked before projection.</param>
    /// <param name="cancellationToken">Cancellation for ingestion and retrieval; these are two separate awaited operations.</param>
    /// <returns>Recommendations or explicit abstention; never a replacement delivery-policy decision.</returns>
    /// <remarks>
    /// Selected semantic rows must be unique, fully available, finite [0,1] values from the expected producer.
    /// Reduced/absent answers and reason-tagged partial reads abstain; no zero-filling and no cold-start ranking.
    /// The host must pass versions from the actual classification configuration, not fabricate them from this instance.
    /// </remarks>
    public async ValueTask<MailRecommendation> RecommendAsync(string assessmentId, MailAction policyAction,
        SemanticAssessment assessment, string actualQuestionSchemaVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(assessmentId);
        ArgumentNullException.ThrowIfNull(assessment);
        if (!Enum.IsDefined(policyAction)) throw new ArgumentException("Unknown policy action.");
        if (actualQuestionSchemaVersion != questionSchemaVersion)
            return new("incompatible_questions", Schema, ["question_schema_version"], null);
        if (assessment.Cache.Stale) return new("insufficient_evidence", Schema, ["stale_assessment"], null);
        var values = new float[questions.Length];
        var issues = new List<string>();
        for (var i = 0; i < questions.Length; i++)
        {
            // Use names, never dictionary enumeration order or default(double).
            var rows = assessment.Evidence.Where(e => e.SignalId == questions[i].Id && e.Origin == EvidenceOrigin.Semantic).ToArray();
            if (rows.Length != 1 || rows[0].Availability != EvidenceAvailability.Available
                || rows[0].Value is not { } value || !double.IsFinite(value) || value is < 0 or > 1
                || rows[0].SourceVersion != sourceVersion || rows[0].Attributes?.Any(a => a.Name == "reason") == true)
            {
                issues.Add(questions[i].Id);
                continue;
            }
            values[i] = (float)value;
        }
        if (issues.Count > 0) return new("insufficient_evidence", Schema, issues, null);
        // Do not use sender ID here: an untrusted message must not pull a sender's historical mean towards itself.
        var subject = Hash(assessmentId);
        await engine.SaveAsync("assessments", new { id = assessmentId, subject, type = "assessed", semantic = values }, cancellationToken);
        var result = await engine.RecommendAsync(subject, "review", Filter.Eq("after", policyAction.ToString()), count: 3, cancellationToken: cancellationToken);
        return new(result.Items.Count == 0 ? "no_eligible_playbooks" : "recommended", Schema, [], result);
    }

    /// <summary>Drains owned work and disposes the SQLite engine; does not dispose the mail classifier.</summary>
    public ValueTask DisposeAsync() => engine.DisposeAsync();

    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
