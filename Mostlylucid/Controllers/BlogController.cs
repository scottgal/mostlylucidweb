using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Services;

namespace Mostlylucidblog.Controllers;

[Route("blog")]
public class BlogController(BlogService blogService, ILogger<BlogController> logger) : Controller
{

    
    public IActionResult Index()
    {
        var posts = blogService.GetPostsForFiles();
        return View("Index", posts);
    }

    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }

    [Route("category/{category}")]
    public IActionResult Category(string category)
    {
        ViewBag.Category = category;
        var posts = blogService.GetPostsByCategory(category);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        return View("Index", posts);
    }
    
    [Route("/{language}/{slug}")]
    public IActionResult Language(string slug, string language)
    {
        var post = blogService.GetPost(slug, language);
        if(Request.IsHtmx())
        {
            return PartialView("_PostPartial", post);
        }
        return View("Post", post);
    }
}