using RelevanceMail;
using StyloMail.Core;

namespace RelevanceMail.Tests;

public sealed class MailRecommendationsTests
{
    private const string Producer = "synthetic-question-fixture/1";
    private static readonly string[] Ids = ["semantic.credential_request", "semantic.payment_redirection", "semantic.urgency_pressure"];
    private static SemanticDimension[] Questions => Ids.Select(id => SemanticDimensions.All.Single(q => q.Id == id)).ToArray();
    private static MailRecommendations Open(string? tenant = "tenant", SemanticDimension[]? questions = null, string? database = null) =>
        MailRecommendations.Open(database ?? Path.Combine(Path.GetTempPath(), $"relevance-mail-test-{Guid.NewGuid():N}.db"),
            tenant!, questions ?? Questions, SemanticDimensions.QuestionSchemaVersion, Producer, "synthetic-evidence/1");
    private static SemanticAssessment Assessment(params double[] values) => new()
    {
        Evidence = Ids.Zip(values).Select(p => new Evidence { SignalId = p.First, Origin = EvidenceOrigin.Semantic,
            Availability = EvidenceAvailability.Available, Value = p.Second, SourceVersion = Producer, ObservedAt = DateTimeOffset.UtcNow }).ToArray(),
        ResolvedModelVersion = Producer,
        Cache = new() { Hit = false, Stale = false, KeyDigest = "test" }
    };
    private static ReviewPlaybook Playbook(string id, MailAction after, params double[] values) =>
        new(id, id, after, Ids.Zip(values).ToDictionary(p => p.First, p => p.Second));

    [Fact]
    public async Task RealStyloMailEvidenceRanksOnlyPolicyEligiblePlaybooks()
    {
        await using var engine = Open();
        await engine.SaveAsync(Playbook("verify-supplier", MailAction.Review, .01, .99, .02));
        await engine.SaveAsync(Playbook("credential-review", MailAction.Review, .99, .01, .99));
        await engine.SaveAsync(Playbook("closer-but-forbidden", MailAction.Allow, .034, .988, .012));
        var assessment = Assessment(.034, .988, .012);
        var result = await engine.RecommendAsync("message", MailAction.Review, assessment, SemanticDimensions.QuestionSchemaVersion);
        Assert.Equal("recommended", result.Status);
        Assert.Equal("verify-supplier", result.Recommendations!.Items[0].Id);
        Assert.DoesNotContain(result.Recommendations.Items, r => r.Id == "closer-but-forbidden");
        Assert.Equal(Math.Sqrt(.024 * .024 + .002 * .002 + .008 * .008), result.Recommendations.Items[0].Reasons[0].Distance, 6);
        // Order of the returned evidence must not change coordinate meaning.
        var reordered = await engine.RecommendAsync("reordered", MailAction.Review,
            assessment with { Evidence = assessment.Evidence.Reverse().ToArray() }, SemanticDimensions.QuestionSchemaVersion);
        Assert.Equal(result.Recommendations.Items[0].Reasons, reordered.Recommendations!.Items[0].Reasons);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("reduced")]
    [InlineData("not-applicable")]
    [InlineData("unavailable")]
    [InlineData("wrong-producer")]
    [InlineData("nonfinite")]
    [InlineData("outside-range")]
    [InlineData("partial-read")]
    [InlineData("wrong-origin")]
    public async Task IncompleteOrIncompatibleEvidenceAbstainsWithoutColdStartRecommendations(string problem)
    {
        await using var engine = Open();
        await engine.SaveAsync(Playbook("must-not-fallback", MailAction.Review, 0, 0, 0));
        var assessment = Assessment(0, 0, 0);
        var rows = assessment.Evidence.ToArray();
        var first = rows[0];
        rows[0] = problem switch
        {
            "reduced" => first with { Availability = EvidenceAvailability.ReducedCoverage },
            "not-applicable" => first with { Availability = EvidenceAvailability.NotApplicable, Value = null },
            "unavailable" => first with { Availability = EvidenceAvailability.Unavailable, Value = null },
            "wrong-producer" => first with { SourceVersion = "other-model" },
            "nonfinite" => first with { Value = double.NaN },
            "outside-range" => first with { Value = 1.01 },
            "partial-read" => first with { Attributes = [new() { Name = "reason", Value = "client input shortened" }] },
            "wrong-origin" => first with { Origin = EvidenceOrigin.Deterministic },
            _ => first
        };
        assessment = assessment with { Evidence = problem switch { "missing" => rows.Skip(1).ToArray(), "duplicate" => rows.Append(first).ToArray(), _ => rows } };
        var result = await engine.RecommendAsync("message", MailAction.Review, assessment, SemanticDimensions.QuestionSchemaVersion);
        Assert.Equal("insufficient_evidence", result.Status);
        Assert.Null(result.Recommendations);
        Assert.Contains(Ids[0], result.Issues);
    }

