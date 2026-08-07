using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mostlylucid.Markdig.FetchExtension.Services;

namespace Mostlylucid.Markdig.FetchExtension.Events;

/// <summary>
/// Default implementation of IMarkdownFetchEventPublisher.
/// Thread-safe singleton that publishes events and manages statistics.
/// </summary>
public class MarkdownFetchEventPublisher : IMarkdownFetchEventPublisher
{
    private readonly IMarkdownFetchService _fetchService;
    private readonly ILogger<MarkdownFetchEventPublisher> _logger;
    private readonly ConcurrentDictionary<string, TimeSpan> _fetchDurations = new();
    private int _totalFetches;
    private int _successfulFetches;
    private int _failedFetches;
    private int _cachedResponses;
    private int _externalUpdates;
    private DateTime? _lastFetchTime;

    public event EventHandler<FetchBeginningEventArgs>? FetchBeginning;
    public event EventHandler<FetchCompletedEventArgs>? FetchCompleted;
    public event EventHandler<FetchFailedEventArgs>? FetchFailed;
    public event EventHandler<ContentUpdatedEventArgs>? ContentUpdated;

    public MarkdownFetchEventPublisher(
        IMarkdownFetchService fetchService,
        ILogger<MarkdownFetchEventPublisher> logger)
    {
        _fetchService = fetchService;
        _logger = logger;
    }

    public void OnFetchBeginning(string url, int blogPostId, int pollFrequencyHours)
    {
        Interlocked.Increment(ref _totalFetches);
        _lastFetchTime = DateTime.UtcNow;

        var args = new FetchBeginningEventArgs(url, blogPostId, pollFrequencyHours);
        FetchBeginning?.Invoke(this, args);

        _logger.LogDebug("Fetch beginning for {Url} (BlogPostId: {BlogPostId})", url, blogPostId);
    }

    public void OnFetchCompleted(string url, int blogPostId, string content, TimeSpan duration, bool wasCached, bool wasStale)
    {
        Interlocked.Increment(ref _successfulFetches);

        if (wasCached)
        {
            Interlocked.Increment(ref _cachedResponses);
        }

        var key = $"{url}_{blogPostId}";
        _fetchDurations[key] = duration;

        var args = new FetchCompletedEventArgs(url, blogPostId, content, duration, wasCached, wasStale);
        FetchCompleted?.Invoke(this, args);

        if (!wasCached)
        {
            var updateArgs = new ContentUpdatedEventArgs(url, blogPostId, content, ContentUpdateSource.Fetch);
            ContentUpdated?.Invoke(this, updateArgs);
        }

        _logger.LogDebug("Fetch completed for {Url} (Duration: {Duration}ms, Cached: {Cached})",
            url, duration.TotalMilliseconds, wasCached);
    }

    public void OnFetchFailed(string url, int blogPostId, Exception exception, bool fallbackToCacheUsed)
    {
        Interlocked.Increment(ref _failedFetches);

        var args = new FetchFailedEventArgs(url, blogPostId, exception, fallbackToCacheUsed);
        FetchFailed?.Invoke(this, args);

        _logger.LogWarning(exception, "Fetch failed for {Url} (BlogPostId: {BlogPostId}, Fallback: {Fallback})",
            url, blogPostId, fallbackToCacheUsed);
    }

    public async Task InvalidateCacheAsync(string url, int blogPostId = 0)
    {
        _logger.LogInformation("Invalidating cache for {Url} (BlogPostId: {BlogPostId})", url, blogPostId);

        // Trigger a fresh fetch by setting poll frequency to 0
        var result = await _fetchService.FetchMarkdownAsync(url, 0, blogPostId);

        if (result.Success)
        {
            var args = new ContentUpdatedEventArgs(url, blogPostId, result.Content ?? string.Empty, ContentUpdateSource.CacheInvalidation);
            ContentUpdated?.Invoke(this, args);
        }
    }

    public async Task UpdateCachedContentAsync(string url, string markdown, int blogPostId = 0)
    {
        Interlocked.Increment(ref _externalUpdates);

        _logger.LogInformation("External update for {Url} (BlogPostId: {BlogPostId}, Length: {Length})",
            url, blogPostId, markdown?.Length ?? 0);

        // Store the content directly in cache by fetching with the provided content
        // This is a bit hacky - we'd need to extend IMarkdownFetchService to support direct cache writes
        // For now, we'll just publish the event
        var args = new ContentUpdatedEventArgs(url, blogPostId, markdown ?? string.Empty, ContentUpdateSource.ExternalUpdate);
        ContentUpdated?.Invoke(this, args);

        // Force a refresh on next access
        await InvalidateCacheAsync(url, blogPostId);
    }

    public FetchStatistics GetStatistics()
    {
        var durations = _fetchDurations.Values.ToList();
        var avgDuration = durations.Count > 0
            ? TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds))
            : TimeSpan.Zero;

        return new FetchStatistics
        {
            TotalFetches = _totalFetches,
            SuccessfulFetches = _successfulFetches,
            FailedFetches = _failedFetches,
            CachedResponses = _cachedResponses,
            ExternalUpdates = _externalUpdates,
            AverageFetchDuration = avgDuration,
            LastFetchTime = _lastFetchTime
        };
    }

    public async Task RemoveCachedContentAsync(string url, int blogPostId = 0)
    {
        _logger.LogInformation("Removing cached content for {Url} (BlogPostId: {BlogPostId})", url, blogPostId);

        // This requires the fetch service to support removal
        // Services can handle this by implementing IMarkdownFetchService.RemoveCachedMarkdownAsync
        if (_fetchService != null)
        {
            await _fetchService.RemoveCachedMarkdownAsync(url, blogPostId);
        }
    }

    public Task StopPollingAsync(string url, int blogPostId = 0)
    {
        _logger.LogInformation("Stop polling requested for {Url} (BlogPostId: {BlogPostId})", url, blogPostId);

        // If using IMarkdownFetchUpdateService, it should unregister the URL
        // For now, we'll just log it - polling services can subscribe to a StopPolling event if needed
        // This is a signal to external systems that polling should stop

        return Task.CompletedTask;
    }
}
