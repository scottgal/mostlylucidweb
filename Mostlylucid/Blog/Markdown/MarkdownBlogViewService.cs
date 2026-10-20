using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Mapper;
using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Config.Markdown;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBlogViewService(MarkdownConfig config, ILogger<MarkdownBlogViewService> logger) : IBlogViewService
{
    protected MarkdownConfig MarkdownConfig => config;

    /// <summary>
    /// Filters posts to exclude hidden posts and posts scheduled for the future
    /// </summary>
    private static IEnumerable<BlogPostDto> ApplyVisibilityFilters(IEnumerable<BlogPostDto> posts)
    {
        var now = DateTimeOffset.UtcNow;
        return posts.Where(x =>
            !x.IsHidden &&
            (x.ScheduledPublishDate == null || x.ScheduledPublishDate <= now));
    }



    public async Task<List<string>> GetCategories(bool noTracking = false)
    {
        var pages = PageCacheHelper.GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return await Task.FromResult(categories);
    }

    public Task<List<CategoryWithCount>> GetCategoriesWithCount(string language = Constants.EnglishLanguage)
    {
        var now = DateTimeOffset.UtcNow;
        var pages = PageCacheHelper.GetPageCache()
            .Where(x => x.Value.Language == language)
            .Select(x => x.Value);

        // Apply visibility filters
        pages = ApplyVisibilityFilters(pages);

        var categoryCounts = pages
            .SelectMany(p => p.Categories)
            .GroupBy(c => c)
            .Select(g => new CategoryWithCount(g.Key, g.Count()))
            .OrderBy(c => c.Name)
            .ToList();

        return Task.FromResult(categoryCounts);
    }


    public Task<bool> Delete(string slug, string language)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetSlug(int postId)
    {
        throw new NotImplementedException();
    }

    public Task<(string Slug, string Language)> GetSlugAndLanguage(int postId)
    {
        throw new NotImplementedException();
    }

    public Task<List<BlogPostViewModel>> GetAllPosts()
    {
        var posts = PageCacheHelper.GetPageCache().Select(x => x.Value);

        // Apply visibility filters
        posts = ApplyVisibilityFilters(posts);

        var result = posts.Select(x => x.ToViewModel()).ToList();
        return Task.FromResult(result);
    }



    public async Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "",
        string language = Constants.EnglishLanguage)
    {
        var pageCache = PageCacheHelper.GetPageCache().Select(x => x.Value).Where(x => x.Language == language);

        // Apply visibility filters
        pageCache = ApplyVisibilityFilters(pageCache);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);

        return await Task.FromResult(pageCache.Select(x => x.ToPostListModel()).ToList());
    }

    public Task<bool> EntryExists(string slug, string language)
    {
        language = string.IsNullOrEmpty(language) ? Constants.EnglishLanguage : language;
        return Task.FromResult(PageCacheHelper.GetPageCache().ContainsKey((slug, language)));
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

        // Apply visibility filters
        pageCache = ApplyVisibilityFilters(pageCache);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);
        var result = pageCache.Select(x => x.ToViewModel()).ToList();

        return await Task.FromResult(result);
    }

    public Task<List<PostListModel>> GetPostsForRange(DateTime? startDate = null, DateTime? endDate = null, string[]? categories = null,
        string language = Services.Markdown.MarkdownBaseService.EnglishLanguage)
    {
        var query = PageCacheHelper.GetPageCache()
            .Select(x => x.Value)
            .Where(p => p.Language == language);

        // Apply visibility filters
        query = ApplyVisibilityFilters(query);

        if (categories != null && categories.Length > 0)
        {
            var categorySet = new HashSet<string>(categories, StringComparer.OrdinalIgnoreCase);
            query = query.Where(p => p.Categories.Any(c => categorySet.Contains(c)));
        }
        if (startDate.HasValue)
            query = query.Where(p => p.PublishedDate.Date >= startDate.Value.Date);
        if (endDate.HasValue)
            query = query.Where(p => p.PublishedDate.Date <= endDate.Value.Date);

        var list = query
            .OrderByDescending(p => p.PublishedDate)
            .Select(p => p.ToPostListModel())
            .ToList();

        return Task.FromResult(list);
    }


    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language =Constants.EnglishLanguage)
    {
        var postsQuery = PageCacheHelper.GetPageCache()
            .Where(x => x.Key.lang == language && x.Value.Categories.Contains(category))
            .Select(x => x.Value);

        // Apply visibility filters
        postsQuery = ApplyVisibilityFilters(postsQuery);

        var postsList = postsQuery.OrderByDescending(x => x.PublishedDate)
            .ThenBy(x => x.Slug).Select(x => x.ToPostListModel()).ToList();

        var totalItems = postsList.Count;

        var posts = postsList
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
        string language = Constants.EnglishLanguage, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = null, string? orderDir = null)
    {
        var model = new PostListViewModel();
        var postsQuery = PageCacheHelper.GetPageCache()
            .Where(x => x.Value.Language == language)
            .Select(x => x.Value);

        // Apply visibility filters (hidden and scheduled)
        postsQuery = ApplyVisibilityFilters(postsQuery);

        if (startDate.HasValue)
            postsQuery = postsQuery.Where(p => p.PublishedDate.Date >= startDate.Value.Date);
        if (endDate.HasValue)
            postsQuery = postsQuery.Where(p => p.PublishedDate.Date <= endDate.Value.Date);

        // Keep one stable order across every page; pins do not override the selected sort.
        postsQuery = (orderBy?.ToLowerInvariant(), orderDir?.ToLowerInvariant()) switch
        {
            ("title", "asc") => postsQuery.OrderBy(x => x.Title).ThenBy(x => x.Slug),
            ("title", _) => postsQuery.OrderByDescending(x => x.Title).ThenBy(x => x.Slug),
            ("date", "asc") => postsQuery.OrderBy(x => x.PublishedDate).ThenBy(x => x.Slug),
            _ => postsQuery.OrderByDescending(x => x.PublishedDate).ThenBy(x => x.Slug)
        };

        var posts = postsQuery.Select(x => x.ToPostListModel()).ToList();
        model.TotalItems = posts.Count;
        model.PageSize = pageSize;
        model.Page = page;
        model.Data = posts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return await Task.FromResult(model);
    }

    public Task<List<PostListModel>> GetPostsBySlugAsync(List<string> slugs, string language)
    {
        var cache = PageCacheHelper.GetPageCache();
        var posts = cache
            .Where(x => slugs.Contains(x.Value.Slug) && x.Value.Language == language)
            .Select(x => x.Value.ToPostListModel())
            .ToList();
        return Task.FromResult(posts);
    }

    public async Task<BlogIndexResult> GetIndexDataAsync(BlogIndexRequest request)
    {
        var orderParts = request.Order?.Split('_', 2);
        var orderBy = orderParts?.Length == 2 ? orderParts[0] : request.OrderBy;
        var orderDir = orderParts?.Length == 2 ? orderParts[1] : request.OrderDir;
        var posts = await GetPagedPosts(1, int.MaxValue, request.Language,
            request.StartDate, request.EndDate, orderBy, orderDir);
        var filtered = posts.Data.Where(post => string.IsNullOrEmpty(request.Category)
            || post.Categories.Contains(request.Category)).ToList();
        posts.Page = Math.Max(1, request.Page);
        posts.PageSize = Math.Clamp(request.PageSize, 1, 100);
        posts.TotalItems = filtered.Count;
        posts.Data = filtered.Skip((posts.Page - 1) * posts.PageSize).Take(posts.PageSize).ToList();
        posts.AllCategories = await GetCategoriesWithCount(request.Language);
        return new BlogIndexResult { Posts = posts };
    }

    public Task<List<string>> GetCalendarDaysAsync(int year, int month, string language)
    {
        throw new NotImplementedException("File mode does not support GetCalendarDaysAsync");
    }

    public Task<DateRangeResult> GetDateRangeAsync(string language)
    {
        throw new NotImplementedException("File mode does not support GetDateRangeAsync");
    }

    public string NormalizeSlug(string slug)
    {
        if (slug.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            slug = slug[..^3];

        return slug.Replace('_', '-').Replace(' ', '-').ToLowerInvariant();
    }
}
