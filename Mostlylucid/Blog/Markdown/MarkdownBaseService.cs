using Mostlylucid.Shared.Config.Markdown;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBaseService(MarkdownConfig markdownConfig)
{
    protected MarkdownConfig MarkdownConfig => markdownConfig;
        
    private static readonly Dictionary<(string slug, string language), BlogPostDto> PageCache = new();

    private ParallelOptions ParallelOptions => new() { MaxDegreeOfParallelism = 4 };


    private string DirectoryPath => markdownConfig.MarkdownPath;
    
    protected Dictionary<(string slug, string lang), BlogPostDto> GetPageCache() => PageCache;


    protected void SetPageCache(Dictionary<(string slug, string lang), BlogPostDto> pages)
    {
        foreach (var (key, value) in pages) PageCache.TryAdd(key, value);
    }

}