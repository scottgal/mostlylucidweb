using System.ComponentModel.DataAnnotations;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog.EntityFramework;
using Mostlylucid.Models.Search;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

[Route("search")]
public class SearchController(
    BaseControllerService baseControllerService,
    BlogSearchService searchService,
    ILogger<SearchController> logger)
    : BaseController(baseControllerService, logger)
{
    [HttpGet]
    [Route("{query?}")]
    public async Task<IActionResult> Search([FromRoute] string? query)
    {
        var searchResults = await searchService.GetPosts(query);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        searchModel.SearchResults.LinkUrl = Url.Action("SearchResults", "Search");
        if (Request.IsHtmx()) return PartialView("SearchResults", searchModel);
        return View("SearchResults", searchModel);
    }

    [HttpGet]
    [Route("results")]
    public async Task<IActionResult> SearchResults([Required] string query, int page = 1, int pageSize = 10)
    {
        var searchResults = await searchService.GetPosts(query, page, pageSize);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        searchModel.SearchResults.LinkUrl = Url.Action("SearchResults", "Search");
        if (Request.IsHtmx()) return PartialView("_SearchResultsPartial", searchModel.SearchResults);
        return View("SearchResults", searchModel);
    }
}