    [Fact]
    public async Task ExplicitZeroIsAValidObservationAndEmptyEligibilityIsReported()
    {
        await using var engine = Open();
        await engine.SaveAsync(Playbook("routine", MailAction.Allow, 0, 0, 0));
        var result = await engine.RecommendAsync("zero", MailAction.Allow, Assessment(0, 0, 0), SemanticDimensions.QuestionSchemaVersion);
        Assert.Equal(0, Assert.Single(result.Recommendations!.Items).Reasons[0].Distance);
        var empty = await engine.RecommendAsync("review", MailAction.Review, Assessment(0, 0, 0), SemanticDimensions.QuestionSchemaVersion);
        Assert.Equal("no_eligible_playbooks", empty.Status);
        Assert.Empty(empty.Recommendations!.Items);
    }

    [Fact]
    public async Task TenantQuestionOrderAndRubricChangesIsolateTheStoredRepresentation()
    {
        var database = Path.Combine(Path.GetTempPath(), $"relevance-mail-isolation-{Guid.NewGuid():N}.db");
        await using var first = Open(database: database);
        await first.SaveAsync(Playbook("routine", MailAction.Allow, 0, 0, 0));
        await using var otherTenant = Open("other-tenant", database: database);
        await using var reordered = Open(questions: Questions.Reverse().ToArray(), database: database);
        var changed = Questions;
        changed[0] = changed[0] with { Instructions = "A different question with the same ID" };
        await using var changedRubric = Open(questions: changed, database: database);
        Assert.Equal(4, new[] { first.Schema, otherTenant.Schema, reordered.Schema, changedRubric.Schema }.Distinct().Count());
        Assert.Empty((await otherTenant.RecommendAsync("same-id", MailAction.Allow, Assessment(0, 0, 0), SemanticDimensions.QuestionSchemaVersion)).Recommendations!.Items);
    }

    [Fact]
    public async Task QuestionMismatchAndStaleCacheAbstainAndAnAssessmentIdCannotChangeItsEvidence()
    {
        await using var engine = Open();
        var assessment = Assessment(.1, .2, .3);
        Assert.Equal("incompatible_questions", (await engine.RecommendAsync("a", MailAction.Review, assessment, "other-schema")).Status);
        Assert.Equal("insufficient_evidence", (await engine.RecommendAsync("stale", MailAction.Review,
            assessment with { Cache = assessment.Cache with { Stale = true } }, SemanticDimensions.QuestionSchemaVersion)).Status);
        await engine.RecommendAsync("a", MailAction.Review, assessment, SemanticDimensions.QuestionSchemaVersion);
        await engine.RecommendAsync("a", MailAction.Review, assessment, SemanticDimensions.QuestionSchemaVersion);
        await Assert.ThrowsAsync<ArgumentException>(async () => await engine.RecommendAsync("a", MailAction.Review,
            Assessment(.3, .2, .1), SemanticDimensions.QuestionSchemaVersion));
    }
}
