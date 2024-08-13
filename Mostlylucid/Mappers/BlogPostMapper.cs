using Mostlylucid.EntityFramework.Models;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Mappers;

public static class BlogPostMapper
{
    public static  BlogPostViewModel ToPostModel(this BlogPost post, List<string> languages)
    {
        return new BlogPostViewModel()
        {


            Categories = post.Categories.Select(x => x.Name).ToArray(),
            Title = post.Title,
            HtmlContent = post.HtmlContent,
            PlainTextContent = post.PlainTextContent,
            Slug = post.Slug,
            Language = post.Language.Name,
            WordCount = post.WordCount,
            Languages = languages.ToArray(),
            PublishedDate = post.PublishedDate.DateTime
        };
    }
    
    public static  PostListModel ToListModel(this BlogPost post, string[]? languages )
    {
        return new PostListModel()
        {
            Categories = post.Categories.Select(x => x.Name).ToArray(),
            Title = post.Title,
            Summary = post.PlainTextContent.TruncateAtWord(200) + "...",
            Slug = post.Slug,
            Language = post.Language.Name,
            Languages = languages ?? [],
            WordCount = post.WordCount,
            PublishedDate = post.PublishedDate.DateTime
        };
    }
}