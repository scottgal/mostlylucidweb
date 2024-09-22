using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Mapper;
using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBlogViewService(MarkdownConfig config, ILogger<MarkdownBlogViewService> logger) : IBlogViewService
{
    protected MarkdownConfig MarkdownConfig => config;
        

    

    public async Task<List<string>> GetCategories(bool noTracking = false)
    {
        var pages = PageCacheHelper.GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return await Task.FromResult(categories);
    }


    public Task<bool> Delete(string slug, string language)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetSlug(int postId)
    {
        throw new NotImplementedException();
    }
    public Task<List<BlogPostViewModel>> GetAllPosts()
    {
        var posts = PageCacheHelper.GetPageCache().Select(x => x.Value.ToViewModel()).ToList();
        return Task.FromResult(posts);
    }

    public async Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "",
        string language = Constants.EnglishLanguage)
    {
        var pageCache = PageCacheHelper.GetPageCache().Select(x => x.Value).Where(x => x.Language == language);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);

        return await Task.FromResult(pageCache.Select(x => x.ToPostListModel()).ToList());
    }

    public async Task<bool> EntryExists(string slug, string language)
    {
        var file = Path.Combine(MarkdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
        return await Task.FromResult(File.Exists(file));
    }

    public async Task<bool> EntryChanged(string slug, string language, string hash)
    {
        string fileName = "";
        var originalFileName = Path.Combine(MarkdownConfig.MarkdownPath, slug + ".md");
        var fileChanged = await originalFileName.IsFileChanged(MarkdownConfig.MarkdownTranslatedPath, language);
        return fileChanged;
    }

    public async Task<BlogPostViewModel> SavePost(string slug, string language, string markdown)
    {
        try
        {
            var outPath = Path.Combine(MarkdownConfig.MarkdownPath, slug + ".md");
            if (language != Constants.EnglishLanguage)
                outPath = Path.Combine(MarkdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
            await File.WriteAllTextAsync(outPath, markdown);
            return await GetPost(slug, language) ?? new BlogPostViewModel();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving post {PostName}", slug);
            return new BlogPostViewModel();
        }
    }
    
    public async Task<BlogPostViewModel> SavePost(BlogPostDto model)
    {
        await SavePost(model.Slug, model.Language, model.Markdown);
        return model.ToViewModel();
    }


    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        var pageCache = PageCacheHelper.GetPageCache().Select(x => x.Value);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);
var result = pageCache.Select(x => x.ToViewModel()).ToList();
        
        return await Task.FromResult(result);
    }

    public Task<List<PostListModel>> GetPostsForRange(DateTime? startDate = null, DateTime? endDate = null, string[]? categories = null,
        string language = Services.Markdown.MarkdownBaseService.EnglishLanguage)
    {
        throw new NotImplementedException();
    }


    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language =Constants.EnglishLanguage)
    {
        var postsQuery = PageCacheHelper.GetPageCache()
            .Where(x => x.Key.lang == language && x.Value.Categories.Contains(category))
            .Select(x => x.Value.ToPostListModel())
            .OrderByDescending(x => x.PublishedDate).ToList();

        var totalItems = postsQuery.Count();

        var posts = postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new PostListViewModel
        {
            Data = posts,
            TotalItems = totalItems,
            PageSize = pageSize,
            Page = page
        };

        return await Task.FromResult(model);
    }


    public async Task<BlogPostViewModel?> GetPost(string slug, string language = Constants.EnglishLanguage)
    {
        try
        {
            // Attempt to retrieve from the cache first
            var pageCache = PageCacheHelper.GetPageCache();
            if (pageCache.TryGetValue((slug, language), out var pageModel)) return await Task.FromResult(pageModel.ToViewModel());

            return null;
        }
        catch (Exception ex)
        {
            // Log the error and return null
            logger.LogError(ex, "Error getting post {PostName}", slug);
            return null;
        }
    }


    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10,
        string language = Constants.EnglishLanguage)
    {
        var model = new PostListViewModel();
        var posts = PageCacheHelper.GetPageCache().Where(x => x.Value.Language == language)
            .Select(x =>(x.Value.ToPostListModel())).ToList();
        model.Data = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return await Task.FromResult(model);
    }
}