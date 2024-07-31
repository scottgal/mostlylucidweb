using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Services;

namespace Mostlylucidblog.Controllers;

[Route("blog")]
public class BlogController(BlogService blogService, ILogger<BlogController> logger) : Controller
{

    
    public IActionResult Index()
    {
        var posts = blogService.GetPosts();
        return View("Index", posts);
    }

    [Route("show/{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       return View("Post", post);
    }
}