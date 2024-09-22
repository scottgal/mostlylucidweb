using Mostlylucid.Models.Blog;
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
            Language = dto.Language,
            UpdatedDate = dto.UpdatedDate.DateTime,
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
            Markdown = dto.Markdown,
            UpdatedDate = dto.UpdatedDate.DateTime,
            HtmlContent = dto.HtmlContent,
            PlainTextContent = dto.PlainTextContent,
            Slug = dto.Slug,
            WordCount = dto.WordCount,
            PublishedDate = dto.PublishedDate,
            Languages = dto.Languages
        };
    }
    
}