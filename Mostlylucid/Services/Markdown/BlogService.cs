using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;
using Path = System.IO.Path;

namespace Mostlylucid.Services.Markdown;

public class BlogService : BaseService, IBlogService, IMarkdownBlogService
{
    private const string LanguageCacheKey = "Languages";
    private const string PageCacheKey = "Pages";

    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex WordCoountRegex = new(@"\b\w+\b",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ILogger<BlogService> _logger;
    private readonly MarkdownConfig _markdownConfig;
    private readonly IMemoryCache _memoryCache;
    private readonly Lazy<Task> _initializationTask;
    
    public BlogService(MarkdownConfig markdownConfig, IMemoryCache memoryCache, ILogger<BlogService> logger)
    {
        _markdownConfig = markdownConfig;
        _logger = logger;
        _memoryCache = memoryCache;
      
        _initializationTask = new Lazy<Task>(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        PopulateLanguages();
       await PopulatePages();
    }
    

    private string DirectoryPath => _markdownConfig.MarkdownPath;


    public async Task<List<string>> GetCategories()
    {
        await _initializationTask.Value;
        var pages = GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return await Task.FromResult(categories);
    }


    public async Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "")
    {
        await _initializationTask.Value;
        var pageCache = GetPageCache().Select(x => x.Value);

        if (!string.IsNullOrEmpty(category))
        {
            pageCache = pageCache.Where(x => x.Categories.Contains(category));
        }

        if (startDate != null)
        {
            pageCache = pageCache.Where(x => x.PublishedDate >= startDate);
        }

        return await Task.FromResult(pageCache.ToList());
    }



    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10)
    {
        await _initializationTask.Value;
        var postsQuery = GetPageCache()
            .Where(x => x.Value.Categories.Contains(category))
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


    public async Task<BlogPostViewModel?> GetPost(string postName, string language = "")
    {
        try
        {
            // Normalize language input for "en"
            if (language == "en")
            {
                language = "";
            }

            await _initializationTask.Value;
            // Attempt to retrieve from the cache first
            var pageCache = GetPageCache();
            if (string.IsNullOrEmpty(language) && pageCache.TryGetValue(postName, out var pageModel))
            {
                return await Task.FromResult(pageModel);
            }

            // Construct the file path for the specified language
            var filePath = Path.Combine(_markdownConfig.MarkdownTranslatedPath, $"{postName}.{language}.md");

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Post {PostName} not found", postName);
                return null;
            }

            // Retrieve and build the page model
            var model =await GetPage(filePath);
            model.Languages = GetLanguages(postName).ToArray();

            return  model;
        }
        catch (Exception ex)
        {
            // Log the error and return null
            _logger.LogError(ex, "Error getting post {PostName}", postName);
            return null;
        }
    }



    public async Task<PostListViewModel> GetPostsForFiles(int page = 1, int pageSize = 10)
    {
        var model = new PostListViewModel();
        await _initializationTask.Value;
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return  await Task.FromResult( model);
    }


    private Dictionary<string, List<string>> GetLanguageCache()
    {
        return _memoryCache.Get<Dictionary<string, List<string>>>(LanguageCacheKey) ??
               new Dictionary<string, List<string>>();
    }

