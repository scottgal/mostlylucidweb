using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog.EntityFramework;
using Serilog.Events;
using Umami.Net.Models;

namespace Mostlylucid.API;

[ApiController]
[Route("api")]
public class SearchApi(
    BlogSearchService searchService,
    UmamiBackgroundSender umamiBackgroundSender,
    SearchService indexService) : ControllerBase
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
            Url.ActionLink("Show", "Blog", new { x.Slug }, "https", host))).ToList();
        return TypedResults.Json(output);
    }

    [HttpGet]
    [Route("search/{query}")]
    public async Task<Results<JsonHttpResult<List<BlogSearchService.SearchResults>>, BadRequest<string>>>
        Search(string query)
    {
        using var activity = Log.Logger.StartActivity("Search {query}", query);
        try
        {
            List<(string Title, string Slug)> posts = new();
            if (!query.Contains(" "))
                posts = await searchService.GetSearchResultForComplete(query);
            else
                posts = await searchService.GetSearchResultForQuery(query);
            var encodedQuery = HttpUtility.UrlEncode(query);

            await umamiBackgroundSender.Track("searchEvent", new UmamiEventData { { "query", encodedQuery } });

            var host = Request.Host.Value;
            var output = posts.Select(x => new BlogSearchService.SearchResults(x.Title.Trim(), x.Slug,
                Url.ActionLink("Show", "Blog", new { x.Slug }, "https", host))).ToList();

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
}