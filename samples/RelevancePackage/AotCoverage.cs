using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Sqlite;

internal static class AotCoverage
{
    private const string Yaml = """
        id: aot-file-config
        points:
          traits: {path: vector, converter: vector, dimensions: 2}
        streams:
          items: {kind: candidate}
          events: {kind: observation, weights: {observe: 1}}
        placements:
          next: {heads: {traits: 1}}
        """;

    public static async Task RunAsync(string root)
    {
        var yamlPath = Path.Combine(root, "aot.yaml");
        var jsonPath = Path.Combine(root, "aot.json");
        await File.WriteAllTextAsync(yamlPath, Yaml);
        await File.WriteAllTextAsync(jsonPath, RelevanceJson.Serialize(RelevanceDefinition.FromYaml(Yaml)));
        foreach (var path in new[] { yamlPath, jsonPath })
        {
            await using var file = await SqliteRelevance.OpenAsync(path, path + ".db");
            await file.SaveAsync("items", new RelevanceItem("item", [.1f, .2f]));
            if ((await file.RecommendAsync("new", "next", count: 1)).Items.Single().Id != "item") throw new InvalidOperationException("File loading failed.");
        }
        for (var i = 0; i < 2; i++)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            if (i == 0) builder.Services.AddRelevanceSqlite(yamlPath, Path.Combine(root, "host-yaml.db"));
            else builder.Services.AddRelevanceSqlite(RelevanceDefinition.ForVectors(2), Path.Combine(root, "host-typed.db"));
            using var host = builder.Build();
            await host.StartAsync();
            var engine = host.Services.GetRequiredService<RelevanceEngine>();
            await engine.SaveAsync("items", new RelevanceItem("a", [.1f, .2f]));
            await engine.SaveAsync("events", new RelevanceObservation("one", "s", "a"));
            var decision = await engine.RecommendAsync("s", "next", count: 1);
            if (i == 1) await engine.SaveAsync("outcomes", new RelevanceFeedback("feedback", decision.Id, "a", 1));
            await host.StopAsync();
        }
        IPointConverter[] builtins = [new NumericConverter(), new CategoricalConverter(), new BooleanConverter(), new PrecomputedConverter(), new FormulaConverter(), new OklabConverter()];
        JsonElement[] inputs = [RelevanceJson.Element(10), RelevanceJson.Element("a"), RelevanceJson.Element(true), JsonDocument.Parse("[1,2]").RootElement.Clone(), RelevanceJson.Element(10), RelevanceJson.Element("#ff0000")];
        PointDefinition[] settings = [new() { Scale = 10 }, new() { Categories = ["a", "b"] }, new(), new() { Dimensions = 2 }, new() { Expression = "clamp(x / 10, 0, 1)" }, new() { Metric = "l2" }];
        for (var i = 0; i < builtins.Length; i++)
        {
            var dimensions = builtins[i].Dimensions(settings[i]);
            VectorMath.Validate(await builtins[i].ConvertAsync(inputs[i], settings[i]), dimensions);
        }
        var custom = RelevanceDefinition.ForVectors(1);
        custom.Points["traits"].Path = "value";
        custom.Points["traits"].Converter = "custom-scale";
        await using (var engine = new RelevanceEngine(custom, new MemoryRelevanceStore(), [new ScaleConverter()]))
        {
            await engine.SaveAsync("items", JsonDocument.Parse("""{"id":"custom","value":10}""").RootElement.Clone());
            if ((await engine.RecommendAsync("s", "next", count: 1)).Items.Single().Id != "custom") throw new InvalidOperationException("Custom converter failed.");
        }
        await using (var store = new SqliteRelevanceStore(Path.Combine(root, "custom-records.db"), recordTypes: CoverageJson.Default))
        {
            var stored = await store.TransactAsync(tx => { tx.Put("s", "custom", "a", new Profile(42), DateTimeOffset.MaxValue); return tx.Get<Profile>("s", "custom", "a"); });
            if (stored?.Count != 42) throw new InvalidOperationException("Custom provider metadata failed.");
        }
        var motion = RelevanceDefinition.ForVectors(1);
        motion.Streams["events"].PointSource = "input";
        motion.Destinations = new()
        {
            States = ["healthy", "purchase"], Outcomes = ["purchase"], MinTransitions = 1,
            Trajectory = new() { Points = ["traits"], MinExamples = 1, HalfLifeSeconds = .000001, BaselineStates = ["healthy"], MinBaselineMass = 1 }
        };
        await using (var engine = new RelevanceEngine(motion, new MemoryRelevanceStore()))
        {
            var start = DateTimeOffset.UtcNow.AddSeconds(-10);
            for (var i = 0; i < 3; i++) await engine.SaveAsync("events", new RelevanceObservation("motion-" + i, "journey", At: start.AddSeconds(i), State: "healthy", Vector: [(float)(.2 + i * .1)], BaselineGroup: "service"));
            var before = await engine.PredictAsync("journey");
            if (before.Trajectory?.Velocity is null || before.Trajectory.Normal is null) throw new InvalidOperationException("Motion/normal evidence missing.");
            await engine.SaveAsync("events", new RelevanceObservation("end", "journey", At: start.AddSeconds(3), State: "purchase", BaselineGroup: "service"));
            var result = await engine.PredictAsync("journey");
            if (RelevanceJson.Deserialize<DestinationPrediction>(RelevanceJson.Serialize(result))!.Outcomes.Single().Probability != 1) throw new InvalidOperationException("Prediction serialization failed.");
        }
        var filter = Filter.All(Filter.Gte("price", 1));
        if (RelevanceJson.Deserialize<Filter>(RelevanceJson.Serialize(filter)) is not Conjunction) throw new InvalidOperationException("Filter metadata missing.");
        Console.WriteLine("aot paths: yaml/json, hosted, all converters, custom metadata, trajectories/normals, public results passed");
    }

    private sealed class ScaleConverter : IPointConverter
    {
        public string Name => "custom-scale";
        public string Version => "v1";
        public int Dimensions(PointDefinition point) => 1;
        public ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default) => ValueTask.FromResult(new[] { value.GetSingle() / 10 });
    }
}
internal sealed record Profile(int Count);
[JsonSerializable(typeof(Profile))]
internal partial class CoverageJson : JsonSerializerContext;
