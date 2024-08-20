using System.Collections.Concurrent;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog.Markdown;

public class MarkdownBlogPopulator(MarkdownConfig markdownConfig, MarkdownRenderingService markdownRenderingService)
    : MarkdownBaseService(markdownConfig), IBlogPopulator, IMarkdownBlogService
{
    private readonly MarkdownConfig _markdownConfig = markdownConfig;

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
            pageCache.TryAdd((page.Slug, page.Language), page);
            
           
        }
        SetPageCache(pageCache);
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
        return markdownRenderingService.GetPageFromMarkdown(lines, publishedDate, filePath);

  
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


}