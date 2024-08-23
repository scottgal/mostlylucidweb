using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Blog;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Models.Blog;
using Mostlylucid.Models.Editor;

namespace Mostlylucid.Controllers;

[Route("editor")]
public class EditorController(
    IBlogService blogService,
    AuthSettings authSettings,
    AnalyticsSettings analyticsSettings,
    TranslateCacheService translateCacheService,
    BackgroundTranslateService backgroundTranslateService,
    TranslateServiceConfig translateServiceConfig,
    ILogger<EditorController> logger) : BaseController(authSettings,
    analyticsSettings, blogService, logger)
{

    
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

        editorModel.UserSessionId = Request.GetUserId(Response);
        if (slug == null)
        {
            return View("Editor", editorModel);
        }

        var blogPost = await blogService.GetPost(slug, language);
        if (blogPost == null)
        {
            return NotFound();
        }

        editorModel.Markdown = blogPost.Markdown;
        editorModel.PostViewModel = blogPost;
        return View("Editor", editorModel);
    }

    [HttpGet]
    [Route("get-translations")]
    public IActionResult GetTranslations()
    {
        var userId = Request.GetUserId(Response);
        var tasks = translateCacheService.GetTasks(userId);
        var translations = tasks;
        return PartialView("_GetTranslations", translations);
    }
    


}