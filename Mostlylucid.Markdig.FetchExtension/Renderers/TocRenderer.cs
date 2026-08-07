using System.Text;
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mostlylucid.Markdig.FetchExtension.Models;

namespace Mostlylucid.Markdig.FetchExtension.Renderers;

/// <summary>
/// HTML renderer for Table of Contents blocks
/// Generates nested ul/li structure with anchor links
/// </summary>
public partial class TocRenderer : HtmlObjectRenderer<TocBlock>
{
    [GeneratedRegex(@"[^a-z0-9\s-]", RegexOptions.IgnoreCase)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    protected override void Write(HtmlRenderer renderer, TocBlock obj)
    {
        // Get the document - walk up from the TOC block
        var document = GetDocument(obj);
        if (document == null)
        {
            // Document not found - render a comment for debugging
            renderer.WriteLine($"<!-- [TOC] ERROR: Could not find document -->");
            return;
        }

        // Collect all headings in the document within the max level constraint
        var allHeadings = document.Descendants()
            .OfType<HeadingBlock>()
            .Where(h => h.Level <= obj.MaxLevel)
            .ToList();

        if (allHeadings.Count == 0)
        {
            // No headings found at all
            renderer.WriteLine($"<!-- [TOC] No headings found (max level: {obj.MaxLevel}). Document has {document.Descendants().Count()} descendants, {document.Descendants().OfType<HeadingBlock>().Count()} are HeadingBlocks -->");
            return;
        }

        // Auto-detect the minimum heading level present in the document
        var actualMinLevel = allHeadings.Min(h => h.Level);

        // Use the detected minimum level if it's higher than configured
        var effectiveMinLevel = Math.Max(obj.MinLevel, actualMinLevel);

        // Filter headings by the effective level range
        var filteredHeadings = allHeadings
            .Where(h => h.Level >= effectiveMinLevel && h.Level <= obj.MaxLevel)
            .ToList();

        if (filteredHeadings.Count == 0)
        {
            // This shouldn't happen given our logic above, but just in case
            renderer.WriteLine($"<!-- [TOC] No headings found after filtering (effective range: {effectiveMinLevel}-{obj.MaxLevel}) -->");
            return;
        }

        // Log the auto-adjustment if it happened
        if (actualMinLevel > obj.MinLevel)
        {
            renderer.WriteLine($"<!-- [TOC] Auto-adjusted minLevel from {obj.MinLevel} to {actualMinLevel} (document starts at H{actualMinLevel}) -->");
        }

        // Convert to our HeadingInfo structure
        var headings = filteredHeadings.Select(h => new HeadingInfo
        {
            Level = h.Level,
            Text = ExtractHeadingText(h),
            Id = GenerateId(ExtractHeadingText(h)),
            Heading = h
        }).ToList();

        // Ensure all headings have IDs
        foreach (var heading in headings)
        {
            EnsureHeadingHasId(heading.Heading, heading.Id);
        }

        // Use provided CSS class or default to "ml_toc"
        var cssClass = !string.IsNullOrEmpty(obj.CssClass) ? obj.CssClass : "ml_toc";

        // Start TOC container nav
        renderer.WriteLine($"<nav class=\"{cssClass}\" aria-label=\"Table of Contents\">");

        if (!string.IsNullOrEmpty(obj.Title))
        {
            renderer.WriteLine($"<div class=\"toc-title\">{EscapeHtml(obj.Title)}</div>");
        }

        // Build nested list starting from the effective minimum level
        BuildNestedList(renderer, headings, 0, effectiveMinLevel);

        renderer.WriteLine("</nav>");
    }

    /// <summary>
    /// Get the document from a block
    /// </summary>
    private static MarkdownDocument? GetDocument(TocBlock block)
    {
        var current = block.Parent;
        while (current != null)
        {
            if (current is MarkdownDocument doc)
            {
                return doc;
            }
            current = current.Parent;
        }
        return null;
    }


    /// <summary>
    /// Extract plain text from a heading block
    /// </summary>
    private static string ExtractHeadingText(HeadingBlock heading)
    {
        if (heading.Inline == null)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var inline in heading.Inline.Descendants())
        {
            if (inline is LiteralInline literal)
            {
                sb.Append(literal.Content.ToString());
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Generate a URL-safe ID from heading text
    /// </summary>
    private static string GenerateId(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "heading";

        // Convert to lowercase
        var id = text.ToLowerInvariant();

        // Remove invalid characters
        id = InvalidCharsRegex().Replace(id, "");

        // Replace whitespace with hyphens
        id = WhitespaceRegex().Replace(id, "-");

        // Remove leading/trailing hyphens
        id = id.Trim('-');

        // Ensure it's not empty
        if (string.IsNullOrEmpty(id))
            id = "heading";

        return id;
    }

    /// <summary>
    /// Ensure the heading has an ID attribute for anchor linking
    /// </summary>
    private static void EnsureHeadingHasId(HeadingBlock heading, string id)
    {
        // Check if heading already has an ID
        var attributes = heading.GetAttributes();
        if (attributes.Id == null)
        {
            attributes.Id = id;
        }
    }

    /// <summary>
    /// Build nested ul/li structure recursively
    /// </summary>
    private void BuildNestedList(HtmlRenderer renderer, List<HeadingInfo> headings, int startIndex, int currentLevel)
    {
        if (startIndex >= headings.Count)
            return;

        renderer.WriteLine("<ul>");

        for (int i = startIndex; i < headings.Count; i++)
        {
            var heading = headings[i];

            if (heading.Level < currentLevel)
            {
                // Parent level - stop here
                renderer.WriteLine("</ul>");
                return;
            }

            if (heading.Level == currentLevel)
            {
                // Same level - render item
                renderer.Write("<li>");
                renderer.Write($"<a href=\"#{heading.Id}\">{EscapeHtml(heading.Text)}</a>");

                // Check if next item is a child
                if (i + 1 < headings.Count && headings[i + 1].Level > currentLevel)
                {
                    // Render children
                    var childrenEnd = FindChildrenEnd(headings, i + 1, currentLevel);
                    BuildNestedList(renderer, headings, i + 1, currentLevel + 1);
                    i = childrenEnd - 1; // Skip processed children
                }

                renderer.WriteLine("</li>");
            }
            else if (heading.Level > currentLevel)
            {
                // Deeper level - should not happen in well-formed iteration
                continue;
            }
        }

        renderer.WriteLine("</ul>");
    }

    /// <summary>
    /// Find where children at the next level end
    /// </summary>
    private static int FindChildrenEnd(List<HeadingInfo> headings, int startIndex, int parentLevel)
    {
        for (int i = startIndex; i < headings.Count; i++)
        {
            if (headings[i].Level <= parentLevel)
            {
                return i;
            }
        }
        return headings.Count;
    }

    /// <summary>
    /// Escape HTML special characters
    /// </summary>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Heading information for TOC generation
    /// </summary>
    private class HeadingInfo
    {
        public int Level { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public HeadingBlock Heading { get; set; } = null!;
    }
}
