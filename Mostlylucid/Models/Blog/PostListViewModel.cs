using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models.Blog;

public class PostListViewModel : BaseViewModel
{
    public string LinkUrl { get; set; }
    public int Page { get; set; }
    
    public int TotalItems { get; set; }
    
    public int PageSize { get; set; }
    public List<PostListModel> Posts { get; set; }
}