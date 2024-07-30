namespace Mostlylucid.Models.Blog;

public class PostListModel
{
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
    public DateTimeOffset Date { get; set; }
    
    public string Summary { get; set; }
}