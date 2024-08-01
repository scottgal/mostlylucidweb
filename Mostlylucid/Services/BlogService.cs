using System.Globalization;
using Mostlylucid.Models.Blog;
using System.Text.RegularExpressions;
using Markdig;
using Mostlylucid.MarkDigExtensions;

namespace Mostlylucid.Services;

public class BlogService
{
    private ILogger<BlogService> _logger;
    public BlogService(ILogger<BlogService> logger)
    {
        _logger = logger;
          pipeline  =  new MarkdownPipelineBuilder().UseAdvancedExtensions().Use<ImgExtension>().Build();
        
    }

    private MarkdownPipeline pipeline;
    private const string Path = "Markdown";

    public BlogPostViewModel? GetPost(string postName)
    {
        try
        {
            var path = System.IO.Path.Combine(Path, postName + ".md");
            var page = GetPage(path, true);
            return new BlogPostViewModel
            {
                Categories = page.categories,WordCount = WordCount(page.restOfTheLines) , Content = page.processed,
                PublishedDate = page.publishDate, Slug = page.slug, Title = page.title
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting post {PostName}", postName);
            return null;
        }
    }

    private static Regex DateRegex = new Regex( @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static Regex WordCoountRegex = new Regex(@"\b\w+\b",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private int WordCount(string text) => WordCoountRegex.Matches(text).Count;


    private string GetSlug(string fileName)
    {
        var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
        return slug.ToLowerInvariant();
    }

    private static Regex CategoryRegex = new Regex(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->",
        RegexOptions.Compiled | RegexOptions.Singleline);

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
    
    public (string title, string slug, DateTime publishDate, string processed, string[] categories, string restOfTheLines) GetPage(string page, bool html)
    {
        var fileInfo = new FileInfo(page);
    
        // Ensure the file exists
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("The specified file does not exist.", page);
        }

        // Read all lines from the file
        var lines = System.IO.File.ReadAllLines(page);
    
        // Get the title from the first line
        var title = lines.Length > 0 ? Markdown.ToPlainText(lines[0].Trim()) : string.Empty;

        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));
    
        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);
    
        DateTime publishedDate = fileInfo.CreationTime;
        var publishDate = DateRegex.Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
        {
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        }
        // Remove category tags from the text
        restOfTheLines = CategoryRegex.Replace(restOfTheLines, "");
        restOfTheLines = DateRegex.Replace(restOfTheLines, "");
        // Process the rest of the lines as either HTML or plain text
        var processed = html ? Markdown.ToHtml(restOfTheLines, pipeline) : Markdown.ToPlainText(restOfTheLines, pipeline);
    
        // Generate the slug from the page filename
        var slug = GetSlug(page);
 


        // Return the parsed and processed content
        return (title, slug,publishedDate , processed, categories, restOfTheLines);
    }

    public List<PostListModel> GetPosts()
    {
        List<PostListModel> pageModels = new();
        var pages = Directory.GetFiles("Markdown", "*.md");
        foreach (var page in pages)
        {
            var pageInfo = GetPage(page, false);

            var summary = Markdown.ToPlainText(pageInfo.restOfTheLines).Substring(0, 100) + "...";
            pageModels.Add(new PostListModel
            {
                Categories = pageInfo.categories, Title = pageInfo.title,
                Slug = pageInfo.slug, WordCount = WordCount(pageInfo.restOfTheLines), 
                PublishedDate = pageInfo.publishDate, Summary = summary
            });
        }

        pageModels = pageModels.OrderByDescending(x => x.PublishedDate).ToList();
        return pageModels;
    }
}