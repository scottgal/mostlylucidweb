namespace Mostlylucid.Markdig.FetchExtension.Storage;

/// <summary>
/// Optional interface for storage providers that support cache inspection.
/// Useful for debugging, monitoring, and demo purposes.
/// </summary>
public interface ICacheInspector
{
    /// <summary>
    /// Gets all cached entries.
    /// </summary>
    IEnumerable<CachedMarkdownEntry> GetAllCachedEntries();

    /// <summary>
    /// Gets a specific cached entry by URL and blog post ID.
    /// </summary>
    CachedMarkdownEntry? GetCachedEntry(string url, int blogPostId = 0);
}

/// <summary>
/// Represents a cached markdown entry with metadata.
/// </summary>
public class CachedMarkdownEntry
{
    public required string Url { get; init; }
    public int BlogPostId { get; init; }
    public required string Content { get; init; }
    public required string ContentHash { get; init; }
    public DateTime LastFetchedAt { get; init; }
    public int PollFrequencyHours { get; init; }
    public TimeSpan Age => DateTime.UtcNow - LastFetchedAt;
    public bool IsStale => Age.TotalHours >= PollFrequencyHours;
    public int ContentLength => Content?.Length ?? 0;
    public string ContentPreview => Content?.Length > 200
        ? string.Concat(Content.AsSpan(0, 200), "...")
        : Content ?? string.Empty;
}
