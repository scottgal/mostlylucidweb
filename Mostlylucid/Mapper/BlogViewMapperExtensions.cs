using Mostlylucid.Models.Blog;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Mapper;

public static class BlogViewMapperExtensions
{
    public static PostListModel ToPostListModel(this BlogPostDto dto)
    {
        return new PostListModel
        {
            Id = dto.Id,
            Summary = GetSummary(dto.PlainTextContent),
            Title = dto.Title,
            Categories = dto.Categories,
            Language = dto.Language,
            UpdatedDate = dto.UpdatedDate?.DateTime,
            Slug = dto.Slug,
            WordCount = dto.WordCount,
            PublishedDate = dto.PublishedDate,
            Languages = dto.Languages
        };
    }
    
    public static BlogPostViewModel ToViewModel(this BlogPostDto dto)
    {
        var wordCount = dto.PlainTextContent.WordCount();
        return new BlogPostViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Language = dto.Language,
            Categories = dto.Categories,
            Markdown = dto.Markdown,
            UpdatedDate = dto.UpdatedDate?.DateTime,
            HtmlContent = dto.HtmlContent,
            PlainTextContent = dto.PlainTextContent,
            Slug = dto.Slug,
            WordCount = wordCount,
            PublishedDate = dto.PublishedDate,
            Languages = dto.Languages,
            ShowUpdatedDate = dto.ShowUpdatedDate,
            UpdatedTemplate = dto.UpdatedTemplate
        };
    }
    
    public static PostListViewModel ToPostListViewModel(this BasePagingModel<BlogPostDto> postEntity)
    {
        return new PostListViewModel
        {
            Data = postEntity.Data?.Select(x => x.ToListModel(x.Languages)).ToList() ?? new List<PostListModel>(),
            TotalItems = postEntity.TotalItems,
            NoMatchFound = postEntity.NoMatchFound,
            Page = postEntity.Page,
            PageSize = postEntity.PageSize
        };
    }
    

    public static PostListModel ToListModel(this BlogPostDto postEntity, string[]? languages = null)
    {
        var summaryText = GetSummary(postEntity.PlainTextContent);
        var wordCount = postEntity.PlainTextContent.WordCount();
        return new PostListModel()
        {
            Categories = postEntity.Categories,
            Title = postEntity.Title,
            Summary = summaryText,
            Slug = postEntity.Slug,
            Language = postEntity.Language,
            Languages = languages ?? [],
            WordCount = wordCount,
            PublishedDate = postEntity.PublishedDate
        };
    }

    private static string GetSummary(string text)
    {
        var summary = string.Join(" ", (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (summary.StartsWith("Introduction ", StringComparison.OrdinalIgnoreCase))
            summary = summary["Introduction ".Length..];
        return summary.Length > 260 ? summary.TruncateAtWord(260) + "…" : summary;
    }
}
