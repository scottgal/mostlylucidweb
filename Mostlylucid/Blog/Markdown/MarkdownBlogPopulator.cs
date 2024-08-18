using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBlogPopulator : MarkdownBaseService, IBlogPopulator, IMarkdownBlogService
{
    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly MarkdownConfig _markdownConfig;

    public MarkdownBlogPopulator(MarkdownConfig markdownConfig) : base(
        markdownConfig)
    {
        _markdownConfig = markdownConfig;
    }

    private ParallelOptions ParallelOptions => new() { MaxDegreeOfParallelism = 4 };

    /// <summary>
    ///     The method to preload the cache with pages and Languages.
    /// </summary>
    public async Task Populate()
    {
        await PopulatePages();
    }

    private async Task PopulatePages()
    {
        if (GetPageCache() is { Count: > 0 }) return;
        Dictionary<(string slug, string lang), BlogPostViewModel> pageCache = new();
        var pages = await GetPages();
        foreach (var page in pages)
        {
            if (page.Language != EnglishLanguage)
            {
                //Do not cache markdown for translated pages
                page.OriginalMarkdown = string.Empty;
            }
         
                pageCache.TryAdd((page.Slug, page.Language), page);
            
           
        }
        SetPageCache(pageCache);
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

    private Regex SplitRegex => new(@"\r\n|\r|\n", RegexOptions.Compiled);

    public BlogPostViewModel GetPageFromMarkdown(string markdownLines, DateTime publishedDate, string filePath)
    {
        var pipeline = Pipeline();
        var lines =  SplitRegex.Split(markdownLines);
        // Get the title from the first line
        var title = lines.Length > 0 ? Markdig.Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

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
            OriginalMarkdown =  string.Join(Environment.NewLine, lines),
            Categories = categories,
            WordCount = restOfTheLines.WordCount(),
            HtmlContent = processed,
            PlainTextContent = plainText,
            PublishedDate = publishedDate,
            Slug = slug,
            Title = title
        };
    }

    public async Task<BlogPostViewModel?> GetPageFromSlug(string slug, string language = "")
    {

        var pagePath =Path.Combine(_markdownConfig.MarkdownPath, $"{slug}.md");
        if (!string.IsNullOrEmpty(language) && language != EnglishLanguage)
            pagePath = Path.Combine(_markdownConfig.MarkdownTranslatedPath, $"{slug}.{language}.md");
        if (!File.Exists(pagePath))
            return null;
        var model= await GetPage(pagePath);
        model.Language = language;
        return model;
    }
    
    private async Task<BlogPostViewModel> GetPage(string filePath)
    {
      
        var fileInfo = new FileInfo(filePath);
        // Ensure the file exists
        if (!fileInfo.Exists) throw new FileNotFoundException("The specified file does not exist.", filePath);
        // Read all lines from the file
        var lines = await File.ReadAllTextAsync(filePath);
        var publishedDate = fileInfo.CreationTime;
        return  GetPageFromMarkdown(lines, publishedDate, filePath);

  
    }


    private async Task<List<BlogPostViewModel>> GetLanguagePages(string language)
    {
        var pages = Directory.GetFiles(_markdownConfig.MarkdownPath, "*.md");
        if (language != EnglishLanguage)
            pages = Directory.GetFiles(_markdownConfig.MarkdownTranslatedPath, $"*.{language}.md");

        var pageModels = new List<BlogPostViewModel>();
        await Parallel.ForEachAsync(pages, ParallelOptions, async (page, ct) =>
        {
            var pageModel = await GetPage(page);
            pageModel.Language = language;
            pageModels.Add(pageModel);
        });
        return pageModels;
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


    public  Dictionary<string, List<string>> LanguageList()
    {
        var pages = Directory.GetFiles(_markdownConfig.MarkdownTranslatedPath, "*.md");
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

    private string GetSlug(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName);
        if (slug.Contains(".")) slug = slug.Substring(0, slug.IndexOf(".", StringComparison.Ordinal));

        return slug.ToLowerInvariant();
    }
}