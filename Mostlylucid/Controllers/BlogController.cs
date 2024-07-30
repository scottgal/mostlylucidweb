using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services;

namespace Mostlylucidblog.Controllers;

public class BlogController(BlogService blogService, ILogger<BlogController> logger) : Controller
{


    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       return View();
    }
}