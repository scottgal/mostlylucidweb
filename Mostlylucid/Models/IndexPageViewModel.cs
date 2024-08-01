using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models;

public class IndexPageViewModel
{
   
   public List<string> Categories { get; set; }
   public List<PostListModel> Posts { get; set; }
}