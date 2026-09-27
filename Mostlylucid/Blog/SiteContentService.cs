using System.Security.Cryptography;
using System.Text;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Shared.Config.Markdown;

namespace Mostlylucid.Blog;

/// <summary>
/// Rendered site content: the HTML plus a short hash of the source, which changes whenever the
/// file is edited (used to re-show a dismissed banner after its text changes).
/// </summary>
public record SiteContent(string Html, string Hash);

public interface ISiteContentService
{
    /// <summary>
    /// Renders {MarkdownPath}/site/{name}.md, or returns null when the file is missing or empty.
    /// </summary>
    SiteContent? Get(string name);
}

/// <summary>
/// Serves editable page fragments (home page intro, site announcement) from markdown files in the
/// site/ folder of the markdown directory. These are not blog posts: every ingest path only scans
/// the top level of the markdown directory, and the directory watcher skips this folder apart
/// from evicting the output cache so edits show up straight away.
/// </summary>
public class SiteContentService(
    MarkdownConfig markdownConfig,
    IMemoryCache memoryCache,
    ILogger<SiteContentService> logger) : ISiteContentService
{
    public const string FolderName = "site";
    public const string OutputCacheTag = "sitecontent";

    public const string HomeIntro = "home-intro";
    public const string Announcement = "announcement";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static bool IsSiteContentPath(string relativeName) =>
        relativeName.StartsWith(FolderName + "/", StringComparison.OrdinalIgnoreCase) ||
        relativeName.StartsWith(FolderName + "\\", StringComparison.OrdinalIgnoreCase);

    public SiteContent? Get(string name)
    {
        var path = Path.Combine(markdownConfig.MarkdownPath, FolderName, $"{name}.md");

        try
        {
            var file = new FileInfo(path);
            if (!file.Exists) return null;

            // Keyed on the file's write time and size, so an edit is picked up on the next request
            // without anything having to invalidate this cache.
            var cacheKey = $"sitecontent:{name}:{file.LastWriteTimeUtc.Ticks}:{file.Length}";
            return memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var markdown = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(markdown)) return null;

                var html = global::Markdig.Markdown.ToHtml(markdown, Pipeline);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(markdown)))[..12]
                    .ToLowerInvariant();
                return new SiteContent(html, hash);
            });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(e, "Could not read site content {Name} from {Path}", name, path);
            return null;
        }
    }
}
