using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models;

public class IndexPageViewModel : BaseViewModel
{
   public PostListViewModel Posts { get; set; } = new();
}