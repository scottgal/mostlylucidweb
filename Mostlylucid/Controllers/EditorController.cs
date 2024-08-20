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
    ILogger<EditorController> logger) : BaseController(authSettings,
    analyticsSettings, blogService, logger)
{
    [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {
        if (slug == null)
        {
            return View("Editor", new EditorModel());
        }

        var blogPost = await blogService.GetPost(slug, language);
        if (blogPost == null)
        {
            return NotFound();
        }

        var model = new EditorModel { Markdown = blogPost.OriginalMarkdown, PostViewModel = blogPost };
        return View("Editor", model);
    }
}