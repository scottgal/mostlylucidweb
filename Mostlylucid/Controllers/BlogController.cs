using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Controllers;
using Mostlylucid.Models.Comments;
using Mostlylucid.Services;
using MarkdownBaseService = Mostlylucid.Blog.MarkdownBaseService;

namespace Mostlylucidblog.Controllers;

[Route("blog")]
public class BlogController(BaseControllerService baseControllerService, 
    IBlogService blogService,
    CommentViewService commentViewService,
    ILogger<BlogController> logger) : BaseController(baseControllerService, logger)
{
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { "page", "pageSize" },
        Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(page), nameof(pageSize) })]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var posts = await blogService.GetPagedPosts(page, pageSize);
        if (Request.IsHtmx()) return PartialView("_BlogSummaryList", posts);
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }

    [Route("{slug}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) }, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) })]
    public async Task<IActionResult> Show(string slug, string language = MarkdownBaseService.EnglishLanguage)
    {
        var post = await blogService.GetPost(slug, language);
        if (post == null) return NotFound();

        var user = await GetUserInfo();
        post.Authenticated = user.LoggedIn;
        post.Name = user.Name;
        post.Email = user.Email;
        post.AvatarUrl = user.AvatarUrl;
        var commentViewList = new CommentViewList
        {
            PostId = int.Parse(post.Id),
            IsAdmin = user.IsAdmin
        };

        if (user.IsAdmin)
            commentViewList.Comments = await commentViewService.GetAllComments(int.Parse(post.Id));
        else
            commentViewList.Comments = await commentViewService.GetApprovedComments(int.Parse(post.Id));

        commentViewList.Comments.ForEach(x => x.IsAdmin = user.IsAdmin);
        post.Comments = commentViewList;
        if (Request.IsHtmx()) return PartialView("_PostPartial", post);
        return View("Post", post);
    }

    [Route("category/{category}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(category), nameof(page), nameof(pageSize) },
        Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(category), nameof(page), nameof(pageSize) })]
    public async Task<IActionResult> Category(string category, int page = 1, int pageSize = 10)
    {
        ViewBag.Category = category;
        var posts = await blogService.GetPostsByCategory(category, page, pageSize);
        var user = await GetUserInfo();
        posts.Authenticated = user.LoggedIn;
        posts.Name = user.Name;
        posts.AvatarUrl = user.AvatarUrl;
        posts.LinkUrl = Url.Action("Category", "Blog");
        ViewBag.Title = category + " - Blog";
        if (Request.IsHtmx()) return PartialView("_BlogSummaryList", posts);
        return View("Index", posts);
    }

    [Route("language/{slug}/{language}")]
    [HttpGet]
    public async Task<IActionResult> Compat(string slug, string language)
    {
        return await Show(slug, language);
    }


    [Route("{language}/{slug}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) }, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) })]
    public async Task<IActionResult> Language(string slug, string language)
    {
        return await Show(slug, language);
    }
}