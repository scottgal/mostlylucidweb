using Mostlylucid.Mapper;
using Mostlylucid.Mappers;
using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Blog;
using Mostlylucid.Shared.Models;
using Constants = Mostlylucid.Shared.Constants;

namespace Mostlylucid.Blog.ViewServices;

public class BlogPostViewService(IBlogService blogPostService) : IBlogViewService
{
    public async Task<bool> EntryChanged(string slug, string language, string hash)
    {
       return await blogPostService.EntryChanged(slug, language, hash);
    }

    public async Task<bool> EntryExists(string slug, string language)
    {
       return await blogPostService.EntryExists(slug, language);
    }

    public async Task<BlogPostViewModel> SavePost(string slug, string language, string markdown)
    {
        var dto= await blogPostService.SavePost(slug, language, markdown);
        return dto.ToViewModel();
    }

    public async Task<List<string>> GetCategories(bool noTracking = false)
    {
        return await blogPostService.GetCategories(noTracking);
    }

    private async Task<List<BlogPostViewModel>> GetPosts(PostListQueryModel model)
    {
        var posts =await blogPostService.Get(model);
        return posts?.Data == null ? new List<BlogPostViewModel>() : posts.Data.Select(x => x.ToViewModel()).ToList();
    }
    
    private async Task<List<PostListModel>> GetListPosts(PostListQueryModel model)
    {
        var posts =await blogPostService.Get(model);
        if(posts?.Data == null) return new List<PostListModel>();
        return posts.Data.Select(x => x.ToPostListModel()).ToList();
    }
    
    private async Task<PostListViewModel> GetListPostsViewModel(PostListQueryModel model)
    {
        var posts =await blogPostService.Get(model);
        if(posts?.Data == null) return new PostListViewModel();
        return posts.ToPostListViewModel();
    }

    public async Task<List<BlogPostViewModel>> GetAllPosts()
    {
        var queryModel = new PostListQueryModel();
        return await GetPosts(queryModel);
  
    }

    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
       var queryModel = new PostListQueryModel(StartDate:startDate,Categories: new []{category} );
        return await GetPosts(queryModel);
       
    }

    public async Task<List<PostListModel>> GetPostsForRange(DateTime? startDate = null, DateTime? endDate = null, string[]? categories = null,
        string language = Constants.EnglishLanguage)
    {
       var queryModel = new PostListQueryModel(StartDate:startDate,EndDate:endDate,Categories: categories,Language:language);
        return await GetListPosts(queryModel);
    }

    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language =Constants.EnglishLanguage)
    {
        var queryModel = new PostListQueryModel(language,Categories: new []{category});
      return await GetListPostsViewModel(queryModel);
    }

    public async Task<BlogPostViewModel?> GetPost(string slug, string language = "")
    {
        var queryModel = new BlogPostQueryModel(slug,language);
        var post =await blogPostService.GetPost(queryModel);
        return post?.ToViewModel() ?? null;
    }

    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = Constants.EnglishLanguage)
    {
        var queryModel = new PostListQueryModel(Page:page,PageSize:pageSize, Language:language);
        return await GetListPostsViewModel(queryModel);
    }

    public Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = Constants.EnglishLanguage)
    {
       var queryModel = new PostListQueryModel(StartDate:startDate,Categories: new []{category},Language:language);
        return GetListPosts(queryModel);
    }


    public async Task<bool> Delete(string slug, string language)
    {
        return await blogPostService.Delete(slug, language);
    }

    public  async Task<string> GetSlug(int id)
    {
        return await blogPostService.GetSlug(id);
    }
}