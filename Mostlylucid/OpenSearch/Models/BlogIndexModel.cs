using Mostlylucid.Models.Blog;

namespace Mostlylucid.OpenSearch.Models;

public class BlogIndexModel
{
    public BlogIndexModel() { }
    public BlogIndexModel(BlogPostViewModel post)
    {
        
        Language = post.Language;
        Title = post.Title;
        LastUpdated = post.UpdatedDate.DateTime;
        Published = post.PublishedDate;
        Categories = post.Categories.ToList();
        Content = post.PlainTextContent;
    }
    public string Language { get; set; }
    
    public string Title { get; set; }
    
    public DateTime LastUpdated { get; set; }
    
    public DateTime Published { get; set; }
    
    public List<string> Categories { get; set; }
    
    public string Content { get; set; }
    
}