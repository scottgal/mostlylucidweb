using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Shared.Mapper;

public static class BlogPostEntityMapper
{

    public static BlogPostDto ToDto(this BlogPostEntity entity, string[] languages = null)
    {
        return new BlogPostDto
        {
            Id = entity.Id.ToString(),
            Title = entity.Title,
            Language = entity.LanguageEntity.Name,
            Markdown = entity.Markdown,
            UpdatedDate = entity.UpdatedDate.DateTime,
            HtmlContent = entity.HtmlContent,
            PlainTextContent = entity.PlainTextContent,
            Slug = entity.Slug,
            WordCount = entity.WordCount,
            PublishedDate = entity.PublishedDate.DateTime,
            Languages = languages ?? Array.Empty<string>()
        };
    }
}