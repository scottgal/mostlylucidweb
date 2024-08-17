using Markdig;
using Mostlylucid.Helpers;
using Mostlylucid.MarkDigExtensions;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog;

public class MarkdownBaseService
{
    public const string EnglishLanguage = "en";
    protected   MarkdownPipeline Pipeline() =>  new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseTableOfContent()
        .Use<ImgExtension>()
        .Build();



    protected PostListModel GetListModel(BlogPostViewModel model)
    {
        return new PostListModel
        {
            Title = model.Title,
            PublishedDate = model.PublishedDate,
            Slug = model.Slug,
            WordCount = model.WordCount,
            Language = model.Language,
            Categories = model.Categories,
            Summary = model.PlainTextContent.TruncateAtWord(200) + "...",
            Languages = model.Languages
        };
    }
}