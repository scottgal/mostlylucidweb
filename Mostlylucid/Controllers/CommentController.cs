using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog;
using Mostlylucid.Blog.Markdown;
using Mostlylucid.Config;

namespace Mostlylucid.Controllers;

public class CommentController(AuthSettings authSettings,CommentService  commentService,  AnalyticsSettings analyticsSettings,IBlogService blogService, ILogger<CommentController> logger)
    : BaseController(authSettings, analyticsSettings, blogService, logger)
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    [Route("comment")]
    [Authorize]
    public async Task<IActionResult> Comment(string slug, string comment)
    {
    var userInformation = GetUserInfo();
        await commentService.AddComment(slug, userInformation, comment);
        return PartialView();
    }

    [HttpGet]
    [Route("list-comments")]
    public async Task<IActionResult> ListComments(string slug)
    {
        return View();
    }
}