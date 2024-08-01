using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Models;
using Mostlylucid.Services;
using Mostlylucidblog.Models;

namespace Mostlylucid.Controllers;


    public class HomeController(BlogService blogService, ILogger<HomeController> logger) : Controller
    {
    [OutputCache(Duration = 60*60*60)]
    public IActionResult Index()
    {
        var posts = blogService.GetPostsForFiles();
      var indexPageViewModel = new IndexPageViewModel { Posts = posts };
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