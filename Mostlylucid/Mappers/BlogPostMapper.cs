using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Mappers;

public static class BlogPostMapper
{
    public static BlogPostViewModel ToPostModel(this BlogPostEntity postEntity, List<string>? languages = null)
    {
        var wordCount = postEntity.PlainTextContent.WordCount();
        return new BlogPostViewModel()
        {
            Id = postEntity.Id.ToString(),
            Categories = postEntity.Categories.Select(x => x.Name).ToArray(),
            Title = postEntity.Title,
            HtmlContent = postEntity.HtmlContent,
            PlainTextContent = postEntity.PlainTextContent,
            Slug = postEntity.Slug,
            Language = postEntity.LanguageEntity.Name,
            WordCount = wordCount,
            UpdatedDate = postEntity.UpdatedDate.DateTime,
            Languages = languages?.OrderBy(x => x).ToArray() ?? Array.Empty<string>(),
            Markdown = postEntity.Markdown,
            PublishedDate = postEntity.PublishedDate.DateTime
        };
    }

    public static PostListModel ToListModel(this BlogPostEntity postEntity, string[]? languages)
    {
        var introductionText = "Introduction\n";
        var summaryText = postEntity.PlainTextContent;
        var wordCount = summaryText.WordCount();
        
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
            WordCount = wordCount,
            PublishedDate = postEntity.PublishedDate.DateTime
        };
    }
}