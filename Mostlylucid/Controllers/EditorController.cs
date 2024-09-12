using Microsoft.AspNetCore.Mvc;
using Mostlylucid.MarkdownTranslator.Models;
using Mostlylucid.Models.Editor;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

[Route("editor")]
public class EditorController(
    BaseControllerService baseControllerService,
    TranslateCacheService translateCacheService,
    TranslateServiceConfig translateServiceConfig,
    ILogger<EditorController> logger) : BaseController(baseControllerService, logger)
{
    [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {
        var userId = UserId;
        var tasks = translateCacheService.GetTasks(userId);
        var translations = tasks.Select(x => new TranslateResultTask(x)).ToList();
        var userInRole = await GetUserInfo();
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

        var blogPost = await BlogService.GetPost(slug, language);
        if (blogPost == null) return NotFound();

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
        var translations = tasks.Select(x => new TranslateResultTask(x)).ToList();
        return PartialView("_GetTranslations", translations);
    }
}