using System.Text.Json;

namespace Mostlylucid.Relevance.Learning;

/// <summary>Versioned context/descriptor spaces, experiment windows and bounded online learning settings.</summary>
public sealed class LearningDefinition
{
    /// <summary>Persistent experiment identity; incompatible changes require a new identity.</summary>
    public string Id { get; set; } = "";
    /// <summary>Ordered context axes. Supply already scaled finite values in [0,1].</summary>
    public string[] ContextAxes { get; set; } = [];
    /// <summary>Ordered intervention axes, also scaled to [0,1]; includes meaning/producer versions.</summary>
    public string[] InterventionAxes { get; set; } = [];
    /// <summary>Version of the context extraction, descriptor producers and objective/reward mapping.</summary>
    public string Version { get; set; } = "v1";
    /// <summary>Candidate stream on the caller-owned relevance engine.</summary>
    public string CandidateStream { get; set; } = "emails";
    /// <summary>Observation stream for positive, actually observed responses, never predictions.</summary>
    public string ResponseStream { get; set; } = "responses";
    /// <summary>Configured positive event type on ResponseStream.</summary>
    public string ResponseType { get; set; } = "helpful";
    /// <summary>Recommendation surface whose candidates are the interventions themselves.</summary>
    public string Placement { get; set; } = "campaign";
    /// <summary>Largest RMS context distance admitted to an existing online group.</summary>
    public double GroupRadius { get; set; } = .18;
    /// <summary>Maximum discovered groups; distant contexts abstain once capacity is reached.</summary>
    public int MaxGroups { get; set; } = 16;
    /// <summary>Largest number of registered exact content versions.</summary>
    public int MaxVariants { get; set; } = 32;
    /// <summary>Minimum completed outcomes in a group before adaptive exploitation is permitted.</summary>
    public int MinOutcomes { get; set; } = 30;
    /// <summary>Probability of uniform exploration after warm-up; control retains positive probability.</summary>
    public double Exploration { get; set; } = .2;
    /// <summary>Positive L2 penalty for the small descriptor-conditioned linear reward model.</summary>
    public double RidgePenalty { get; set; } = .1;
    /// <summary>Maximum RMS descriptor distance from an observed variant for model-based scoring.</summary>
    public double DescriptorRadius { get; set; } = .4;
    /// <summary>Assignment-anchored outcome window. No observation after expiry becomes a fabricated failure.</summary>
    public double HorizonSeconds { get; set; } = 3600;
    /// <summary>Retention for decisions, events and active aggregate state.</summary>
    public double RetentionHours { get; set; } = 168;
    /// <summary>Maximum retained decisions and deduplication tokens in this scope.</summary>
    public int MaxRecords { get; set; } = 100000;
}

/// <summary>An exact, immutable content/configuration version and the vector actually extracted from it.</summary>
/// <param name="Id">Unique version ID; changing content under the same ID is rejected.</param>
/// <param name="Features">Intervention coordinates in definition order.</param>
/// <param name="Candidate">Full candidate envelope accepted by the existing relevance stream, including its id and vectors.</param>
/// <param name="Control">True for the one registered baseline.</param>
public sealed record InterventionVariant(string Id, double[] Features, JsonElement Candidate, bool Control = false);

/// <summary>A retry-stable assignment request built only from observations available before the intervention.</summary>
/// <param name="Id">Stable decision ID; retries return the original assignment.</param>
/// <param name="Unit">Session/account/incident assignment unit, scoped to this decision episode.</param>
/// <param name="Coordinates">Observed context coordinates in definition order.</param>
/// <param name="ObservedAt">Timestamp of the context evidence.</param>
/// <param name="Eligible">Optional application-constrained subset; must retain the control.</param>
/// <param name="Randomised">Use uniform random assignment for the independent evaluation phase.</param>
/// <param name="ExplicitGroup">Optional application-defined audience; normally omitted for detected grouping.</param>
public sealed record InterventionRequest(string Id, string Unit, double[] Coordinates, DateTimeOffset ObservedAt,
    string[]? Eligible = null, bool Randomised = false, string? ExplicitGroup = null);

/// <summary>One policy score, retaining observed support and relevance rank separately from estimated reward.</summary>
public sealed record InterventionScore(string Id, double Score, long Outcomes, double? MeanReward, int? RelevanceRank,
    string Source);

/// <summary>A durable assignment, exact treatment, pre-treatment context and the actual logged propensity.</summary>
public sealed record InterventionDecision(string Id, string Unit, string Group, long GroupSupport, double[] Context,
    InterventionVariant Variant, DateTimeOffset AssignedAt, DateTimeOffset Deadline, double Probability,
    string Policy, IReadOnlyList<InterventionScore> Alternatives);

/// <summary>Observed evidence, never an inferred read or a predicted outcome.</summary>
/// <param name="Id">Stable event ID; conflicting reuse is rejected atomically.</param>
/// <param name="Decision">Retained decision identity.</param>
/// <param name="Stage">attempted, delivered, applied, viewed, clicked, failed, outcome or censored.</param>
/// <param name="At">When the evidence occurred, not a fabricated timestamp at processing.</param>
/// <param name="Source">Evidence producer, such as first-party preview, simulator or delivery provider.</param>
/// <param name="Reward">Observed objective mapped to [0,1], required only for outcome.</param>
/// <param name="Outcome">Concrete endpoint or censoring reason.</param>
/// <param name="After">Optional post-intervention context in the same coordinate system.</param>
/// <param name="Measures">Optional raw objective components, retained alongside the scalar reward.</param>
/// <param name="Link">Exact link occurrence ID, not merely its destination.</param>
public sealed record InterventionEvent(string Id, string Decision, string Stage, DateTimeOffset At, string Source,
    double? Reward = null, string? Outcome = null, double[]? After = null,
    Dictionary<string, double>? Measures = null, string? Link = null);

/// <summary>Observed assignment-level evidence for one exact variant within one detected group.</summary>
public sealed record InterventionEvidence(string Group, string Variant, long Assigned, long Completed, long Censored,
    long Applied, long Viewed, long Clicked, long Failed, double? MeanReward, double? StandardError);
/// <summary>A discovered context pattern; its name is presentation, not an audience rule.</summary>
public sealed record DetectedGroup(string Id, long Support, double[] Centroid, double[] RewardCoefficients);
/// <summary>Durable assignment plus observed stages; missing view evidence does not imply unread content.</summary>
public sealed record InterventionJourney(InterventionDecision Decision, IReadOnlyList<InterventionEvent> Events, bool Closed);
/// <summary>Detached learning state for inspection, evaluation and application UI.</summary>
public sealed record LearningSnapshot(IReadOnlyList<DetectedGroup> Groups, IReadOnlyList<InterventionEvidence> Evidence,
    IReadOnlyList<InterventionJourney> Recent, IReadOnlyList<InterventionVariant> Variants);
