using Markdig;
using Mostlylucid.Helpers;
using Mostlylucid.MarkDigExtensions;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services;

public class BaseService
{
    protected  readonly MarkdownPipeline _pipeline;
    protected  BaseService()
    {   
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseTableOfContent().Use<ImgExtension>()
            .Build();
    }
    protected PostListModel GetListModel(BlogPostViewModel model)
    {
        return new PostListModel
        {
            Title = model.Title,
            PublishedDate = model.PublishedDate,
            Slug = model.Slug,
            Categories = model.Categories,
            Summary = model.PlainTextContent.TruncateAtWord(200) + "...",
            Languages = model.Languages
        };
    }
}