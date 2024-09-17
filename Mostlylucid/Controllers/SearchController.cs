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
    [Route("")]
    public async Task<IActionResult> Search(string? query, int page = 1, int pageSize = 10,[FromHeader] bool pagerequest=false)
    {
        var searchResults = await searchService.GetPosts(query, page, pageSize);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        var linkUrl = Url.Action("Search", "Search");
        searchModel.SearchResults.LinkUrl = linkUrl;
        if(pagerequest && Request.IsHtmx()) return PartialView("_SearchResultsPartial", searchModel.SearchResults);
        
        if (Request.IsHtmx()) return PartialView("SearchResults", searchModel);
        return View("SearchResults", searchModel);
    }

    [HttpGet]
    [Route("{query}")]
    public  IActionResult InitialSearch([FromRoute] string query)
    {
        return RedirectToAction("Search", new { query });
    }

}