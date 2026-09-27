using System.Diagnostics;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Models;
using Mostlylucid.Models.Blog;
using mostlylucid.pagingtaghelper.Helpers;
using Mostlylucid.Services;
using Mostlylucidblog.Models;
using MarkdownBaseService = Mostlylucid.Services.Markdown.MarkdownBaseService;

namespace Mostlylucid.Controllers;

public class HomeController(BaseControllerService baseControllerService, ILogger<HomeController> logger)
    : BaseController(baseControllerService, logger)
{
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request", "pagerequest", "homerequest", "Cookie" },
        VaryByQueryKeys = new[] { "page", "pageSize", "startDate", "endDate", "language", "orderBy", "orderDir", "order", "category" },
        Tags = new[] { SiteContentService.OutputCacheTag })]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null,
        string language = MarkdownBaseService.EnglishLanguage, string orderBy = "date", string orderDir = "desc",
        string? order = null, string? category = null, [FromHeader] bool homerequest = false)
    {
        var authenticateResult = await GetUserInfo();

        // Parse combined order param (e.g., "date_asc") if provided
        if (!string.IsNullOrEmpty(order) && order.Contains('_'))
        {
            var parts = order.Split('_');
            orderBy = parts[0];
            orderDir = parts[1];
        }

        // Use the same index logic as BlogController
        var request = new BlogIndexRequest(page, pageSize, startDate, endDate, language, orderBy, orderDir, order, category);
        var result = await BlogViewService.GetIndexDataAsync(request);

        result.Posts.LinkUrl = Url.Action("Index", "Home", new { startDate, endDate, language, order = order ?? $"{orderBy}_{orderDir}", category });

        var indexPageViewModel = new IndexPageViewModel
        {
            Posts = result.Posts,
            Authenticated = authenticateResult.LoggedIn,
            Name = authenticateResult.Name,
            AvatarUrl = authenticateResult.AvatarUrl
        };

        // For paging requests (has pagerequest header) or filter changes (targeting #content)
        var htmxTarget = Request.Headers["HX-Target"].FirstOrDefault();
        if (Request.IsPageRequest() || htmxTarget == "content")
            return PartialView("_BlogSummaryList", result.Posts);

        // For home button click (has homerequest header)
        if (homerequest)
            return PartialView("_HomePartial", indexPageViewModel);

        // Full page load
        return View(indexPageViewModel);
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [OutputCache(Duration = 86400, VaryByHeaderNames = new[] { "hx-request" })] // Cache for 24 hours
    [HttpGet("software")]
    public IActionResult Software()
    {
        if (Request.IsHtmx())
        {
            return PartialView("Software");
        }
        return View();
    }
}