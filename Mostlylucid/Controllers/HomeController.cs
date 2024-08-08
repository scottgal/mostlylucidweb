using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Config;
using Mostlylucid.Models;
using Mostlylucid.Services.Markdown;
using Mostlylucidblog.Models;

namespace Mostlylucid.Controllers;


    public class HomeController(AuthSettings authSettings, BlogService blogService, ILogger<HomeController> logger) 
        : BaseController(authSettings, blogService, logger)
    {
    [OutputCache(Duration = 60*60*60)]
    public async Task<IActionResult> Index()
    {
        var authenticateResult = GetUserInfo();
       
        var posts = blogService.GetPostsForFiles();
      var indexPageViewModel = new IndexPageViewModel { Posts = posts, Authenticated =  authenticateResult.LoggedIn, Name = authenticateResult.Name, AvatarUrl = authenticateResult.AvatarUrl };
 
      indexPageViewModel.Categories = blogService.GetCategories();
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