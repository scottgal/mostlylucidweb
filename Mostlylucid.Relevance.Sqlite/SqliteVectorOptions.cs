namespace Mostlylucid.Relevance.Sqlite;

/// <summary>SQLite-owned HNSW configuration using the sqlite-vector-rs virtual-table extension.</summary>
/// <remarks>Explicit opt-in: use a locally built, pinned extension. No downloads or managed index are performed.</remarks>
public sealed record SqliteVectorOptions
{
    /// <summary>Path to sqlite-vector-rs's platform-specific library.</summary>
    public required string ExtensionPath { get; init; }
    /// <summary>Maximum graph connectivity, from 2 through 128.</summary>
    public int Connections { get; init; } = 16;
    /// <summary>Construction search width; at least Connections and at most 10000.</summary>
    public int ConstructionWidth { get; init; } = 200;
    /// <summary>Query search width, from 1 through 10000; tune against an exact baseline.</summary>
    public int SearchWidth { get; init; } = 100;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ExtensionPath) || Connections is < 2 or > 128
            || ConstructionWidth < Connections || ConstructionWidth > 10000 || SearchWidth is < 1 or > 10000)
            throw new ArgumentException("Invalid native SQLite HNSW configuration.");
    }
}
