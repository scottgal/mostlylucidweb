using System.Text.Json;
using System.Text.Json.Serialization;
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Learning;
using Mostlylucid.Relevance.Sqlite;
using Mostlylucid.Sqlite.Vector;

var root = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "relevance-package-" + Guid.NewGuid().ToString("N"));
var vectors = args.Length > 1 ? new SqliteVectorOptions { ExtensionPath = args[1] == "bundled" ? SqliteVectorExtension.Path : args[1] } : null;
var factory = new SqliteRelevanceFactory(root, ["east", "west"], vectors);
var definition = new RelevanceDefinition
{
    Id = "reusable-definition",
    Filters = ["available"],
    Streams = new()
    {
        ["items"] = new() { Kind = "candidate" },
        ["events"] = new() { Kind = "observation", Weights = new() { ["helpful"] = 1 } }
    },
    Points = new() { ["traits"] = new() { Path = "vector", Converter = "vector", Dimensions = 2 } },
    Placements = new() { ["next"] = new() { Heads = new() { ["traits"] = 1 } } }
};
definition = RelevanceDefinition.FromJson(RelevanceJson.Serialize(definition));
// Different domains reuse the machinery, not each other's incompatible vectors or learned state.
var domains = new[] { new RelevanceSpace("shop", "products"), new RelevanceSpace("shop", "emails"), new RelevanceSpace("ops", "actions") };
await using (var hub = new RelevanceHub(factory))
{
    foreach (var space in domains)
    {
        var engine = await hub.OpenAsync(space, definition);
        await engine.SaveAsync("items", new Item("a", [0.1f, 0.2f], true), AppJson.Default.Item);
        await engine.SaveAsync("items", new Item("b", [0.2f, 0.3f], true), AppJson.Default.Item);
        await engine.SaveAsync("items", new Item("c", [0.9f, 0.8f], false), AppJson.Default.Item);
        await engine.SaveAsync("events", JsonSerializer.SerializeToElement(
            new Observation("view-1", "entity-1", "a", "helpful"), AppJson.Default.Observation));
        var result = await engine.RecommendAsync("entity-1", "next", Filter.Eq("available", true), 2);
        var related = await engine.RelatedAsync("a", "traits", count: 1);
        if (result.Items.Count != 2 || related.Single().Candidate.Id != "b") throw new InvalidOperationException("Unexpected ranking.");
        Console.WriteLine($"{space.Tenant}/{space.Name} shard={hub.ShardFor(space)} recommendations={string.Join(',', result.Items.Select(i => i.Id))} related={related[0].Candidate.Id}");
    }
    var emails = await hub.OpenAsync(domains[1], definition);
    // The optional intervention layer also runs without reflection or a neural training pipeline.
    await using var learner = new InterventionLearning(new LearningDefinition
    {
        Id = "package-smoke", ContextAxes = ["intent"], InterventionAxes = ["offer"],
        CandidateStream = "items", ResponseStream = "events", Placement = "next"
    }, new SqliteRelevanceStore(Path.Combine(root, "learning.db")), emails);
    foreach (var (id, offer, control) in new[] { ("control", 0.0, true), ("offer", 1.0, false) })
        await learner.RegisterAsync(new(id, [offer], JsonSerializer.SerializeToElement(new Item(id, [(float)offer, 0], true), AppJson.Default.Item), control));
    var decision = await learner.ChooseAsync(new("trial-1", "visit-1", [.5], DateTimeOffset.UtcNow));
    await learner.SaveAsync(new("outcome-1", decision.Id, "outcome", DateTimeOffset.UtcNow, "smoke", .8, "purchase"));
    if ((await learner.SnapshotAsync()).Evidence.Sum(e => e.Completed) != 1) throw new InvalidOperationException("Learning failed.");
    Console.WriteLine("learning completed=1");
}
await using (var reopened = new RelevanceHub(factory))
{
    var engine = await reopened.OpenAsync(domains[0], definition);
    var result = await engine.RecommendAsync("entity-1", "next", count: 2);
    if (result.Items[0].Id != "a") throw new InvalidOperationException("Reopen failed.");
    Console.WriteLine($"reopened={result.Items[0].Id} native-hnsw={vectors is not null}");
}

internal sealed record Item(string id, float[] vector, bool available);
internal sealed record Observation(string id, string subject, string candidate, string type);
[JsonSerializable(typeof(Item))]
[JsonSerializable(typeof(Observation))]
internal partial class AppJson : JsonSerializerContext;
