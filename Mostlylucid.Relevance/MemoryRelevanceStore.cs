using System.Text.Json;

namespace Mostlylucid.Relevance;

// Reference provider for development and provider-parity tests, not durable storage.
/// <summary>Non-durable reference provider using detached JSON records and copy-on-write atomic transactions.</summary>
public sealed class MemoryRelevanceStore(System.Text.Json.Serialization.JsonSerializerContext? recordTypes = null) : IRelevanceStore
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private Dictionary<(string Scope, string Kind, string Id), (string Json, DateTimeOffset Expires)> rows = [];
    private Dictionary<string, string> schemas = [];
    /// <inheritdoc />
    public async ValueTask<T> TransactAsync<T>(Func<IRelevanceTransaction, T> action, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var working = new Dictionary<(string, string, string), (string, DateTimeOffset)>(rows);
            var bindings = new Dictionary<string, string>(schemas);
            var result = action(new Transaction(working, bindings, recordTypes));
            cancellationToken.ThrowIfCancellationRequested();
            rows = working;
            schemas = bindings;
            return result;
        }
        finally { gate.Release(); }
    }
    /// <summary>Disposes synchronization resources after all callers have finished using the provider.</summary>
    public ValueTask DisposeAsync() { gate.Dispose(); return ValueTask.CompletedTask; }

    private sealed class Transaction(Dictionary<(string Scope, string Kind, string Id), (string Json, DateTimeOffset Expires)> rows,
        Dictionary<string, string> schemas, System.Text.Json.Serialization.JsonSerializerContext? recordTypes) : IRelevanceTransaction
    {
        public void BindSchema(string scope, string fingerprint)
        {
            if (schemas.TryGetValue(scope, out var current) && current != fingerprint) throw new InvalidOperationException("Configuration/converter version changed; use a new engine ID or migrate state.");
            schemas[scope] = fingerprint;
        }
        public bool Accept(string scope, string id, string hash, DateTimeOffset expiresAt)
        {
            var current = Get<string>(scope, "dedup", id);
            if (current is not null && current != hash) throw new ArgumentException("Event ID reused with different content.");
            if (current is not null) return false;
            Put(scope, "dedup", id, hash, expiresAt);
            return true;
        }
        public T? Get<T>(string scope, string kind, string id) => rows.TryGetValue((scope, kind, id), out var row) ? RelevanceJson.Deserialize<T>(row.Json, recordTypes) : default;
        public void Put<T>(string scope, string kind, string id, T value, DateTimeOffset expiresAt) => rows[(scope, kind, id)] = (RelevanceJson.Serialize(value, recordTypes), expiresAt);
        public int Count(string scope, string kind) => rows.Keys.Count(k => k.Scope == scope && k.Kind == kind);
        public void Upsert(string scope, Candidate candidate) => Put(scope, "candidate", candidate.Id, candidate, DateTimeOffset.MaxValue);
        public IReadOnlyList<Candidate> Eligible(string scope, Filter? filter, int limit) => Candidates(scope, filter).Take(limit).ToArray();
        public IReadOnlyList<Neighbor> Nearest(string scope, VectorQuery query, Filter? filter, int limit) => Candidates(scope, filter)
            .Where(c => c.Points.TryGetValue(query.Point, out var p) && p.Signature == query.Signature)
            .Select(c => new Neighbor(c, VectorMath.Distance(c.Points[query.Point].Values, query.Values, query.Metric)))
            .OrderBy(n => n.Distance).ThenBy(n => n.Candidate.Id, StringComparer.Ordinal).Take(limit).ToArray();
        private IEnumerable<Candidate> Candidates(string scope, Filter? filter) => rows.Where(r => r.Key.Scope == scope && r.Key.Kind == "candidate")
            .Select(r => RelevanceJson.Deserialize<Candidate>(r.Value.Json)!).Where(c => DataValues.Matches(c.Data, filter)).OrderBy(c => c.Id, StringComparer.Ordinal);
        public void Prune(DateTimeOffset now)
        {
            foreach (var key in rows.Where(r => r.Value.Expires <= now).Select(r => r.Key).ToArray()) rows.Remove(key);
        }
    }
}
