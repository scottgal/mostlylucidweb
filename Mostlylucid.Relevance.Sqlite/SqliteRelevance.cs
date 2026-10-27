using Microsoft.Extensions.DependencyInjection;

namespace Mostlylucid.Relevance.Sqlite;

/// <summary>Small entry points for standalone or hosted in-process SQLite integration.</summary>
public static class SqliteRelevance
{
    /// <summary>Loads YAML or JSON and opens an engine that owns its SQLite store and background worker.</summary>
    /// <param name="configurationPath">Local YAML/JSON path; relative paths resolve from the process working directory.</param>
    /// <param name="databasePath">SQLite path, defaulting to the YAML path with a .db extension; parent directory must exist.</param>
    /// <param name="converters">Caller-owned additional converters, uniquely named alongside built-ins.</param>
    /// <param name="vectorExtensionPath">Optional local sqlite-vec binary. Explicit loading failures propagate; no download or fallback.</param>
    /// <param name="cancellationToken">Cancellation while reading configuration.</param>
    /// <returns>An engine to dispose with await using.</returns>
    /// <remarks>With no native extension, exact managed distance functions are registered in SQLite.</remarks>
    public static async ValueTask<RelevanceEngine> OpenAsync(string configurationPath, string? databasePath = null,
        IEnumerable<IPointConverter>? converters = null, string? vectorExtensionPath = null, CancellationToken cancellationToken = default)
    {
        var definition = ReadDefinition(configurationPath, await File.ReadAllTextAsync(configurationPath, cancellationToken));
        definition.Validate();
        var store = new SqliteRelevanceStore(databasePath ?? Path.ChangeExtension(configurationPath, ".db"), vectorExtensionPath);
        try { return new RelevanceEngine(definition, store, converters); }
        catch { await store.DisposeAsync(); throw; }
    }

    /// <summary>Registers one singleton engine and its shared hosted worker in an existing application host.</summary>
    /// <param name="services">Service collection; call once for this single-engine registration.</param>
    /// <param name="configurationPath">Local YAML/JSON path loaded when the engine is first resolved.</param>
    /// <param name="databasePath">SQLite path, defaulting to the YAML path with a .db extension.</param>
    /// <param name="vectorExtensionPath">Optional explicit native sqlite-vec path; null uses managed exact distances.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Register custom IPointConverter instances and an optional IDestinationPredictor before resolving the engine.
    /// Start the host before submitting work; let the host stop the worker and dispose the engine.
    /// These are singleton registrations for one engine, not a named multi-engine factory.
    /// </remarks>
    public static IServiceCollection AddRelevanceSqlite(this IServiceCollection services, string configurationPath, string? databasePath = null, string? vectorExtensionPath = null)
    {
        services.AddSingleton<RelevanceWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RelevanceWorker>());
        services.AddSingleton(provider =>
        {
            var definition = ReadDefinition(configurationPath, File.ReadAllText(configurationPath));
            var store = new SqliteRelevanceStore(databasePath ?? Path.ChangeExtension(configurationPath, ".db"), vectorExtensionPath);
            try { return new RelevanceEngine(definition, store, provider.GetServices<IPointConverter>(), worker: provider.GetRequiredService<RelevanceWorker>(), predictor: provider.GetService<IDestinationPredictor>()); }
            catch { store.DisposeAsync().GetAwaiter().GetResult(); throw; }
        });
        return services;
    }
    /// <summary>Registers one AOT-safe engine from a typed definition and its hosted worker.</summary>
    /// <param name="services">Host services; register once for the single-engine integration.</param>
    /// <param name="definition">Configuration snapshotted immediately during registration.</param>
    /// <param name="databasePath">Owned SQLite connection path; parent directory must exist.</param>
    /// <param name="vectorOptions">Optional SQLite-owned HNSW configuration.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>Start the host before submitting work. Host shutdown drains the worker before disposing the engine.
    /// Register converters and an optional predictor as explicit instances. Use RelevanceHub for named domains.</remarks>
    public static IServiceCollection AddRelevanceSqlite(this IServiceCollection services, RelevanceDefinition definition,
        string databasePath, SqliteVectorOptions? vectorOptions = null)
    {
        var frozen = RelevanceJson.Deserialize<RelevanceDefinition>(RelevanceJson.Serialize(definition))!;
        frozen.Validate();
        services.AddSingleton<RelevanceWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RelevanceWorker>());
        services.AddSingleton(provider =>
        {
            var store = new SqliteRelevanceStore(databasePath, vectorOptions: vectorOptions);
            try { return new RelevanceEngine(frozen, store, provider.GetServices<IPointConverter>(), worker: provider.GetRequiredService<RelevanceWorker>(), predictor: provider.GetService<IDestinationPredictor>()); }
            catch { store.DisposeAsync().GetAwaiter().GetResult(); throw; }
        });
        return services;
    }

    private static RelevanceDefinition ReadDefinition(string path, string text) =>
        Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? RelevanceDefinition.FromJson(text) : RelevanceDefinition.FromYaml(text);

}
