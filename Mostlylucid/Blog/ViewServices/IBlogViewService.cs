using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Blog.ViewServices;

public interface IBlogViewService : IMarkdownFileBlogService
{
   Task<List<string>> GetCategories(bool noTracking = false);
   
   Task<List<BlogPostViewModel>> GetAllPosts();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    
    Task<List<PostListModel>> GetPostsForRange(DateTime? startDate = null, DateTime? endDate = null, 
        string[]? categories=null, string language = MarkdownBaseService.EnglishLanguage);
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    Task<BlogPostViewModel?> GetPost(string slug, string language = "");
    Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = MarkdownBaseService.EnglishLanguage);
    
    
    Task<bool> Delete(string slug, string language);
    Task<string> GetSlug(int id);

}

public interface IMarkdownFileBlogService
{
    Task<bool> EntryChanged(string slug, string language, string hash);
    Task<bool> EntryExists(string slug, string language);
    Task<BlogPostViewModel> SavePost(string slug, string language,  string markdown);
      
}