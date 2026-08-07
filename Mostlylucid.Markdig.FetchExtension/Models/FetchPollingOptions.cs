namespace Mostlylucid.Markdig.FetchExtension.Models;

/// <summary>
///     Options to control the built-in in-memory polling service that tracks remote content
///     referenced by <fetch> tags and raises an event when content changes.
/// </summary>
public class FetchPollingOptions
{
    /// <summary>
    ///     Master switch to enable/disable the background polling loop.
    ///     Defaults to false to keep this optional for hosts.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     How often the internal scheduler wakes up to check which URLs are due.
    /// </summary>
    public TimeSpan SchedulerTickInterval { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Global HTTP timeout for individual fetch attempts.
    /// </summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Maximum number of concurrent fetches per tick.
    /// </summary>
    public int MaxConcurrentFetches { get; set; } = 4;
}