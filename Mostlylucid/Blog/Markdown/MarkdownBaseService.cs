using Mostlylucid.Config.Markdown;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBaseService(MarkdownConfig markdownConfig) : Blog.MarkdownBaseService
{
    protected MarkdownConfig MarkdownConfig => markdownConfig;
        
    private static readonly Dictionary<(string slug, string language), BlogPostViewModel> PageCache = new();

    private ParallelOptions ParallelOptions => new() { MaxDegreeOfParallelism = 4 };


    private string DirectoryPath => markdownConfig.MarkdownPath;
    
    protected Dictionary<(string slug, string lang), BlogPostViewModel> GetPageCache() => PageCache;


    protected void SetPageCache(Dictionary<(string slug, string lang), BlogPostViewModel> pages)
    {
        foreach (var (key, value) in pages) PageCache.TryAdd(key, value);
    }

}