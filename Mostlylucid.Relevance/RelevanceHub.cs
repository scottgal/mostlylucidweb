using System.Security.Cryptography;
using System.Text;

namespace Mostlylucid.Relevance;

/// <summary>A tenant-local recommendation domain, such as products, emails or operational actions.</summary>
/// <remarks>Each space has independent candidates, subjects, representations, outcomes and schema bindings.</remarks>
public sealed record RelevanceSpace(string Tenant, string Name)
{
    /// <summary>Unambiguous, stable storage identity; IDs containing separators cannot collide.</summary>
    public string Key => RelevanceJson.Serialize(new[] { Tenant, Name });
}

/// <summary>Opens an owned provider connection for a named shard; PostgreSQL providers can implement this boundary.</summary>
public interface IRelevanceStoreFactory
{
    /// <summary>Stable shard names. Changing the topology requires explicit data migration.</summary>
    IReadOnlyList<string> Shards { get; }
    /// <summary>Creates an exclusive store lifetime; the engine disposes it.</summary>
    IRelevanceStore Open(string shard);
}

/// <summary>Small multi-domain entry point with deterministic whole-space sharding.</summary>
/// <remarks>
/// Each tenant/space routes to one shard, keeping state, candidates and attribution atomic.
/// This is partitioning across domains, not distributed top-K within one oversized catalog.
/// Dispose the hub after callers finish; it owns every engine it opens.
/// </remarks>
public sealed class RelevanceHub : IAsyncDisposable
{
    private readonly IRelevanceStoreFactory stores;
    private readonly string[] shards;
    private readonly Dictionary<string, (string Definition, RelevanceEngine Engine)> engines = [];
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly int maxSpaces;
    private bool disposed;

    /// <summary>Creates a bounded registry of engines over a caller-owned provider factory.</summary>
    public RelevanceHub(IRelevanceStoreFactory stores, int maxSpaces = 128)
    {
        ArgumentNullException.ThrowIfNull(stores);
        if (maxSpaces < 1) throw new ArgumentOutOfRangeException(nameof(maxSpaces));
        shards = stores.Shards.Order(StringComparer.Ordinal).ToArray();
        if (shards.Length == 0 || shards.Any(string.IsNullOrWhiteSpace) || shards.Distinct(StringComparer.Ordinal).Count() != shards.Length)
            throw new ArgumentException("Provide unique, nonempty shard names.");
        this.stores = stores;
        this.maxSpaces = maxSpaces;
    }

    /// <summary>Returns the stable rendezvous-hash shard for a space, independent of shard-list order.</summary>
    public string ShardFor(RelevanceSpace space)
    {
        if (string.IsNullOrWhiteSpace(space.Tenant) || space.Tenant.Length > 128 || string.IsNullOrWhiteSpace(space.Name) || space.Name.Length > 128)
            throw new ArgumentException("Tenant and space names require 1 through 128 characters.");
        return shards.MaxBy(shard => Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(RelevanceJson.Serialize(new[] { space.Key, shard })))), StringComparer.Ordinal)!;
    }

    /// <summary>Opens or reuses a space, freezing its configuration and rejecting incompatible repeated registration.</summary>
    /// <remarks>The application must authorise the tenant before opening a space. The returned engine is borrowed: dispose the hub, not individual engines.
    /// Converters are fixed by the first open; repeat calls must use the same converter semantics.</remarks>
    public async ValueTask<RelevanceEngine> OpenAsync(RelevanceSpace space, RelevanceDefinition definition,
        IEnumerable<IPointConverter>? converters = null, CancellationToken cancellationToken = default)
    {
        var frozen = RelevanceJson.Deserialize<RelevanceDefinition>(RelevanceJson.Serialize(definition))!;
        frozen.Id = "space:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(space.Key)));
        var supplied = (converters ?? []).ToArray();
        var signature = RelevanceJson.Serialize(frozen) + RelevanceJson.Serialize(supplied
            .OrderBy(c => c.Name, StringComparer.Ordinal).Select(c => c.Name + ":" + c.Version).ToArray());
        var shard = ShardFor(space);
        await gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (engines.TryGetValue(space.Key, out var existing))
                return existing.Definition == signature ? existing.Engine : throw new ArgumentException("Space already opened with different semantics.");
            if (engines.Count >= maxSpaces) throw new InvalidOperationException("Open-space capacity reached.");
            frozen.Validate();
            var store = stores.Open(shard);
            try
            {
                var engine = new RelevanceEngine(frozen, store, supplied);
                engines.Add(space.Key, (signature, engine));
                return engine;
            }
            catch { await store.DisposeAsync(); throw; }
        }
        finally { gate.Release(); }
    }

    /// <summary>Closes admission and drains each engine before releasing its provider.</summary>
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed) return;
            disposed = true;
            List<Exception> errors = [];
            foreach (var entry in engines.Values)
                try { await entry.Engine.DisposeAsync(); } catch (Exception error) { errors.Add(error); }
            engines.Clear();
            if (errors.Count != 0) throw new AggregateException(errors);
        }
        finally { gate.Release(); }
    }
}
