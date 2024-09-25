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
            WordCount = dto.WordCount,
            PublishedDate = dto.PublishedDate,
            Languages = dto.Languages
        };
    }
    
    public static PostListViewModel ToPostListViewModel(this BasePagingModel<BlogPostDto> postEntity)
    {
        return new PostListViewModel
        {
            Data = postEntity.Data.Select(x => x.ToListModel(x.Languages)).ToList(),
            TotalItems = postEntity.TotalItems,
            Page = postEntity.Page,
            PageSize = postEntity.PageSize
        };
    }
    

    public static PostListModel ToListModel(this BlogPostDto postEntity, string[]? languages = null)
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
}