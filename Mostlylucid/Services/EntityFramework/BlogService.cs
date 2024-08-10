using Mostlylucid.EntityFramework;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services.EntityFramework;

public class BlogService(MostlylucidDBContext context, IMarkdownBlogService markdownBlogService, ILogger<BlogService> logger): IBlogService
{
    
    public Task<List<string>> GetCategories()
    {
        throw new NotImplementedException();
    }

    public Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        throw new NotImplementedException();
    }

    public Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10)
    {
        throw new NotImplementedException();
    }

    public Task<BlogPostViewModel?> GetPost(string postName, string language = "")
    {
        throw new NotImplementedException();
    }

    public Task<PostListViewModel> GetPostsForFiles(int page = 1, int pageSize = 10)
    {
        throw new NotImplementedException();
    }
}