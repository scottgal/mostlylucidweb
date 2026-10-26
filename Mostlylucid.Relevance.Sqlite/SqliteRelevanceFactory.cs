using System.Text.RegularExpressions;

namespace Mostlylucid.Relevance.Sqlite;

/// <summary>SQLite shard files plus a persisted topology guard; no server or separate index process.</summary>
public sealed class SqliteRelevanceFactory : IRelevanceStoreFactory
{
    private readonly string directory;
    private readonly SqliteVectorOptions? vectors;
    private readonly string[] shards;
    /// <inheritdoc />
    public IReadOnlyList<string> Shards => Array.AsReadOnly(shards);

    /// <summary>Creates shard storage; refuses silent rerouting when an existing directory's topology changes.</summary>
    /// <param name="directory">Root for manifest and SQLite files; created if missing.</param>
    /// <param name="shards">Stable file-safe names. Defaults to one shard called main.</param>
    /// <param name="vectors">Optional SQLite-native vector index; omitted means portable exact SQL.</param>
    public SqliteRelevanceFactory(string directory, IEnumerable<string>? shards = null, SqliteVectorOptions? vectors = null)
    {
        this.directory = Path.GetFullPath(directory);
        this.shards = (shards ?? ["main"]).Order(StringComparer.Ordinal).ToArray();
        if (this.shards.Length == 0 || this.shards.Distinct().Count() != this.shards.Length
            || this.shards.Any(s => !Regex.IsMatch(s, "\\A[A-Za-z0-9_-]{1,64}\\z")))
            throw new ArgumentException("Shard names must be unique, file-safe identifiers.");
        this.vectors = vectors;
        Directory.CreateDirectory(this.directory);
        var manifest = Path.Combine(this.directory, "relevance-shards.json");
        var expected = RelevanceJson.Serialize(new[] { "rendezvous-sha256-v1" }.Concat(this.shards).ToArray());
        // The exclusive lock also covers first creation, so another process cannot read a partial manifest.
        using var file = new FileStream(manifest, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (file.Length == 0)
        {
            if (Directory.EnumerateFiles(this.directory, "*.db").Any())
                throw new InvalidOperationException("Shard manifest missing beside existing databases; recover it or migrate explicitly.");
            var bytes = System.Text.Encoding.UTF8.GetBytes(expected);
            file.Write(bytes); file.Flush(flushToDisk: true);
        }
        else
        {
            using var reader = new StreamReader(file);
            if (reader.ReadToEnd() != expected) throw new InvalidOperationException("Shard topology changed; migrate into a new directory explicitly.");
        }
    }

    /// <inheritdoc />
    public IRelevanceStore Open(string shard) => shards.Contains(shard, StringComparer.Ordinal)
        ? new SqliteRelevanceStore(Path.Combine(directory, shard + ".db"), vectorOptions: vectors)
        : throw new ArgumentException("Unknown shard.");
}
