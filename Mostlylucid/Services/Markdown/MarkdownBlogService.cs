using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;
using Path = System.IO.Path;

namespace Mostlylucid.Services.Markdown;

public class MarkdownBlogService(MarkdownConfig markdownConfig, ILogger<MarkdownBlogService> logger)
    : BaseService, IBlogService, IMarkdownBlogService
{
    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Dictionary<(string slug, string language), BlogPostViewModel> PageCache = new();

    private ParallelOptions ParallelOptions => new() { MaxDegreeOfParallelism = 4 };


    private string DirectoryPath => markdownConfig.MarkdownPath;


    /// <summary>
    ///     The method to preload the cache with pages and Languages.
    /// </summary>
    public async Task Populate()
    {
        await PopulatePages();
    }


    public async Task<List<string>> GetCategories()
    {
        var pages = GetPageCache();
        var categories = pages.Values.SelectMany(x => x.Categories).Distinct().ToList();
        return await Task.FromResult(categories);
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


    public async Task<BlogPostViewModel?> GetPost(string postName, string language = EnglishLanguage)
    {
        try
        {
            // Attempt to retrieve from the cache first
            var pageCache = GetPageCache();
            if (pageCache.TryGetValue((postName, language), out var pageModel)) return await Task.FromResult(pageModel);

            return null;
        }
        catch (Exception ex)
        {
            // Log the error and return null
            logger.LogError(ex, "Error getting post {PostName}", postName);
            return null;
        }
    }


    public async Task<PostListViewModel> GetPosts(int page = 1, int pageSize = 10, string language = EnglishLanguage)
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

    public async Task<List<BlogPostViewModel>> GetPages()
    {
        var pageList = new ConcurrentBag<BlogPostViewModel>();
        var languages = LanguageList();
        var pages = await GetLanguagePages(EnglishLanguage);
        foreach (var page in pages) pageList.Add(page);
        var pageLanguages = languages.Values.SelectMany(x => x).Distinct().ToList();
        await Parallel.ForEachAsync(pageLanguages, ParallelOptions, async (pageLang, ct) =>
        {
            var langPages = await GetLanguagePages(pageLang);
            if (langPages is { Count: > 0 })
                foreach (var page in langPages)
                    pageList.Add(page);
        });
        foreach (var page in pageList)
        {
            var currentPagelangs = languages.Where(x => x.Key == page.Slug).SelectMany(x => x.Value)?.ToList();
            var listLangs = currentPagelangs ?? new List<string>();
            listLangs.Add(EnglishLanguage);
            page.Languages = listLangs.OrderBy(x => x).ToArray();
        }

        return pageList.ToList();
    }


    public Dictionary<string, List<string>> LanguageList()
    {
        var pages = Directory.GetFiles(markdownConfig.MarkdownTranslatedPath, "*.md");
        Dictionary<string, List<string>> languageList = new();
        foreach (var page in pages)
        {
            var pageName = Path.GetFileNameWithoutExtension(page);
            var languageCode = pageName.LastIndexOf(".", StringComparison.Ordinal) + 1;
            var language = pageName.Substring(languageCode);
            var originPage = pageName.Substring(0, languageCode - 1);
            if (languageList.TryGetValue(originPage, out var languages))
            {
                languages.Add(language);
                languageList[originPage] = languages;
            }
            else
            {
                languageList[originPage] = new List<string> { language };
            }
        }

        return languageList;
    }

    private async Task PopulatePages()
    {
        if (GetPageCache() is { Count: > 0 }) return;
        Dictionary<(string slug, string lang), BlogPostViewModel> pageCache = new();
        var pages = await GetPages();
        foreach (var page in pages) pageCache.TryAdd((page.Slug, page.Language), page);
        SetPageCache(pageCache);
    }

    private async Task<List<BlogPostViewModel>> GetLanguagePages(string language)
    {
        var pages = Directory.GetFiles(DirectoryPath, "*.md");
        if (language != EnglishLanguage)
            pages = Directory.GetFiles(markdownConfig.MarkdownTranslatedPath, $"*.{language}.md");

        var pageModels = new List<BlogPostViewModel>();
        await Parallel.ForEachAsync(pages, ParallelOptions, async (page, ct) =>
        {
            var pageModel = await GetPage(page);
            pageModel.Language = language;
            pageModels.Add(pageModel);
        });
        return pageModels;
    }

    private Dictionary<(string slug, string lang), BlogPostViewModel> GetPageCache()
    {
        return PageCache;
    }


    private void SetPageCache(Dictionary<(string slug, string lang), BlogPostViewModel> pages)
    {
        PageCache.Clear();
        foreach (var (key, value) in pages) PageCache.TryAdd(key, value);
    }


    private string GetSlug(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName);
        if (slug.Contains(".")) slug = slug.Substring(0, slug.IndexOf(".", StringComparison.Ordinal));

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

    private async Task<BlogPostViewModel> GetPage(string filePath)
    {
        var pipeline = Pipeline();
        var fileInfo = new FileInfo(filePath);

        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", filePath);

        // Read all lines from the file
        var lines = await File.ReadAllLinesAsync(filePath);

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
        var processed = Markdig.Markdown.ToHtml(restOfTheLines, pipeline);
        var plainText = Markdig.Markdown.ToPlainText(restOfTheLines, pipeline);

        // Generate the slug from the page filename
        var slug = GetSlug(filePath);

        // Return the parsed and processed content
        return new BlogPostViewModel
        {
            Categories = categories,
            WordCount = restOfTheLines.WordCount(),
            HtmlContent = processed,
            PlainTextContent = plainText,
            PublishedDate = publishedDate,
            Slug = slug,
            Title = title
        };
    }
}