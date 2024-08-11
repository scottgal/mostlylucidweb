using Mostlylucid.Models.Blog;

namespace Mostlylucid.Services;

public interface IBlogService
{
   Task<List<string>> GetCategories();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10);
    Task<BlogPostViewModel?> GetPost(string postName, string language = "");
    Task<PostListViewModel> GetPosts(int page = 1, int pageSize = 10);

    Task Populate();
}

public interface IMarkdownBlogService
{
    Task<List<BlogPostViewModel>> GetPages();
    
    Dictionary<string, List<String>> LanguageList();

    Task<BlogPostViewModel?> GetPost(string postName, string language = "");


}