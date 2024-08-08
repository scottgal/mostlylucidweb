using System.Security.Claims;
using Htmx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Config;
using Mostlylucid.Controllers;
using Mostlylucid.Services;
using Mostlylucid.Services.Markdown;

namespace Mostlylucidblog.Controllers;

[Route("blog")]
public class BlogController(AuthSettings authSettings, BlogService blogService, CommentService commentService,
    ILogger<BlogController> logger) : BaseController(authSettings,blogService, logger)
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
       var user = GetUserInfo();
       post.Authenticated = user.LoggedIn;
       post.Name = user.Name;
       post.AvatarUrl = user.AvatarUrl;
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
        var user = GetUserInfo();
        posts.Authenticated = user.LoggedIn;
        posts.Name = user.Name;
        posts.AvatarUrl = user.AvatarUrl;
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        return View("Index", posts);
    }
    
    [Route("language/{slug}/{language}")]
    public IActionResult Compat(string slug, string language)
    {
       return RedirectToAction(nameof(Language), new { slug, language });
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