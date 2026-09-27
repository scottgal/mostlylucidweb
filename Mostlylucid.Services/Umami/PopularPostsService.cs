using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.Services.Blog;
using Mostlylucid.Shared.Models;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Mostlylucid.Services.Umami;

public interface IPopularPostsService
{
    Task<PopularPost?> GetMostPopularPostAsync();
    Task<PopularPost?> GetCachedPopularPost();
    Task<List<PopularPost>> GetTopPopularPostsAsync(int count = 5);
    List<PopularPost> GetCachedTopPopularPosts(int count = 5);
}

public class PopularPost
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Views { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PopularPostsService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PopularPostsService> logger) : IPopularPostsService
{
    private PopularPost? _cachedPopularPost;
    private List<PopularPost> _cachedTopPosts = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<PopularPost?> GetMostPopularPostAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var umamiDataService = scope.ServiceProvider.GetRequiredService<UmamiDataService>();
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddHours(-24); // Last 24 hours

            // Get URL metrics from Umami for the past 24 hours
            // Note: Umami v3 uses 'path' instead of 'url'
            var metricsRequest = new MetricsRequest
            {
                StartAtDate = startDate,
                EndAtDate = endDate,
                Type = MetricType.path,
                Unit = Unit.hour, // Use hour for 24-hour period (like Umami admin does)
                Timezone = "UTC", // Explicit timezone
                Limit = 100 // Limit to top 100
            };

            var result = await umamiDataService.GetMetrics(metricsRequest);

            if (result?.Status != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("Failed to get metrics from Umami: {Status}", result?.Status);
                return _cachedPopularPost;
            }

            if (result.Data == null || result.Data.Length == 0)
            {
                logger.LogWarning("No data returned from Umami API");
                return _cachedPopularPost;
            }

            logger.LogInformation("Received {Count} total paths from Umami", result.Data.Length);

            // Log first few paths to see what we're getting
            var samplePaths = result.Data.Take(10).Select(m => $"{m.x} ({m.y} views)");
            logger.LogInformation("Sample paths: {Paths}", string.Join(", ", samplePaths));

            // Filter for blog posts (URLs starting with /blog/)
            var blogPosts = result.Data
                .Where(m => m.x.StartsWith("/blog/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            logger.LogInformation("Found {Count} blog post paths after filtering", blogPosts.Count);

            if (blogPosts.Count == 0)
            {
                logger.LogWarning("No blog posts found in metrics (no paths starting with /blog/)");
                return _cachedPopularPost;
            }

            // Aggregate all language variants of the same post
            var aggregatedPosts = AggregateBySlug(blogPosts);

            // Find the most popular post
            var mostPopular = aggregatedPosts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

            if (mostPopular.Key == null)
            {
                logger.LogInformation("No aggregated posts found");
                return _cachedPopularPost;
            }

            // Get the blog post details to get the title
            var queryModel = new BlogPostQueryModel(mostPopular.Key, "en");
            var blogPost = await blogService.GetPost(queryModel);

            var popularPost = new PopularPost
            {
                Url = $"/blog/{mostPopular.Key}",
                Title = blogPost?.Title ?? mostPopular.Key.Replace("-", " "),
                Views = mostPopular.Value,
                LastUpdated = DateTime.UtcNow
            };

            _cachedPopularPost = popularPost;
            logger.LogInformation(
                "Most popular post in last 24h: {Title} ({Url}) with {Views} views",
                popularPost.Title,
                popularPost.Url,
                popularPost.Views);

            return popularPost;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting popular posts from Umami");
            return _cachedPopularPost; // Return cached if available
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static Dictionary<string, int> AggregateBySlug(IEnumerable<MetricsResponseModels> blogPaths)
    {
        var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in blogPaths)
        {
            var slug = GetPostSlug(path.x);
            if (slug == null) continue;
            aggregated[slug] = aggregated.GetValueOrDefault(slug) + path.y;
        }

        return aggregated;
    }

    // Blog routes that live under /blog/ but are not posts
    private static readonly HashSet<string> NonPostSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "category", "categories", "calendar-days", "date-range", "drafts", "language"
    };

    /// <summary>
    /// Maps a tracked path to the post it belongs to, so every language of a post counts once:
    /// /blog/{slug}, /blog/{language}/{slug}, /blog/language/{slug}/{language} and the older
    /// /blog/{slug}.{language}. Returns null for paths that are not a post.
    /// </summary>
    public static string? GetPostSlug(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var end = path.IndexOfAny(['?', '#']);
        if (end >= 0) path = path[..end];

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !segments[0].Equals("blog", StringComparison.OrdinalIgnoreCase)) return null;

        string slug;
        switch (segments.Length)
        {
            case 2 when !NonPostSegments.Contains(segments[1]):
                slug = segments[1];
                break;
            case 3 when segments[1].Length == 2:
                slug = segments[2];
                break;
            case 4 when segments[1].Equals("language", StringComparison.OrdinalIgnoreCase):
                slug = segments[2];
                break;
            default:
                return null;
        }

        // Older translated URLs carried the language as an extension (e.g. "slug.fr")
        var dot = slug.LastIndexOf('.');
        if (dot > 0) slug = slug[..dot];

        return slug;
    }

    public async Task<PopularPost?> GetCachedPopularPost()
    {
        return _cachedPopularPost ?? await GetMostPopularPostAsync();
    }

    public List<PopularPost> GetCachedTopPopularPosts(int count = 5)
    {
        return _cachedTopPosts.Take(count).ToList();
    }

    public async Task<List<PopularPost>> GetTopPopularPostsAsync(int count = 5)
    {
        await _semaphore.WaitAsync();
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var umamiDataService = scope.ServiceProvider.GetRequiredService<UmamiDataService>();
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddHours(-24);

            var metricsRequest = new MetricsRequest
            {
                StartAtDate = startDate,
                EndAtDate = endDate,
                Type = MetricType.path,
                Unit = Unit.hour,
                Timezone = "UTC",
                Limit = 100
            };

            var result = await umamiDataService.GetMetrics(metricsRequest);

            if (result?.Status != System.Net.HttpStatusCode.OK || result.Data == null || result.Data.Length == 0)
            {
                logger.LogWarning("Failed to get metrics from Umami for top posts");
                return _cachedTopPosts.Take(count).ToList();
            }

            // Filter for blog posts
            var blogPosts = result.Data
                .Where(m => m.x.StartsWith("/blog/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (blogPosts.Count == 0)
            {
                return _cachedTopPosts.Take(count).ToList();
            }

            // Aggregate all language variants of the same post
            var aggregatedPosts = AggregateBySlug(blogPosts);

            // Get top posts
            var topSlugs = aggregatedPosts
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .ToList();

            var topPosts = new List<PopularPost>();
            foreach (var item in topSlugs)
            {
                var queryModel = new BlogPostQueryModel(item.Key, "en");
                var blogPost = await blogService.GetPost(queryModel);

                topPosts.Add(new PopularPost
                {
                    Url = $"/blog/{item.Key}",
                    Title = blogPost?.Title ?? item.Key.Replace("-", " "),
                    Views = item.Value,
                    LastUpdated = DateTime.UtcNow
                });
            }

            _cachedTopPosts = topPosts;

            // Also update the single most popular post cache
            if (topPosts.Count > 0)
                _cachedPopularPost = topPosts[0];

            logger.LogInformation("Cached {Count} top popular posts", topPosts.Count);
            return topPosts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting top popular posts from Umami");
            return _cachedTopPosts.Take(count).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