    private void SetLanguageCache(Dictionary<string, List<string>> languages)
    {
        _memoryCache.Set(LanguageCacheKey, languages, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        });
    }


    private async Task PopulatePages()
    {
        if(GetPageCache() is {Count: > 0}) return;
        Dictionary<string, BlogPostViewModel> pageCache = new();
        var pages = Directory.GetFiles(DirectoryPath, "*.md");
        foreach(var page in pages)
        {
            var pageModel =await GetPage(page);
            pageCache.TryAdd(pageModel.Slug, pageModel);
        }
     

        SetPageCache(pageCache.ToDictionary());
    }

    private Dictionary<string, BlogPostViewModel> GetPageCache()
    {
        return _memoryCache.Get<Dictionary<string, BlogPostViewModel>>(PageCacheKey) ??
               new Dictionary<string, BlogPostViewModel>();
    }

    private void SetPageCache(Dictionary<string, BlogPostViewModel> pages)
    {
        _memoryCache.Set(PageCacheKey, pages, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        });
    }

    private List<string> GetLanguages(string page)
    {
        page = Path.GetFileName(page);
        var cacheLangs = GetLanguageCache();
        var outLangs = cacheLangs.TryGetValue(page, out var languages) ? languages : new List<string>();
        if (outLangs.Any() && !outLangs.Contains("en"))
            outLangs.Insert(0, "en");
        return outLangs;
    }

    private void PopulateLanguages()
    {
        var cacheLangs = GetLanguageCache();
        if(cacheLangs is {Count: > 0}) return;
        var pages = Directory.GetFiles(_markdownConfig.MarkdownTranslatedPath, "*.md");
        var count = 0;

        foreach (var page in pages)
        {
            var pageName = Path.GetFileNameWithoutExtension(page);
            var languageCode = pageName.LastIndexOf(".", StringComparison.Ordinal) + 1;
            var language = pageName.Substring(languageCode);
            var originPage = pageName.Substring(0, languageCode - 1);

            var pageEntry = cacheLangs.TryGetValue(originPage, out var languagesList)
                ? languagesList
                : new List<string>();
            var languageAlreadyAdded = pageEntry.Any(x => x.Contains(language));

            if (languageAlreadyAdded) continue;
            count++;
            pageEntry.Add(language);

            cacheLangs[originPage] = pageEntry;
        }

        if (count > 0) SetLanguageCache(cacheLangs);
    }

    private int WordCount(string text)
    {
        return WordCoountRegex.Matches(text).Count;
    }


    private string GetSlug(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName);
        if (slug.Contains(".")) slug = slug.Substring(0, slug.LastIndexOf(".", StringComparison.Ordinal));

        return slug.ToLowerInvariant();
    }

    private static string[] GetCategories(string markdownText)
    {
        var matches = CategoryRegex.Matches(markdownText);
        var categories = matches
            .SelectMany(match => match.Groups.Cast<Group>()
                .Skip(1) // Skip the entire match group
                .Where(group => group.Success) // Ensure the group matched
                .Select(group => group.Value.Trim()))
            .ToArray();
        return categories;
    }

    private async Task<BlogPostViewModel> GetPage(string page)
    {
        var fileInfo = new FileInfo(page);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", page);

        // Read all lines from the file
        var lines = await File.ReadAllLinesAsync(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdig.Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

        var publishedDate = fileInfo.CreationTime;
        var publishDate = DateRegex.Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");
        restOfTheLines = DateRegex.Replace(restOfTheLines, "");
        // Process the rest of the lines as either HTML or plain text
        var processed = Markdig.Markdown.ToHtml(restOfTheLines, _pipeline);
        var plainText = Markdig.Markdown.ToPlainText(restOfTheLines, _pipeline);

        // Generate the slug from the page filename
        var slug = GetSlug(page);
        var languages = GetLanguages(Path.GetFileNameWithoutExtension(page)).ToArray();

        // Return the parsed and processed content
        return new BlogPostViewModel
        {
            Languages = languages,
            Categories = categories,
            WordCount = WordCount(restOfTheLines),
            HtmlContent = processed,
            PlainTextContent = plainText,
            PublishedDate = publishedDate,
            Slug = slug,
            Title = title
        };
    }

    private PostListModel GetListModel(BlogPostViewModel model)
    {
        return new PostListModel
        {
            Title = model.Title,
            PublishedDate = model.PublishedDate,
            Slug = model.Slug,
            Categories = model.Categories,
            Summary = model.PlainTextContent.TruncateAtWord(200) + "...",
            Languages = model.Languages
        };
    }
}