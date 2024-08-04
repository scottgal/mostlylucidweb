using System.Globalization;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Config.Markdown;
using Mostlylucid.MarkDigExtensions;
using Mostlylucid.Models.Blog;
using Path = System.IO.Path;

namespace Mostlylucid.Services;

public class BlogService
{
    private  string DirectoryPath => _markdownConfig.MarkdownPath;
    private const string CacheKey = "Categories";
    private const string LanguageCacheKey = "Languages";

    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex WordCoountRegex = new(@"\b\w+\b",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ILogger<BlogService> _logger;

    private readonly IMemoryCache _memoryCache;

    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownConfig _markdownConfig;

    public BlogService(MarkdownConfig markdownConfig, IMemoryCache memoryCache, ILogger<BlogService> logger)
    {
        _markdownConfig = markdownConfig;
        _logger = logger;
        _memoryCache = memoryCache;
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseTableOfContent().Use<ImgExtension>()
            .Build();
        ListCategories();
        ListLanguages();
    }


    public Dictionary<string, List<string>> GetLanguageCache()
    {
        return _memoryCache.Get<Dictionary<string, List<string>>>(LanguageCacheKey) ??
               new Dictionary<string, List<string>>();
    }

    public void SetLanguageCache(Dictionary<string, List<string>> languages)
    {
        _memoryCache.Set(LanguageCacheKey, languages, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        });
    }

    private Dictionary<string, List<string>> GetCategoryCache()
    {
        return _memoryCache.Get<Dictionary<string, List<string>>>(CacheKey) ?? new Dictionary<string, List<string>>();
    }

    private void SetCategoryCache(Dictionary<string, List<string>> categories)
    {
        _memoryCache.Set(CacheKey, categories, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        });
    }

    public List<string> GetLanguages(string page)
    {
        var cacheLangs = GetLanguageCache();
        var outLangs= cacheLangs.TryGetValue(page, out var languages) ? languages : new List<string>();
        if(outLangs.Any() && !outLangs.Contains("en"))
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

    public async Task AddComment(string slug, string markdown, string nameIdentifier)
    {
        var path = Path.Combine(_markdownConfig.MarkdownCommentsPath,$"{DateTime.Now.ToFileTimeUtc()}_{nameIdentifier}_{slug}.md");
        var comment =markdown;
        await File.WriteAllTextAsync(path, comment);
    }

    private void ListCategories()
    {
        var cacheCats = GetCategoryCache();
        var pages = Directory.GetFiles("Markdown", "*.md");
        var count = 0;

        foreach (var page in pages)
        {
            var pageAlreadyAdded = cacheCats.Values.Any(x => x.Contains(page));

            if (pageAlreadyAdded) continue;


            var text = File.ReadAllText(page);
            var categories = GetCategories(text);
            if (!categories.Any()) continue;
            count++;
            foreach (var category in categories)
                if (cacheCats.TryGetValue(category, out var pagesList))
                {
                    pagesList.Add(page);
                    cacheCats[category] = pagesList;
                    _logger.LogInformation("Added category {Category} for {Page}", category, page);
                }
                else
                {
                    cacheCats.Add(category, new List<string> { page });
                    _logger.LogInformation("Created category {Category} for {Page}", category, page);
                }
        }

        if (count > 0) SetCategoryCache(cacheCats);
    }

    public List<string> GetCategories()
    {
        var cacheCats = GetCategoryCache();
        return cacheCats.Keys.ToList();
    }


    public List<PostListModel> GetPostsByCategory(string category)
    {
        var pages = GetCategoryCache()[category];
        return GetPosts(pages.ToArray());
    }

    public BlogPostViewModel? GetPost(string postName, string language = "")
    {
        if(language == "en")
            language = "";
        try
        {
            var path = Path.Combine(DirectoryPath, postName + ".md");
            if (!string.IsNullOrEmpty(language))
            {
                path = System.IO.Path.Combine(_markdownConfig.MarkdownTranslatedPath, postName + "." + language + ".md");
            }

            var page = GetPage(path, true);
            return new BlogPostViewModel
            {
                Categories = page.categories, WordCount = WordCount(page.restOfTheLines), Content = page.processed,
                PublishedDate = page.publishDate, Slug = page.slug, Title = page.title,
                Languages = GetLanguages(postName + ".md").ToArray()
            };
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
        var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
        if (slug.Contains("."))
        {
            slug = slug.Substring(0, slug.LastIndexOf(".", StringComparison.Ordinal));
        }

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

    public (string title, string slug, DateTime publishDate, string processed, string[] categories, string
        restOfTheLines) GetPage(string page, bool html)
    {
        var fileInfo = new FileInfo(page);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", page);

        // Read all lines from the file
        var lines = File.ReadAllLines(page);

        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

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
        var processed =
            html ? Markdown.ToHtml(restOfTheLines, _pipeline) : Markdown.ToPlainText(restOfTheLines, _pipeline);

        // Generate the slug from the page filename
        var slug = GetSlug(page);


        // Return the parsed and processed content
        return (title, slug, publishedDate, processed, categories, restOfTheLines);
    }

    public List<PostListModel> GetPosts(string[] pages)
    {
        List<PostListModel> pageModels = new();

        foreach (var page in pages)
        {
            var pageInfo = GetPage(page, false);

            var summary = Markdown.ToPlainText(pageInfo.restOfTheLines).Substring(0, 100) + "...";
            pageModels.Add(new PostListModel
            {
                Categories = pageInfo.categories, Title = pageInfo.title,
                Slug = pageInfo.slug, WordCount = WordCount(pageInfo.restOfTheLines),
                PublishedDate = pageInfo.publishDate, Summary = summary,
                Languages = GetLanguages(Path.GetFileName(page)).ToArray()
            });
        }

        pageModels = pageModels.OrderByDescending(x => x.PublishedDate).ToList();
        return pageModels;
    }


    public List<PostListModel> GetPostsForFiles()
    {
        var pages = Directory.GetFiles(DirectoryPath, "*.md");
        return GetPosts(pages);
    }
}