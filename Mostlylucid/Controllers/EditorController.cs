using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Models.Editor;

namespace Mostlylucid.Controllers;

[Route("editor")]
public class EditorController(
    IBlogService blogService,
    AuthSettings authSettings,
    AnalyticsSettings analyticsSettings,
    TranslateServiceConfig translateServiceConfig,
    ILogger<EditorController> logger) : BaseController(authSettings,
    analyticsSettings, blogService, logger)
{
    private string GetUserId()
    {
        var userId = Request.Cookies["UserIdentifier"];
        if (userId == null)
        {
            userId = Guid.NewGuid().ToString();
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(60),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("UserIdentifier", userId, cookieOptions);
        }

        return userId;
    }
    
    [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {
        var userInRole = GetUserInfo();
        var editorModel = new EditorModel();
        editorModel.Languages= translateServiceConfig.Languages.ToList();
        editorModel.Name = userInRole.Name;
        editorModel.Authenticated = userInRole.LoggedIn;
       editorModel.IsAdmin = userInRole.IsAdmin;
        editorModel.AvatarUrl = userInRole.AvatarUrl;

        GetUserId();
        
     
        
        if (slug == null)
        {
            return View("Editor", editorModel);
        }

        var blogPost = await blogService.GetPost(slug, language);
        if (blogPost == null)
        {
            return NotFound();
        }

        editorModel.Markdown = blogPost.OriginalMarkdown;
        editorModel.PostViewModel = blogPost;
        return View("Editor", editorModel);
    }
}