using System.Globalization;
using System.Text.RegularExpressions;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog;

public class MarkdownRenderingService : MarkdownBaseService
{
    private static readonly Regex DateRegex = new(
        @"<datetime class=""hidden"">(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</datetime>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private static readonly Regex CategoryRegex = new(@"<!--\s*category\s*--\s*(.+?)\s*-->", RegexOptions.Compiled);

    
    
    private static string[] GetCategories(string markdownText)
    {
        var matches = CategoryRegex.Match(markdownText);
        if(matches.Success)
            return matches.Groups[1].Value.Split(',').Select(x => x.Trim()).ToArray();
        return Array.Empty<string>();
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
            Markdown =  string.Join(Environment.NewLine, lines),
            Categories = categories,
            WordCount = restOfTheLines.WordCount(),
            HtmlContent = processed,
            PlainTextContent = plainText,
            PublishedDate = publishedDate,
            Slug = slug,
            Title = title
        };
    }
    
    private string GetSlug(string fileName)
    {
        var slug = Path.GetFileNameWithoutExtension(fileName);
        if (slug.Contains(".")) slug = slug.Substring(0, slug.IndexOf(".", StringComparison.Ordinal));

        return slug.ToLowerInvariant();
    }
}