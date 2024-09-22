using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Blog;

public interface IBlogService
{
    Task<BasePagingModel<BlogPostDto>?> Get(PostListQueryModel model);
    Task<string> GetSlug(int postId);
    Task<bool> EntryExists(string slug, string language);
    Task<bool> EntryChanged(string slug, string language, string hash);
    Task<BlogPostDto> SavePost(string slug, string language, string markdown);
    Task<BlogPostDto> SavePost(BlogPostDto model);
    Task<bool> Delete(string slug, string language);
    Task<BlogPostDto?> GetPost(BlogPostQueryModel model);
    Task<List<string>> GetCategories(bool noTracking = false);
}