using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Markdown;

public static class PageCacheHelper
{
    private static readonly Dictionary<(string slug, string language), BlogPostDto> PageCache = new();
    public static Dictionary<(string slug, string lang), BlogPostDto> GetPageCache() => PageCache;
    
    public static void SetPageCache(Dictionary<(string slug, string lang), BlogPostDto> pages)
    {
        PageCache.Clear();
        
        foreach (var (key, value) in pages) PageCache.TryAdd(key, value);
    }
}