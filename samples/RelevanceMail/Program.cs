using RelevanceMail;
using StyloMail.Core;

// Reuse the real question definitions. Stable order is part of the representation, not incidental enumeration.
var ids = new[] { "semantic.credential_request", "semantic.payment_redirection", "semantic.urgency_pressure" };
var questions = ids.Select(id => SemanticDimensions.All.Single(q => q.Id == id)).ToArray();
const string producer = "synthetic-question-fixture/1";
var database = args.FirstOrDefault() ?? Path.Combine(Path.GetTempPath(), $"relevance-mail-{Guid.NewGuid():N}.db");
await using var recommendations = MailRecommendations.Open(database, "example-tenant", questions,
    SemanticDimensions.QuestionSchemaVersion, producer, "synthetic-evidence/1");

// These prototypes are supplied by the host. A model never invents or registers allowed actions.
foreach (var playbook in new[]
{
    Playbook("verify-supplier", "Verify the supplier change through an established contact", MailAction.Review, .01, .99, .02),
    Playbook("check-credentials", "Review the credential request and preserve evidence", MailAction.Review, .99, .01, .99),
    Playbook("urgent-payment", "Escalate the urgent payment change for independent verification", MailAction.Review, .01, .99, .99),
    Playbook("routine-processing", "Continue the existing routine-mail workflow", MailAction.Allow, .01, .01, .01)
}) await recommendations.SaveAsync(playbook);

// Deterministic fixtures: these numbers exercise the bridge; this program makes no Jev/Nimble request.
foreach (var (id, values) in new[]
{
    ("supplier-change", new[] { .034, .988, .012 }),
    ("credential-request", new[] { .999, .009, .999 }),
    ("urgent-payment", new[] { .034, .999, .993 })
})
{
    var result = await recommendations.RecommendAsync(id, MailAction.Review, Assessment(values), SemanticDimensions.QuestionSchemaVersion);
    Console.WriteLine($"{id}: {result.Recommendations!.Items[0].Id}; policy stays {MailAction.Review}");
}
var complete = Assessment([.01, .99, .02]);
var incomplete = complete with { Evidence = complete.Evidence.Take(2).ToArray() };
var abstention = await recommendations.RecommendAsync("incomplete", MailAction.Review, incomplete, SemanticDimensions.QuestionSchemaVersion);
Console.WriteLine($"missing urgency answer: {abstention.Status}; no recommendations={abstention.Recommendations is null}");

ReviewPlaybook Playbook(string id, string name, MailAction after, params double[] values) =>
    new(id, name, after, ids.Zip(values).ToDictionary(p => p.First, p => p.Second));

SemanticAssessment Assessment(double[] values) => new()
{
    Evidence = ids.Zip(values).Select(p => new Evidence { SignalId = p.First, Origin = EvidenceOrigin.Semantic,
        Availability = EvidenceAvailability.Available, Value = p.Second, SourceVersion = producer, ObservedAt = DateTimeOffset.UtcNow }).ToArray(),
    ResolvedModelVersion = producer,
    Cache = new() { Hit = false, Stale = false, KeyDigest = "synthetic-fixture" }
};
