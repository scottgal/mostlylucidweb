using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Mappers;

public static class BlogPostMapper
{
    public static BlogPostViewModel ToPostModel(this BlogPostEntity postEntity, List<string> languages)
    {
        return new BlogPostViewModel()
        {
            Categories = postEntity.Categories.Select(x => x.Name).ToArray(),
            Title = postEntity.Title,
            HtmlContent = postEntity.HtmlContent,
            PlainTextContent = postEntity.PlainTextContent,
            Slug = postEntity.Slug,
            Language = postEntity.LanguageEntity.Name,
            WordCount = postEntity.WordCount,
            Languages = languages.OrderBy(x => x).ToArray(),
            OriginalMarkdown = postEntity.OriginalMarkdown,
            PublishedDate = postEntity.PublishedDate.DateTime
        };
    }

    public static PostListModel ToListModel(this BlogPostEntity postEntity, string[]? languages)
    {
        var introductionText = "Introduction\n";
        var summaryText = postEntity.PlainTextContent;
        if (summaryText.StartsWith(introductionText, StringComparison.OrdinalIgnoreCase))
        {
            summaryText = summaryText.Substring(introductionText.Length);
        }

        summaryText = summaryText.TruncateAtWord(200) + "...";
        return new PostListModel()
        {
            Categories = postEntity.Categories.Select(x => x.Name).ToArray(),
            Title = postEntity.Title,
            Summary = summaryText,
            Slug = postEntity.Slug,
            Language = postEntity.LanguageEntity.Name,
            Languages = languages ?? [],
            WordCount = postEntity.WordCount,
            PublishedDate = postEntity.PublishedDate.DateTime
        };
    }
}