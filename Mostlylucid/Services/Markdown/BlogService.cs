using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;
using Path = System.IO.Path;

namespace Mostlylucid.Services.Markdown;

public class BlogService : BaseService
{
    private const string CacheKey = "Categories";
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


    public BlogService(MarkdownConfig markdownConfig, IMemoryCache memoryCache, ILogger<BlogService> logger)
    {
        _markdownConfig = markdownConfig;
        _logger = logger;
        _memoryCache = memoryCache;
        ListLanguages();
        PopulatePageCache();
    }

    private string DirectoryPath => _markdownConfig.MarkdownPath;


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


    private void PopulatePageCache()
    {
        Dictionary<string, BlogPostViewModel> pageCache = new();
        var pages = Directory.GetFiles(DirectoryPath, "*.md");
        foreach (var page in pages)
        {
            var pageModel = GetPage(page);
            pageCache.Add(pageModel.Slug, pageModel);
        }

        SetPageCache(pageCache);
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

    public List<string> GetLanguages(string page)
    {
        var cacheLangs = GetLanguageCache();
        var outLangs = cacheLangs.TryGetValue(page, out var languages) ? languages : new List<string>();
        if (outLangs.Any() && !outLangs.Contains("en"))
            outLangs.Insert(0, "en");
        return outLangs;
    }

    private void ListLanguages()
    {
        var cacheLangs = GetLanguageCache();
        var pages = Directory.GetFiles("Markdown/translated", "*.md");
        var count = 0;

        foreach (var page in pages)
        {
            var pageName = Path.GetFileNameWithoutExtension(page);
            var languageCode = pageName.LastIndexOf(".", StringComparison.Ordinal) + 1;
            var language = pageName.Substring(languageCode);
            var originPage = pageName.Substring(0, languageCode - 1) + ".md";

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


    public List<string> GetCategories()
    {
        var pages = GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return categories;
    }


    public List<BlogPostViewModel> GetPosts(DateTime? startDate = null, string category = "")
    {
        List<BlogPostViewModel> pagesList = new();
        if (!string.IsNullOrEmpty(category))
            pagesList = GetPageCache().Where(x => x.Value.Categories
                .Contains(category)).Select(x => x.Value).ToList();
        else
            pagesList = GetPageCache().Select(x => x.Value).ToList();
        if (startDate != null) pagesList = pagesList.Where(x => x.PublishedDate >= startDate).ToList();


        return pagesList;
    }


    public PostListViewModel GetPostsByCategory(string category)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache()
            .Where(x => x.Value.Categories.Contains(category))
            .Select(x => x.Value).Select(GetListModel);
        model.Posts = posts.ToList();
        return model;
    }

    public BlogPostViewModel? GetPost(string postName, string language = "")
    {
        if (language == "en")
            language = "";
        try
        {
            var pageCache = GetPageCache();
            if (pageCache.TryGetValue(postName, out var pageModel)) return pageModel;

            var page = Path.Combine(DirectoryPath, $"{postName}.{language}.md");
            if (!File.Exists(page))
            {
                _logger.LogWarning("Post {PostName} not found", postName);
                return null;
            }

            var model = GetPage(page);
            return model;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting post {PostName}", postName);
            return null;
        }
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

    public BlogPostViewModel GetPage(string page)
    {
        var fileInfo = new FileInfo(page);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", page);

        // Read all lines from the file
        var lines = File.ReadAllLines(page);

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


        // Return the parsed and processed content
        return new BlogPostViewModel
        {
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
            Summary = model.PlainTextContent.TruncateAtWord(100)
        };
    }

    private List<PostListModel> GetPosts(string[] pages)
    {
        List<PostListModel> pageModels = new();

        var pageCache = GetPageCache();

        foreach (var page in pages)
            if (pageCache.TryGetValue(page, out var pageModel))
                pageModels.Add(GetListModel(pageModel));

        pageModels = pageModels.OrderByDescending(x => x.PublishedDate).ToList();
        return pageModels;
    }


    public PostListViewModel GetPostsForFiles()
    {
        var model = new PostListViewModel();
        model.Posts = GetPageCache().Values.Select(GetListModel).ToList();
        return model;
    }
}