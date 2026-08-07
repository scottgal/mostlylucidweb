using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Mostlylucid.Markdig.FetchExtension.Events;
using Mostlylucid.Markdig.FetchExtension.Models;
using Mostlylucid.Markdig.FetchExtension.Services;

namespace Mostlylucid.Markdig.FetchExtension.Storage;

/// <summary>
///     Simple in-memory implementation of IMarkdownFetchService.
///     Useful for demos, testing, or apps that don't need persistence.
/// </summary>
public class InMemoryMarkdownFetchService : IMarkdownFetchService, ICacheInspector
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InMemoryMarkdownFetchService> _logger;
    private readonly IMarkdownFetchEventPublisher? _eventPublisher;

    public InMemoryMarkdownFetchService(
        IHttpClientFactory httpClientFactory,
        ILogger<InMemoryMarkdownFetchService> logger,
        IMarkdownFetchEventPublisher? eventPublisher = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task<MarkdownFetchResult> FetchMarkdownAsync(
        string url,
        int pollFrequencyHours,
        int blogPostId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var cacheKey = GetCacheKey(url, blogPostId);

            // Check if we have cached content
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                var age = DateTimeOffset.UtcNow - cached.LastFetchedAt;
                var isStale = age.TotalHours >= pollFrequencyHours;

                if (!isStale && !string.IsNullOrEmpty(cached.Content))
                {
                    _logger.LogDebug(
                        "Returning cached markdown for {Url} (age: {Age:F2}h)",
                        url,
                        age.TotalHours);

                    stopwatch.Stop();
                    _eventPublisher?.OnFetchCompleted(url, blogPostId, cached.Content, stopwatch.Elapsed, true, false);

                    return new MarkdownFetchResult
                    {
                        Success = true,
                        Content = cached.Content,
                        LastRetrieved = cached.LastFetchedAt.UtcDateTime,
                        IsCached = true,
                        IsStale = false,
                        SourceUrl = url,
                        PollFrequencyHours = pollFrequencyHours
                    };
                }
            }

            // Publish fetch beginning event
            _eventPublisher?.OnFetchBeginning(url, blogPostId, pollFrequencyHours);

            // Fetch fresh content
            var result = await FetchFromUrlAsync(url);

            if (result.Success && !string.IsNullOrEmpty(result.Content))
            {
                var now = DateTimeOffset.UtcNow;
                var hash = ComputeHash(result.Content);
                _cache.AddOrUpdate(cacheKey,
                    _ => new CacheEntry
                    {
                        Url = url,
                        BlogPostId = blogPostId,
                        Content = result.Content,
                        ContentHash = hash,
                        LastFetchedAt = now,
                        PollFrequencyHours = pollFrequencyHours
                    },
                    (_, existing) =>
                    {
                        existing.Content = result.Content;
                        existing.ContentHash = hash;
                        existing.LastFetchedAt = now;
                        existing.PollFrequencyHours = pollFrequencyHours;
                        return existing;
                    });

                _logger.LogInformation("Successfully fetched and cached markdown from {Url}", url);

                // Update result with metadata
                result.LastRetrieved = now.UtcDateTime;
                result.IsCached = false;
                result.IsStale = false;
                result.SourceUrl = url;
                result.PollFrequencyHours = pollFrequencyHours;

                stopwatch.Stop();
                _eventPublisher?.OnFetchCompleted(url, blogPostId, result.Content, stopwatch.Elapsed, false, false);
            }
            else if (cached != null && !string.IsNullOrEmpty(cached.Content))
            {
                // Fetch failed but we have cached content, return stale cache
                _logger.LogWarning(
                    "Fetch failed for {Url}, returning stale cached content. Error: {Error}",
                    url,
                    result.ErrorMessage);

                stopwatch.Stop();
                _eventPublisher?.OnFetchFailed(url, blogPostId, new Exception(result.ErrorMessage ?? "Unknown error"), true);
                _eventPublisher?.OnFetchCompleted(url, blogPostId, cached.Content, stopwatch.Elapsed, true, true);

                return new MarkdownFetchResult
                {
                    Success = true,
                    Content = cached.Content,
                    LastRetrieved = cached.LastFetchedAt.UtcDateTime,
                    IsCached = true,
                    IsStale = true,
                    SourceUrl = url,
                    PollFrequencyHours = pollFrequencyHours
                };
            }
            else
            {
                // Fetch failed and no cache available
                stopwatch.Stop();
                _eventPublisher?.OnFetchFailed(url, blogPostId, new Exception(result.ErrorMessage ?? "Unknown error"), false);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching markdown from {Url}", url);
            stopwatch.Stop();
            _eventPublisher?.OnFetchFailed(url, blogPostId, ex, false);

            return new MarkdownFetchResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private async Task<MarkdownFetchResult> FetchFromUrlAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new MarkdownFetchResult
                {
                    Success = false,
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                };
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new MarkdownFetchResult
                {
                    Success = false,
                    ErrorMessage = "Fetched content was empty"
                };
            }

            return new MarkdownFetchResult
            {
                Success = true,
                Content = content
            };
        }
        catch (TaskCanceledException)
        {
            return new MarkdownFetchResult
            {
                Success = false,
                ErrorMessage = "Request timed out after 30 seconds"
            };
        }
        catch (HttpRequestException ex)
        {
            return new MarkdownFetchResult
            {
                Success = false,
                ErrorMessage = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from URL {Url}", url);
            return new MarkdownFetchResult
            {
                Success = false,
                ErrorMessage = $"Fetch error: {ex.Message}"
            };
        }
    }

    private static string GetCacheKey(string url, int blogPostId)
    {
        return blogPostId > 0 ? $"{url}|{blogPostId}" : url;
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    // ICacheInspector implementation
    public IEnumerable<CachedMarkdownEntry> GetAllCachedEntries()
    {
        return _cache.Values
            .OrderByDescending(e => e.LastFetchedAt)
            .Take(100)
            .Select(e => new CachedMarkdownEntry
            {
                Url = e.Url,
                BlogPostId = e.BlogPostId,
                Content = e.Content,
                ContentHash = e.ContentHash,
                LastFetchedAt = e.LastFetchedAt.UtcDateTime,
                PollFrequencyHours = e.PollFrequencyHours
            })
            .ToList();
    }

    public CachedMarkdownEntry? GetCachedEntry(string url, int blogPostId = 0)
    {
        var cacheKey = GetCacheKey(url, blogPostId);
        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            return new CachedMarkdownEntry
            {
                Url = entry.Url,
                BlogPostId = entry.BlogPostId,
                Content = entry.Content,
                ContentHash = entry.ContentHash,
                LastFetchedAt = entry.LastFetchedAt.UtcDateTime,
                PollFrequencyHours = entry.PollFrequencyHours
            };
        }
        return null;
    }

    public Task<bool> RemoveCachedMarkdownAsync(string url, int blogPostId = 0)
    {
        var cacheKey = GetCacheKey(url, blogPostId);
        var removed = _cache.TryRemove(cacheKey, out _);

        if (removed)
        {
            _logger.LogInformation("Removed cached markdown for {Url} (blogPostId: {BlogPostId})", url, blogPostId);
        }

        return Task.FromResult(removed);
    }

    private class CacheEntry
    {
        public required string Url { get; init; }
        public int BlogPostId { get; init; }
        public required string Content { get; set; }
        public required string ContentHash { get; set; }
        public DateTimeOffset LastFetchedAt { get; set; }
        public int PollFrequencyHours { get; set; }
    }
}
