using System.Diagnostics;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Config;
using Mostlylucid.Models;
using Mostlylucid.Services.Markdown;
using Mostlylucidblog.Models;

namespace Mostlylucid.Controllers;


    public class HomeController(AuthSettings authSettings, BlogService blogService, AnalyticsSettings analyticsSettings, ILogger<HomeController> logger) 
        : BaseController(authSettings,analyticsSettings, blogService, logger)
    {
    [OutputCache(Duration = 60*60*60)]
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
   
            var authenticateResult = GetUserInfo();

            var posts = blogService.GetPostsForFiles(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    
    }

    public IActionResult Privacy()
    
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}