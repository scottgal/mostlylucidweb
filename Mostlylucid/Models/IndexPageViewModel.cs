using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models;

public class IndexPageViewModel : BaseViewModel
{
   

   
   public List<string> Categories { get; set; }
   public PostListViewModel Posts { get; set; }
}