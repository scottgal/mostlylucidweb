using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Blog;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.MarkdownTranslator.Models;
using Mostlylucid.Models.Blog;
using Mostlylucid.Models.Editor;

namespace Mostlylucid.Controllers;

[Route("editor")]
public class EditorController(
    IBlogService blogService,
    AuthSettings authSettings,
    AnalyticsSettings analyticsSettings,
    TranslateCacheService translateCacheService,
    TranslateServiceConfig translateServiceConfig,
    ILogger<EditorController> logger) : BaseController(authSettings,
    analyticsSettings, blogService, logger)
{

    
    [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {

        var userId = UserId;
        var tasks = translateCacheService.GetTasks(userId);
        var translations = tasks.Select(x=> new TranslateResultTask(x, false)).ToList();
        var userInRole =await GetUserInfo();
        var editorModel = new EditorModel
        {
            Languages = translateServiceConfig.Languages.ToList(),
            Name = userInRole.Name,
            Authenticated = userInRole.LoggedIn,
            IsAdmin = userInRole.IsAdmin,
            AvatarUrl = userInRole.AvatarUrl,
            UserSessionId = UserId,
            TranslationTasks = translations
        };
        if (slug == null)
        {
            editorModel.IsNew = true;
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
        var userId = UserId;
        var tasks = translateCacheService.GetTasks(userId);
        var translations = tasks.Select(x=> new TranslateResultTask(x, false)).ToList();
        return PartialView("_GetTranslations", translations);
    }
    


}