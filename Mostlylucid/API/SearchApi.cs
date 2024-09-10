using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mostlylucid.Blog;
using Mostlylucid.EntityFramework;
using Mostlylucid.OpenSearch;
using NpgsqlTypes;
using Serilog;
using SerilogTracing;
using Umami.Net;
using Umami.Net.Models;

namespace Mostlylucid.API;

[ApiController]
[Route("api")]
public class SearchApi(IMostlylucidDBContext context, UmamiBackgroundSender umamiBackgroundSender, SearchService indexService) : ControllerBase
{
    [HttpGet]
    [Route("osearch/{query}")]
    [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
    
    [HttpGet]
    [Route("search/{query}")]
    [ValidateAntiForgeryToken]
    public async Task<Results<JsonHttpResult<List<SearchResults>>, BadRequest<string>>> Search(string query)
    {
        using var activity = Log.Logger.StartActivity("Search {query}", query);
        try
        {

       
        List<(string Title, string Slug)> posts = new();
        if (!query.Contains(" "))
        {
            posts = await GetSearchResultForComplete(query);
        }
        else
        {
            posts = await GetSearchResultForQuery(query);
        }
        var encodedQuery = System.Web.HttpUtility.UrlEncode(query);
        
       await  umamiBackgroundSender.Track( "searchEvent",  eventData: new UmamiEventData(){{"query", encodedQuery}});
      
        var host = Request.Host.Value;
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        
        activity?.Activity?.SetTag("Results", output.Count);

        activity?.Complete();
        return TypedResults.Json(output);
        }
        catch (Exception e)
        {
              Log.Error(e, "Error in search");
              return TypedResults.BadRequest("Error in search");
        }
    }
    
    
    private async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
        var processedQuery = query;
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)) // Use precomputed SearchVector for title and content
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)))) // Search in categories
                && x.LanguageEntity.Name == "en")// Filter by language
            
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", processedQuery))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug,  })
            .Take(5)
            .ToListAsync();
        return posts.Select(x=> (x.Title, x.Slug)).ToList();
    }
    private async Task<List<(string Title, string Slug)>> GetSearchResultForComplete(string query)
    {
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*")))) // Search in categories
                && x.LanguageEntity.Name == "en")// Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug,  })
            .Take(5)
            .ToListAsync();
        return posts.Select(x=> (x.Title, x.Slug)).ToList();
    }
    
    public record SearchResults(string Title, string Slug, string Url);
    
    
}