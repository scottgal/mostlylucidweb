using Mostlylucid.Models.Blog;

using System.IO;
namespace Mostlylucid.Services;

public class BlogService(ILogger<BlogService> logger)
{
    private const string Path = "Markdown";
    public string GetPost(string postName)
    {
        var pages = Directory.GetFiles("Markdown" , "*.md");
        var post = pages.First(x => x.Equals(postName + ".md", StringComparison.InvariantCultureIgnoreCase));
        var text= System.IO.File.ReadAllText(post);
        return Markdig.Markdown.ToHtml(text);
    }
    
    
    private string GetSlug(string fileName)
    {
      var slug = System.IO.Path.GetFileNameWithoutExtension(fileName);
      return slug.ToLowerInvariant();
    }
    
    public List<PostListModel> GetPosts()
    {
        List<PostListModel> pageModels = new();
        var pages = Directory.GetFiles("Markdown" , "*.md");
        foreach (var page in pages)
        {
            var fileInfo = new FileInfo(page);
            var lines = System.IO.File.ReadAllLines(page);
            var title = Markdig.Markdown.ToPlainText( lines[0].Trim());
            
            var restOfTheLines = string.Concat(lines.Skip(1));
            var summary = Markdig.Markdown.ToPlainText(restOfTheLines).Substring(0,50) + "...";
            var slug = GetSlug(page);
            pageModels.Add(new PostListModel { Title = title, Slug = slug, Description = summary, Date = fileInfo.LastWriteTime, Summary = summary });
        }
        return pageModels;
    }
}