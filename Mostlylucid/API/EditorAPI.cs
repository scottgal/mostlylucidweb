using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services.Markdown;
using Umami.Net.Models;

namespace Mostlylucid.API;

[Route("api/editor")]
[ApiController]
public class Editor(MarkdownRenderingService markdownBlogService, UmamiClient umamiClient) : ControllerBase
{
    [HttpPost]
    [Route("getcontent")]
    public async Task<IActionResult> GetContent([FromBody] ContentModel model)
    {
        Request.Cookies.TryGetValue("UserIdentifier", out var userId);
        await umamiClient.Send( new UmamiPayload(){Url = "api/editor/getcontent", Referrer = Request.Headers["Referer"]});
        var blogPost = markdownBlogService.GetPageFromMarkdown(model.Content, DateTime.Now, "");
        return Ok(blogPost); // Use Ok() for proper JSON responses
    }

    public class ContentModel
    {
        public string Content { get; set; }
    }
}