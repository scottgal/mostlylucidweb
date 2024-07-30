using ASP;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services;

namespace Mostlylucidblog.Controllers;

public class BlogController(BlogService blogService, ILogger<BlogController> logger) : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }


    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       return View();
    }
}