using System.Diagnostics;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Models;
using Mostlylucid.Services;
using Mostlylucidblog.Models;

namespace Mostlylucid.Controllers;

public class HomeController(BaseControllerService baseControllerService, ILogger<HomeController> logger)
    : BaseController(baseControllerService, logger)
{
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { "page", "pageSize" })]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { "page", "pageSize" },
        Location = ResponseCacheLocation.Any)]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var authenticateResult = await GetUserInfo();
        var posts = await BlogService.GetPagedPosts(page, pageSize);
        posts.LinkUrl = Url.Action("Index", "Home");
        if (Request.IsHtmx()) return PartialView("_BlogSummaryList", posts);
        var indexPageViewModel = new IndexPageViewModel
        {
            Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
            AvatarUrl = authenticateResult.AvatarUrl
        };

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
}