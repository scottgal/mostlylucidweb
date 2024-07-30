using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using System.IO;
using Mostlylucidblog.Models;

namespace Mostlylucidblog.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        List<IndexPageViewModel> pageModels = new();
        var pages = Directory.GetFiles("~/Markdown" , "*.md");
        foreach (var page in pages)
        {
            var fileInfo = new FileInfo(page);
            var lines = System.IO.File.ReadAllLines(page);
            var title = lines[0];
          
            pageModels.Add(new IndexPageViewModel { Title = title, Path = page, DateModified =fileInfo.LastWriteTime });
        }
        return View();
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