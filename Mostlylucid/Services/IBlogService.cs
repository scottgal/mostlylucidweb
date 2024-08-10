using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services;

public interface IBlogService
{
   Task<List<string>> GetCategories();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10);
    Task<BlogPostViewModel?> GetPost(string postName, string language = "");
    Task<PostListViewModel> GetPostsForFiles(int page = 1, int pageSize = 10);
}

public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    
    
}