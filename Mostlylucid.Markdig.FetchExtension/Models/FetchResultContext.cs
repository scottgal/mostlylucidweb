using System.Collections.Concurrent;
using Markdig.Syntax;

namespace Mostlylucid.Markdig.FetchExtension.Models;

/// <summary>
///     Stores fetch results during document parsing so they can be referenced by fetch-summary tags
/// </summary>
public class FetchResultContext
{
    private const string ContextKey = "FetchResultContext";
    private readonly ConcurrentDictionary<string, MarkdownFetchResult> _results = new();

    /// <summary>
    ///     Store a fetch result by URL
    /// </summary>
    public void StoreResult(string url, MarkdownFetchResult result)
    {
        _results[url] = result;
    }

    /// <summary>
    ///     Retrieve a fetch result by URL
    /// </summary>
    public MarkdownFetchResult? GetResult(string url)
    {
        return _results.TryGetValue(url, out var result) ? result : null;
    }

    /// <summary>
    ///     Get or create the context for a MarkdownDocument
    /// </summary>
    public static FetchResultContext GetOrCreate(MarkdownDocument document)
    {
        if (document.GetData(ContextKey) is FetchResultContext existing)
            return existing;

        var context = new FetchResultContext();
        document.SetData(ContextKey, context);
        return context;
    }

    /// <summary>
    ///     Try to get the context from a MarkdownDocument
    /// </summary>
    public static FetchResultContext? TryGet(MarkdownDocument? document)
    {
        return document?.GetData(ContextKey) as FetchResultContext;
    }
}
