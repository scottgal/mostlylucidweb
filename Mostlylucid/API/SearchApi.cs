using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.SemanticSearch.Config;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Services.Umami;
using Serilog.Events;
using Umami.Net.Models;

namespace Mostlylucid.API;

[ApiController]
[Route("api")]
public class SearchApi(
    IBlogSearchService searchService,
    UmamiBackgroundSender umamiBackgroundSender,
    SearchService indexService,
    SemanticSearchConfig semanticSearchConfig,
    IPopularPostsService popularPostsService) : ControllerBase
{

    [HttpGet]
    [Route("osearch/{query}")]
    [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<BlogSearchService.SearchResults>>> OpenSearch(string query,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);

        var host = Request.Host.Value;
        var output = results.Select(x => new BlogSearchService.SearchResults(x.Title.Trim(), x.Slug,
            Url.ActionLink("Show", "Blog", new { slug = x.Slug }, "https", host))).ToList();
        return TypedResults.Json(output);
    }

    [HttpGet]
    [Route("typeahead/{query}")]
    [OutputCache(Duration = 3600, VaryByRouteValueNames = new[] { "query" })]
    public async Task<Results<JsonHttpResult<List<BlogSearchService.SearchResults>>, BadRequest<string>>>
        Search(string query)
    {
        using var activity = Log.Logger.StartActivity("Search {query}", query);
        try
        {
            var host = Request.Host.Value;
            List<BlogSearchService.SearchResults> output;

            // Use hybrid search if semantic search is enabled
            if (semanticSearchConfig.Enabled)
            {
                output = await HybridSearchAsync(query, host);
            }
            else
            {
                // Fallback to full-text search only
                output = await FullTextSearchAsync(query, host);
            }

            var encodedQuery = HttpUtility.UrlEncode(query);
            await umamiBackgroundSender.Track("searchEvent", new UmamiEventData { { "query", encodedQuery } });

            activity?.Activity?.SetTag("Results", output.Count);
            activity?.Complete();
            return TypedResults.Json(output);
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
            Log.Error(e, "Error in search");
            return TypedResults.BadRequest("Error in search");
        }
    }

    private async Task<List<BlogSearchService.SearchResults>> FullTextSearchAsync(string query, string host)
    {
        List<(string Title, string Slug)> posts;
        if (!query.Contains(' '))
            posts = await searchService.GetSearchResultForComplete(query);
        else
            posts = await searchService.GetSearchResultForQuery(query);

        return posts.Select(x => new BlogSearchService.SearchResults(
            x.Title.Trim(),
            x.Slug,
            Url.ActionLink("Show", "Blog", new { slug = x.Slug }, "https", host))).ToList();
    }

    private async Task<List<BlogSearchService.SearchResults>> HybridSearchAsync(string query, string host)
    {
        // Use the BlogSearchService which handles PostgreSQL lookups for titles
        var results = await searchService.HybridSearchAsync(query);

        // Add full URLs
        return results.Select(r => new BlogSearchService.SearchResults(
            r.Title,
            r.Slug,
            Url.ActionLink("Show", "Blog", new { slug = r.Slug }, "https", host),
            r.Score
        )).ToList();
    }

    /// <summary>
    /// Get the top 5 popular posts from cached Umami data.
    /// Used by the search typeahead to show popular posts on focus.
    /// </summary>
    [HttpGet]
    [Route("popular")]
    public JsonHttpResult<List<BlogSearchService.SearchResults>> GetPopularPosts()
    {
        var host = Request.Host.Value;
        var cachedPosts = popularPostsService.GetCachedTopPopularPosts(5);

        var output = cachedPosts.Select(p =>
        {
            // Extract slug from URL (remove /blog/ prefix)
            var slug = p.Url.StartsWith("/blog/") ? p.Url.Substring(6) : p.Url;
            return new BlogSearchService.SearchResults(
                p.Title,
                slug,
                Url.ActionLink("Show", "Blog", new { slug }, "https", host) ?? p.Url
            );
        }).ToList();

        return TypedResults.Json(output);
    }

    /// <summary>
    /// Get search term suggestions for autocomplete (not article titles - just search terms)
    /// Returns terms extracted from actual blog content: categories, common title words, etc.
    /// </summary>
    [HttpGet]
    [Route("suggest/{prefix}")]
    [OutputCache(Duration = 3600, VaryByRouteValueNames = new[] { "prefix" })]
    public async Task<JsonHttpResult<List<string>>> GetSearchSuggestions(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 2)
        {
            return TypedResults.Json(new List<string>());
        }

        var suggestions = await searchService.GetSearchTermSuggestions(prefix, 10);
        return TypedResults.Json(suggestions);
    }
}