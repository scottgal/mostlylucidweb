using Mostlylucid.Models.Blog;

using System.IO;
using System.Text.RegularExpressions;
using Markdig;

namespace Mostlylucid.Services;

public class BlogService(ILogger<BlogService> logger)
{
    private const string Path = "Markdown";
    public BlogPostViewModel GetPost(string postName)
    {
    
        var path = System.IO.Path.Combine(Path, postName + ".md");
        var (title, slug, lastWrite, plainText, categories, restOfTheLines) = GetPage(path, true);
        return new BlogPostViewModel {Categories = categories, Content = plainText, Date = lastWrite, Slug = slug, Title = title};
    }
    
    
    private static Regex WordCoountRegex = new  Regex( @"\b\w+\b", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private int WordCount(string text) => WordCoountRegex.Matches(text).Count;

    
    private string GetSlug(string fileName)
    {
      var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
      return slug.ToLowerInvariant();
    }

    private static Regex CategoryRegex= new Regex(@"<!--\s*category\s*--\s*([^,]+?)\s*(?:,\s*([^,]+?)\s*)?-->", RegexOptions.Compiled | RegexOptions.Singleline);
    private static string[] GetCategories(string markdownText)
    {
     
        var matches = CategoryRegex.Matches(markdownText);
        var categories = matches
            .SelectMany(match => match.Groups.Cast<Group>()
                .Skip(1)  // Skip the entire match group
                .Where(group => group.Success)  // Ensure the group matched
                .Select(group => group.Value.Trim()))
            .ToArray();
        return categories;
    }

    public (string title, string slug, DateTime lastWrite, string plainText,
        string[] categories, string restOfTheLines) GetPage(string page, bool html)
    {
        var fileInfo = new FileInfo(page);
        var lines = System.IO.File.ReadAllLines(page);
        var title = Markdown.ToPlainText( lines[0].Trim());
         
        var restOfTheLines = string.Concat(lines.Skip(1));
        var categories = GetCategories(restOfTheLines);
        restOfTheLines= CategoryRegex.Replace(restOfTheLines, "");
        var processed = html? Markdown.ToHtml(restOfTheLines) :  Markdown.ToPlainText(restOfTheLines);
      
        
        var slug = GetSlug(page);
        return (title, slug, fileInfo.LastWriteTime, processed, categories, restOfTheLines);
    }
    
    public List<PostListModel> GetPosts()
    {

        List<PostListModel> pageModels = new();
        var pages = Directory.GetFiles("Markdown" , "*.md");
        foreach (var page in pages)
        {
           
            var pageInfo = GetPage(page, false);
            
            var summary = Markdown.ToPlainText( pageInfo.restOfTheLines).Substring(0,100) + "...";
            pageModels.Add(new PostListModel {Categories = pageInfo.categories, Title = pageInfo.title,
                Slug = pageInfo.slug, WordCount = WordCount(pageInfo.restOfTheLines),Date = pageInfo.lastWrite, Summary = summary });
        }
        pageModels = pageModels.OrderByDescending(x => x.Date).ToList();
        return pageModels;
    }
}