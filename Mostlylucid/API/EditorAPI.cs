using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog;
using Umami.Net;

namespace Mostlylucid.API;

[Route("api/editor")]
[ApiController]
public class Editor(MarkdownRenderingService markdownBlogService, UmamiClient umamiClient) : ControllerBase
{
    [HttpPost]
    [Route("getcontent")]
    public IActionResult GetContent([FromBody] ContentModel model)
    {
        var content = model.Content.Replace("\n", Environment.NewLine);
        var blogPost = markdownBlogService.GetPageFromMarkdown(content, DateTime.Now, "");
        return Ok(blogPost); // Use Ok() for proper JSON responses
    }

    public class ContentModel
    {
        public string Content { get; set; }
    }
}