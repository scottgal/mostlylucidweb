using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.OpenSearch.Models;

public class BlogIndexModel
{
    public BlogIndexModel() { }
    public BlogIndexModel(BlogPostDto post)
    {
        Id = string.IsNullOrEmpty(post.Id) ? $"{post.Slug}-{post.Language}" : post.Id;
        Language = post.Language;
        Title = post.Title;
        Hash = post.PlainTextContent.ContentHash();
        LastUpdated = post.UpdatedDate?.DateTime;
        Published = post.PublishedDate;
        Categories = post.Categories.ToList();
        Content = post.PlainTextContent;
        Slug = post.Slug;
    }
    
    public string Id { get; set; }
    public string Language { get; set; }
    
    public string Slug { get; set; }
    public string Title { get; set; }
    
    public DateTime? LastUpdated { get; set; }
    
    public DateTime Published { get; set; }
    
    
    public string Hash { get; set; }
    public List<string> Categories { get; set; }
    
    public string Content { get; set; }
    
}