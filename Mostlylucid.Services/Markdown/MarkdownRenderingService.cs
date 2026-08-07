using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.Markdig.Extensions;
using Mostlylucid.Markdig.FetchExtension;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Markdown;

public partial class MarkdownRenderingService : MarkdownBaseService
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<MarkdownRenderingService>? _logger;

    public MarkdownRenderingService() : base()
    {
        // Parameterless constructor for when DI is not available
    }

    public MarkdownRenderingService(IServiceProvider serviceProvider, ILogger<MarkdownRenderingService> logger) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*datetime\s+class\s*=\s*""hidden""\s*>(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})</\s*datetime\s*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex DateRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"<!--\s*category\s*--\s*(.+?)\s*-->")]
    private static partial Regex CategoryRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*pinned\s*/\s*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex PinnedRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*hidden\s*/\s*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex HiddenRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*scheduled\s+datetime\s*=\s*""(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})""\s*/\s*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ScheduledRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"<\s*updated(?:\s+template\s*=\s*""([^""]+)"")?\s*/?\s*>", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex UpdatedRegex();

    private static string[] GetCategories(string markdownText)
    {
        var matches = CategoryRegex().Match(markdownText);
        if(matches.Success)
            return matches.Groups[1].Value.Split(',').Select(x => x.Trim()).ToArray();
        return Array.Empty<string>();
    }

    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex SplitRegex();

    public BlogPostDto? GetPageFromMarkdown(string markdown, DateTime publishedDate, string filePath)
    {
        return GetPageFromMarkdown(markdown, publishedDate, filePath, sourceUrl: null);
    }

    public BlogPostDto? GetPageFromMarkdown(string markdown, DateTime publishedDate, string filePath, string? sourceUrl)
    {
        // Leading blank lines are harmless but would otherwise make the title check below fail and
        // silently skip the whole post, so normalise them away before anything looks at line 0.
        markdown = markdown.TrimStart('\r', '\n', ' ', '\t');

        // Preprocess markdown to inject fetched content BEFORE parsing
        // This ensures everything goes through the pipeline once
        if (_serviceProvider != null)
        {
            var preprocessor = new Mostlylucid.Markdig.FetchExtension.Processors.MarkdownFetchPreprocessor(_serviceProvider);
            markdown = preprocessor.Preprocess(markdown);
        }

        // Use RemoteLinkRewriteExtension if we have a source URL (for fetched content)
        var pipeline = string.IsNullOrEmpty(sourceUrl)
            ? Pipeline()
            : Pipeline(builder =>
            {
                var extension = new RemoteLinkRewriteExtension(sourceUrl);
                builder.Extensions.Add(extension);
            });

        var lines =  SplitRegex().Split(markdown);
        // Get the title from the first line - must start with # to be valid
        var firstLine = lines.Length > 0 ? lines[0].Trim() : string.Empty;

        // Validate that the first line is a proper markdown heading
        if (!firstLine.StartsWith('#'))
        {
            _logger?.LogWarning("Markdown file {FilePath} does not start with a heading (first line: '{FirstLine}'). Skipping.",
                filePath, firstLine.Length > 50 ? firstLine[..50] + "..." : firstLine);
            return null;
        }

        var title = global::Markdig.Markdown.ToPlainText(firstLine);
        title = title.Trim();

        // Skip files with empty titles
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger?.LogWarning("Markdown file {FilePath} has an empty title. Skipping.", filePath);
            return null;
        }
        // Concatenate the rest of the lines with newline characters
        var restOfTheLines = string.Join(Environment.NewLine, lines.Skip(1));

        // Extract categories from the text
        var categories = GetCategories(restOfTheLines);

        var publishDate = DateRegex().Match(restOfTheLines).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(publishDate))
            publishedDate = DateTime.ParseExact(publishDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        // Extract pinned, hidden, and scheduled flags
        var isPinned = PinnedRegex().IsMatch(restOfTheLines);
        var isHidden = HiddenRegex().IsMatch(restOfTheLines);

        DateTimeOffset? scheduledPublishDate = null;
        var scheduledMatch = ScheduledRegex().Match(restOfTheLines);
        if (scheduledMatch.Success)
        {
            var scheduledDateString = scheduledMatch.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(scheduledDateString))
            {
                var scheduledDateTime = DateTime.ParseExact(scheduledDateString, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                scheduledPublishDate = new DateTimeOffset(scheduledDateTime);
            }
        }

        // Extract updated tag and template
        bool showUpdatedDate = false;
        string? updatedTemplate = null;
        var updatedMatch = UpdatedRegex().Match(restOfTheLines);
        if (updatedMatch.Success)
        {
            showUpdatedDate = true;
            // Group 1 contains the template if present
            if (updatedMatch.Groups[1].Success && !string.IsNullOrWhiteSpace(updatedMatch.Groups[1].Value))
            {
                updatedTemplate = updatedMatch.Groups[1].Value;
            }
        }

        // Remove category tags and metadata from the text
        restOfTheLines = CategoryRegex().Replace(restOfTheLines, "");
        restOfTheLines = DateRegex().Replace(restOfTheLines, "");
        restOfTheLines = PinnedRegex().Replace(restOfTheLines, "");
        restOfTheLines = HiddenRegex().Replace(restOfTheLines, "");
        restOfTheLines = ScheduledRegex().Replace(restOfTheLines, "");
        restOfTheLines = UpdatedRegex().Replace(restOfTheLines, "");

        // Process the rest of the lines as either HTML or plain text
        string processed;
        string plainText;
        try
        {
            processed = global::Markdig.Markdown.ToHtml(restOfTheLines, pipeline);
            // Post-process HTML to rewrite local Windows file paths in <img src> to web-served paths
            processed = RewriteLocalImageSources(processed);
            plainText = global::Markdig.Markdown.ToPlainText(restOfTheLines, pipeline);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("depth limit exceeded"))
        {
            _logger?.LogWarning("Markdown file {FilePath} has excessive nesting depth and cannot be parsed. Skipping. Error: {Error}",
                filePath, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse markdown file {FilePath}. Skipping.", filePath);
            return null;
        }

        // Generate the slug from the page filename
        var slug = GetSlug(filePath);

        // Return the parsed and processed content
        return new BlogPostDto()
        {
            Markdown =  markdown,
            Categories = categories,
            WordCount = restOfTheLines.WordCount(),
            HtmlContent = processed,
            PlainTextContent = plainText,
            PublishedDate = publishedDate,
            Slug = slug,
            Title = title,
            IsPinned = isPinned,
            IsHidden = isHidden,
            ScheduledPublishDate = scheduledPublishDate,
            ShowUpdatedDate = showUpdatedDate,
            UpdatedTemplate = updatedTemplate
        };
    }

    private static bool LooksLikeWindowsAbsolutePath(string src)
    {
        if (string.IsNullOrWhiteSpace(src)) return false;
        // file:/// or drive letter like C:\ or C:/ or UNC \\\\server\share
        if (src.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return true;
        if (src.Length >= 3 && char.IsLetter(src[0]) && src[1] == ':' && (src[2] == '/' || src[2] == '\\')) return true;
        if (src.StartsWith("\\\\")) return true;
        return false;
    }

    private static string RewriteLocalImageSources(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;
        // Find <img ... src="..." ...>
        var imgRegex = new Regex("<img[^>]*?src=\"([^\"]+)\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return imgRegex.Replace(html, match =>
        {
            var tag = match.Value;
            var src = match.Groups[1].Value;
            if (!LooksLikeWindowsAbsolutePath(src)) return tag;

            try
            {
                // Extract filename and rewrite to /img/shots/{filename}
                var fileName = System.IO.Path.GetFileName(src.Replace('\\', '/'));
                if (string.IsNullOrEmpty(fileName)) return tag;
                var newSrc = "/img/shots/" + Uri.EscapeDataString(fileName);
                var replaced = Regex.Replace(tag, "src=\"[^\"]+\"", $"src=\"{newSrc}\"", RegexOptions.IgnoreCase);
                if (!replaced.Contains("onerror=", StringComparison.OrdinalIgnoreCase))
                {
                    // insert onerror fallback before closing '>'
                    var idx = replaced.LastIndexOf('>');
                    if (idx >= 0)
                    {
                        replaced = replaced.Insert(idx, " onerror=\"this.onerror=null;this.src='/img/placeholder.svg'\"");
                    }
                }
                return replaced;
            }
            catch
            {
                return tag;
            }
        });
    }

    private string GetSlug(string fileName)
    {
        // Get filename without extension
        var slug = Path.GetFileNameWithoutExtension(fileName);

        // Handle language-suffixed files like "mypost.es.md" -> "mypost"
        // Extract only the base name before any dot (language suffix)
        if (slug.Contains("."))
        {
            slug = slug.Substring(0, slug.IndexOf(".", StringComparison.Ordinal));
        }

        // Normalize slug: convert underscores/spaces to hyphens and lowercase for consistent lookup
        slug = slug.Replace('_', '-').Replace(' ', '-').ToLowerInvariant();

        return slug;
    }
}