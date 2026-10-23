using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Demo;

public sealed class DemoEngines : IAsyncDisposable
{
    public RelevanceEngine Shop { get; }
    public RelevanceEngine Operations { get; }

    public DemoEngines(IHostEnvironment environment, IConfiguration configuration, RelevanceWorker worker)
    {
        var path = configuration["Demo:DataPath"] ?? Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(path);
        Shop = Create("shop", path, environment.ContentRootPath, worker);
        try { Operations = Create("operations", path, environment.ContentRootPath, worker); }
        catch { Shop.DisposeAsync().GetAwaiter().GetResult(); throw; }
    }

    private static RelevanceEngine Create(string name, string dataPath, string root, RelevanceWorker worker) => new(
        RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(root, "Configuration", name + ".yaml"))),
        new SqliteRelevanceStore(Path.Combine(dataPath, name + ".db")), worker: worker);

    public async ValueTask DisposeAsync() { await Operations.DisposeAsync(); await Shop.DisposeAsync(); }
}

public sealed class DemoSeeder(DemoCatalog catalog, DemoEngines engines) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var p in catalog.Products)
            await engines.Shop.SaveAsync("products", new { id = p.Id, name = p.Name, category = p.Category, price = p.Price, available = p.Available, swatch = p.Swatch }, cancellationToken);

        for (var i = 0; i < 60; i++)
        {
            var product = catalog.Products[i % catalog.Products.Count];
            foreach (var type in new[] { "view", "compare", "cart", i % 4 == 0 ? "cancel" : "purchase" })
                await engines.Shop.SaveAsync("events", new { id = $"seed-{i}-{type}", subject = $"seed-shop-{i}", candidate = product.Id, type }, cancellationToken);
        }
        var actions = new[]
        {
            new { id = "tighten-rate-limit", name = "Reduce incoming request rate", handler = "tighten-rate-limit", evidence = "Retry-heavy traffic and queue pressure", latency_ms = 450, error_fraction = 0.25, retries = 4.0, queue_depth = 220, available = true },
            new { id = "inspect-database", name = "Inspect database waits", handler = "inspect-database", evidence = "High latency with relatively little retry activity", latency_ms = 900, error_fraction = 0.08, retries = 0.3, queue_depth = 20, available = true },
            new { id = "inspect-queue", name = "Inspect queue consumers", handler = "inspect-queue", evidence = "A growing queue with moderate latency", latency_ms = 350, error_fraction = 0.08, retries = 1.0, queue_depth = 600, available = true }
        };
        foreach (var action in actions) await engines.Operations.SaveAsync("actions", action, cancellationToken);
        for (var i = 0; i < 24; i++)
            foreach (var state in new[] { "degraded", i % 4 == 0 ? "outage" : "recovered" })
                await engines.Operations.SaveAsync("logs", new { id = $"seed-ops-{i}-{state}", subject = $"seed-ops-{i}", type = "measurement", state,
                    latency_ms = 500, error_fraction = 0.2, retries = 2.0, queue_depth = 200 }, cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
