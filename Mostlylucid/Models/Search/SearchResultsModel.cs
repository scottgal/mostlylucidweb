using Mostlylucid.Models.Blog;
using Mostlylucid.Shared.Entities;

namespace Mostlylucid.Models.Search;

public class SearchResultsModel : BaseViewModel
{
    public string? Query { get; set; }
    public PostListViewModel SearchResults { get; set; }
}