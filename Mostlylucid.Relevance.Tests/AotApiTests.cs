using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mostlylucid.Relevance.Learning;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class AotApiTests
{
    public const string Yaml = """
        id: aot-yaml
        points:
          traits: {path: vector, converter: vector, dimensions: 2, metric: l2}
        streams:
          items: {kind: candidate}
          events: {kind: observation, weights: {observe: 1, helpful: 1}}
          outcomes: {kind: feedback}
        placements:
          next: {heads: {traits: 1}}
        """;

    [Fact]
    public void YamlUsesGeneratedMetadataAndRejectsAmbiguousOrUnboundedInput()
    {
        var d = RelevanceDefinition.FromYaml(Yaml);
        d.Validate();
        Assert.Equal(2, d.Points["traits"].Dimensions);
        Assert.ThrowsAny<Exception>(() => RelevanceDefinition.FromYaml(Yaml + "\nunknown_setting: 1"));
        Assert.ThrowsAny<Exception>(() => RelevanceDefinition.FromYaml("id: a\nid: b"));
        Assert.Throws<ArgumentException>(() => RelevanceDefinition.FromYaml("id: &identity a\npoints: *identity"));
        Assert.Throws<ArgumentException>(() => RelevanceDefinition.FromYaml("a: " + new string('[', 40) + "x" + new string(']', 40)));
        Assert.Throws<ArgumentException>(() => RelevanceDefinition.FromYaml("id: " + new string('a', 1_048_576)));
        Assert.Equal("001", RelevanceDefinition.FromYaml(Yaml.Replace("aot-yaml", "\"001\"")).Id);
    }

    [Fact]
    public async Task BuiltInEnvelopesAndPublicEvidenceHaveShippedMetadata()
    {
        var d = RelevanceDefinition.FromYaml(Yaml);
        d.Filters.Add("metadata.available");
        await using var engine = new RelevanceEngine(d, new MemoryRelevanceStore());
        var metadata = RelevanceJson.Element(new Dictionary<string, bool> { ["available"] = true });
        await engine.SaveAsync("items", new RelevanceItem("a", [.1f, .2f], metadata));
        await engine.SaveAsync("events", new RelevanceObservation("e", "subject", "a"));
        var filter = Filter.All(Filter.Eq("metadata.available", true));
        var restored = RelevanceJson.Deserialize<Filter>(RelevanceJson.Serialize(filter));
        var decision = await engine.RecommendAsync("subject", "next", restored, 1);
        await engine.SaveAsync("outcomes", new RelevanceFeedback("f", decision.Id, "a", 1));
        Assert.Equal("a", Assert.Single(RelevanceJson.Deserialize<RecommendationResult>(RelevanceJson.Serialize(decision))!.Items).Id);
        var prediction = new DestinationPrediction("s", "view", 8, [new("purchase", .5)], .5, 10, "estimated");
        Assert.Equal(.5, RelevanceJson.Deserialize<DestinationPrediction>(RelevanceJson.Serialize(prediction))!.RemainingProbability);
        var snapshot = new LearningSnapshot([], [], [], []);
        Assert.Empty(RelevanceJson.Deserialize<LearningSnapshot>(RelevanceJson.Serialize(snapshot, LearningJson.Metadata), LearningJson.Metadata)!.Groups);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostedRegistrationWorksWithTypedAndYamlDefinitions(bool yaml)
    {
        var directory = Directory.CreateTempSubdirectory("relevance-aot-host-");
        try
        {
            var builder = Host.CreateApplicationBuilder();
            if (yaml)
            {
                var path = Path.Combine(directory.FullName, "relevance.yaml");
                await File.WriteAllTextAsync(path, Yaml);
                builder.Services.AddRelevanceSqlite(path);
            }
            else builder.Services.AddRelevanceSqlite(RelevanceDefinition.ForVectors(2), Path.Combine(directory.FullName, "typed.db"));
            using var host = builder.Build();
            await host.StartAsync();
            var engine = host.Services.GetRequiredService<RelevanceEngine>();
            await engine.SaveAsync("items", new RelevanceItem("a", [.1f, .2f]));
            Assert.Equal("a", Assert.Single((await engine.RecommendAsync("s", "next", count: 1)).Items).Id);
            await host.StopAsync();
        }
        finally { directory.Delete(true); }
    }

    [Fact]
    public async Task CustomProviderRecordsUseExplicitMetadataWithoutReflection()
    {
        await using var store = new MemoryRelevanceStore(AotTestJson.Default);
        var result = await store.TransactAsync(tx =>
        {
            tx.Put("scope", "custom", "a", new CustomState(42), DateTimeOffset.MaxValue);
            return tx.Get<CustomState>("scope", "custom", "a");
        });
        Assert.Equal(42, result!.Count);
    }
}

internal sealed record CustomState(int Count);
[JsonSerializable(typeof(CustomState))]
internal partial class AotTestJson : JsonSerializerContext;
