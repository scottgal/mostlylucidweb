using Mostlylucid.Mapper;
using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Blog;
using Mostlylucid.Shared.Models;
using CategoryWithCount = Mostlylucid.Shared.Models.CategoryWithCount;
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

    public async Task<List<CategoryWithCount>> GetCategoriesWithCount(string language = Constants.EnglishLanguage)
    {
        return await blogPostService.GetCategoriesWithCount(language);
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
        var queryModel = new PostListQueryModel(language,Categories: new []{category},Page:page,PageSize:pageSize);
      return await GetListPostsViewModel(queryModel);
    }

    public async Task<BlogPostViewModel?> GetPost(string slug, string language = "")
    {
        var queryModel = new BlogPostQueryModel(slug,language);
        var post =await blogPostService.GetPost(queryModel);
        return post?.ToViewModel() ?? null;
    }

    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = Constants.EnglishLanguage,
        DateTime? startDate = null, DateTime? endDate = null , string? orderBy =null , string? orderDir = null)
    {
        var queryModel = new PostListQueryModel(Page:page,PageSize:pageSize, Language:language, StartDate:startDate, EndDate:endDate, orderBy:orderBy, orderDir:orderDir);
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

    public async Task<(string Slug, string Language)> GetSlugAndLanguage(int id)
    {
        return await blogPostService.GetSlugAndLanguage(id);
    }

    public async Task<List<PostListModel>> GetPostsBySlugAsync(List<string> slugs, string language)
    {
        var posts = await blogPostService.GetPostsBySlugsAsync(slugs, language);
        return posts.Select(p => p.ToPostListModel()).ToList();
    }

    public async Task<BlogIndexResult> GetIndexDataAsync(BlogIndexRequest request)
    {
        var (page, pageSize, startDate, endDate, language, orderBy, orderDir, order, category) = request;

        // Support combined order parameter (e.g., "date_desc") for simpler HTMX forms
        if (!string.IsNullOrEmpty(order) && order.Contains('_'))
        {
            var parts = order.Split('_', 2);
            orderBy = parts[0];
            orderDir = parts.Length > 1 ? parts[1] : "desc";
        }

        var posts = await GetListPostsViewModel(new PostListQueryModel(
            Page: page, PageSize: pageSize, Language: language,
            Categories: string.IsNullOrEmpty(category) ? null : new[] { category },
            StartDate: startDate, EndDate: endDate, orderBy: orderBy, orderDir: orderDir));

        var result = new BlogIndexResult { Posts = posts };

        // Get all categories for the filter dropdown
        posts.AllCategories = await GetCategoriesWithCount(language);

        return result;
    }

    public async Task<List<string>> GetCalendarDaysAsync(int year, int month, string language)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var posts = await GetPostsForRange(start, end, language: language);

        if (posts is null) return new List<string>();

        return posts
            .Select(p => p.PublishedDate.Date)
            .Distinct()
            .OrderBy(d => d)
            .Select(d => d.ToString("yyyy-MM-dd"))
            .ToList();
    }

    public async Task<DateRangeResult> GetDateRangeAsync(string language)
    {
        var allPosts = await GetAllPosts();

        if (allPosts is null || !allPosts.Any())
        {
            return new DateRangeResult(
                DateTime.UtcNow.AddYears(-1).ToString("yyyy-MM-dd"),
                DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        // Filter by language if specified
        var posts = allPosts;
        if (!string.IsNullOrEmpty(language) && language != Constants.EnglishLanguage)
        {
            posts = allPosts.Where(p => p.Language.Equals(language, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // If no posts in that language, fall back to all posts
        if (!posts.Any())
        {
            posts = allPosts;
        }

        var minDate = posts.Min(p => p.PublishedDate.Date);
        var maxDate = posts.Max(p => p.PublishedDate.Date);

        return new DateRangeResult(
            minDate.ToString("yyyy-MM-dd"),
            maxDate.ToString("yyyy-MM-dd"));
    }

    public string NormalizeSlug(string slug)
    {
        if (slug.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            slug = slug[..^3];

        return slug.Replace('_', '-').Replace(' ', '-').ToLowerInvariant();
    }
}
