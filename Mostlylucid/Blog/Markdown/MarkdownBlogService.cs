using Mostlylucid.Config.Markdown;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBlogService : MarkdownBaseService, IBlogService, IMarkdownFileBlogService
{
    private readonly ILogger<MarkdownBlogService> _logger;

    public MarkdownBlogService(MarkdownConfig markdownConfig, ILogger<MarkdownBlogService> logger) : base(
        markdownConfig)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetCategories()
    {
        var pages = GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return await Task.FromResult(categories);
    }

    public async Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "",
        string language = EnglishLanguage)
    {
        var pageCache = GetPageCache().Select(x => x.Value).Where(x => x.Language == language);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);

        return await Task.FromResult(pageCache.Select(x => GetListModel(x)).ToList());
    }

    public async Task<bool> EntryExists(string slug, string language)
    {
        var file = Path.Combine(MarkdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
        return await Task.FromResult(File.Exists(file));
    }

    public async Task<bool> EntryChanged(string slug, string language, string hash)
    {
        string fileName = "";
        var originalFileName = fileName = Path.Combine(MarkdownConfig.MarkdownPath, slug + ".md");
        var fileChanged = await originalFileName.IsFileChanged(MarkdownConfig.MarkdownTranslatedPath);
        if (language == EnglishLanguage)
        {
            fileName = originalFileName;
        }
        else
        {
            Path.Combine(MarkdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
        }

        if (!File.Exists(fileName)) return true;
        return fileChanged;
    }

    public async Task<BlogPostViewModel> SavePost(string slug, string language, string markdown)
    {
        try
        {
            var outPath = Path.Combine(MarkdownConfig.MarkdownPath, slug + ".md");
            if (language != EnglishLanguage)
                outPath = Path.Combine(MarkdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
            await File.WriteAllTextAsync(outPath, markdown);
            return await GetPost(slug, language) ?? new BlogPostViewModel();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving post {PostName}", slug);
            return new BlogPostViewModel();
        }
    }


    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        var pageCache = GetPageCache().Select(x => x.Value);

        if (!string.IsNullOrEmpty(category)) pageCache = pageCache.Where(x => x.Categories.Contains(category));

        if (startDate != null) pageCache = pageCache.Where(x => x.PublishedDate >= startDate);

        return await Task.FromResult(pageCache.ToList());
    }


    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = EnglishLanguage)
    {
        var postsQuery = GetPageCache()
            .Where(x => x.Key.lang == language && x.Value.Categories.Contains(category))
            .Select(x => GetListModel(x.Value))
            .OrderByDescending(x => x.PublishedDate).ToList();

        var totalItems = postsQuery.Count();

        var posts = postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new PostListViewModel
        {
            Posts = posts,
            TotalItems = totalItems,
            PageSize = pageSize,
            Page = page
        };

        return await Task.FromResult(model);
    }


    public async Task<BlogPostViewModel?> GetPost(string slug, string language = EnglishLanguage)
    {
        try
        {
            // Attempt to retrieve from the cache first
            var pageCache = GetPageCache();
            if (pageCache.TryGetValue((slug, language), out var pageModel)) return await Task.FromResult(pageModel);

            return null;
        }
        catch (Exception ex)
        {
            // Log the error and return null
            _logger.LogError(ex, "Error getting post {PostName}", slug);
            return null;
        }
    }


    public async Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10,
        string language = EnglishLanguage)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Where(x => x.Value.Language == language)
            .Select(x => GetListModel(x.Value)).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return await Task.FromResult(model);
    }
}