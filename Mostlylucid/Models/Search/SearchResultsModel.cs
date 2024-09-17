using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models.Search;

public class SearchResultsModel : BaseViewModel
{
    public string Query { get; set; }
    public PostListViewModel SearchResults { get; set; }
}