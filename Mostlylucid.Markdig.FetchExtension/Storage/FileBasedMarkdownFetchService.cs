using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mostlylucid.Markdig.FetchExtension.Events;
using Mostlylucid.Markdig.FetchExtension.Models;
using Mostlylucid.Markdig.FetchExtension.Services;

namespace Mostlylucid.Markdig.FetchExtension.Storage;

/// <summary>
///     File-based implementation of IMarkdownFetchService.
///     Caches fetched markdown to disk for persistence across restarts.
/// </summary>
public class FileBasedMarkdownFetchService : IMarkdownFetchService
{
    // Cached: allocating JsonSerializerOptions per call defeats the serializer's internal caching.
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    private readonly string _cacheDirectory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileBasedMarkdownFetchService> _logger;
    private readonly IMarkdownFetchEventPublisher? _eventPublisher;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public FileBasedMarkdownFetchService(
        IHttpClientFactory httpClientFactory,
        ILogger<FileBasedMarkdownFetchService> logger,
        IMarkdownFetchEventPublisher? eventPublisher = null,
        string? cacheDirectory = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _cacheDirectory = cacheDirectory ?? Path.Combine(Path.GetTempPath(), "MarkdownFetchCache");

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
        _logger.LogInformation("FileBasedMarkdownFetchService initialized with cache directory: {Directory}",
            _cacheDirectory);
    }

    public async Task<MarkdownFetchResult> FetchMarkdownAsync(
        string url,
        int pollFrequencyHours,
        int blogPostId)
    {
        try
        {
            var cacheKey = GetCacheKey(url, blogPostId);
            var cacheFile = GetCacheFilePath(cacheKey);

            // Check if we have cached content
            var cached = await LoadCacheEntryAsync(cacheFile);
            if (cached != null)
            {
                var age = DateTimeOffset.UtcNow - cached.LastFetchedAt;
                var isStale = age.TotalHours >= pollFrequencyHours;

                if (!isStale && !string.IsNullOrEmpty(cached.Content))
                {
                    _logger.LogDebug(
                        "Returning cached markdown for {Url} from file (age: {Age:F2}h)",
                        url,
                        age.TotalHours);

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

            // Fetch fresh content
            var result = await FetchFromUrlAsync(url);

            if (result.Success && !string.IsNullOrEmpty(result.Content))
            {
                var now = DateTimeOffset.UtcNow;
                var hash = ComputeHash(result.Content);
                var newEntry = new CacheEntry
                {
                    Url = url,
                    Content = result.Content,
                    ContentHash = hash,
                    LastFetchedAt = now,
                    PollFrequencyHours = pollFrequencyHours
                };

                await SaveCacheEntryAsync(cacheFile, newEntry);
                _logger.LogInformation("Successfully fetched and cached markdown from {Url} to file", url);

                // Update result with metadata
                result.LastRetrieved = now.UtcDateTime;
                result.IsCached = false;
                result.IsStale = false;
                result.SourceUrl = url;
                result.PollFrequencyHours = pollFrequencyHours;
            }
            else if (cached != null && !string.IsNullOrEmpty(cached.Content))
            {
                // Fetch failed but we have cached content, return stale cache
                _logger.LogWarning(
                    "Fetch failed for {Url}, returning stale cached content from file. Error: {Error}",
                    url,
                    result.ErrorMessage);

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

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching markdown from {Url}", url);
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

    private async Task<CacheEntry?> LoadCacheEntryAsync(string cacheFile)
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(cacheFile))
                return null;

            var json = await File.ReadAllTextAsync(cacheFile);
            return JsonSerializer.Deserialize<CacheEntry>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cache entry from {File}", cacheFile);
            return null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> RemoveCachedMarkdownAsync(string url, int blogPostId = 0)
    {
        var cacheKey = GetCacheKey(url, blogPostId);
        var cacheFile = GetCacheFilePath(cacheKey);

        if (!File.Exists(cacheFile))
        {
            return false;
        }

        await _fileLock.WaitAsync();
        try
        {
            File.Delete(cacheFile);
            _logger.LogInformation("Removed cached markdown file for {Url} (blogPostId: {BlogPostId})", url, blogPostId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache file {File}", cacheFile);
            return false;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveCacheEntryAsync(string cacheFile, CacheEntry entry)
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(entry, IndentedJson);
            await File.WriteAllTextAsync(cacheFile, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cache entry to {File}", cacheFile);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private string GetCacheFilePath(string cacheKey)
    {
        // Use hash as filename to avoid invalid characters
        var fileName = ComputeHash(cacheKey) + ".json";
        return Path.Combine(_cacheDirectory, fileName);
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

    private class CacheEntry
    {
        public required string Url { get; init; }
        public required string Content { get; set; }
        public required string ContentHash { get; set; }
        public DateTimeOffset LastFetchedAt { get; set; }
        public int PollFrequencyHours { get; set; }
    }
}
