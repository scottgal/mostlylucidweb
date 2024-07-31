namespace Mostlylucid.Models.Blog;

public class BlogPostViewModel
{
    public string[] Categories { get; set; } = Array.Empty<string>();
    
    public string Title { get; set; }= string.Empty;
    
    public string Content { get; set; }= string.Empty;
    
    public string Slug { get; set; }= string.Empty;
    
    public DateTime UpdatedDate { get; set; }
    
    public DateTime CreatedDate { get; set; }
}