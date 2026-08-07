using System.Text.RegularExpressions;

namespace Mostlylucid.Markdig.FetchExtension.Utilities;

using Mostlylucid.Markdig.FetchExtension.Models;

/// <summary>
///     Utility to rewrite relative markdown links to absolute URLs pointing back to source
///     Used for fetched remote markdown to preserve link integrity
/// </summary>
public partial class MarkdownLinkRewriter
{
    [GeneratedRegex(@"\[([^\]]+)\]\(([^\)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    public static string RewriteLinks(string markdown, string sourceUrl)
    {
        if (string.IsNullOrEmpty(markdown) || string.IsNullOrEmpty(sourceUrl))
            return markdown;

        return MarkdownLinkRegex().Replace(markdown, match =>
        {
            var linkText = match.Groups[1].Value;
            var linkUrl = match.Groups[2].Value;

            // Skip absolute URLs
            if (linkUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                linkUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                linkUrl.StartsWith("//", StringComparison.Ordinal))
                return match.Value;

            // Skip anchor links and mailto
            if (linkUrl.StartsWith('#') || linkUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            // Resolve relative URL
            var resolvedUrl = ResolveRelativeUrl(sourceUrl, linkUrl);
            return $"[{linkText}]({resolvedUrl})";
        });
    }

    private static string ResolveRelativeUrl(string sourceUrl, string relativeUrl)
    {
        try
        {
            // Special handling for GitHub raw URLs
            if (sourceUrl.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
                return ResolveGitHubUrl(sourceUrl, relativeUrl);

            // For other sources, use standard URL resolution
            var baseUri = new Uri(sourceUrl);
            var resolvedUri = new Uri(baseUri, relativeUrl);
            return resolvedUri.ToString();
        }
        catch (Exception)
        {
            // If URL resolution fails, return the original
            return relativeUrl;
        }
    }

    private static string ResolveGitHubUrl(string sourceUrl, string relativeUrl)
    {
        try
        {
            // Parse GitHub raw URL
            // Format: https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}
            var uri = new Uri(sourceUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 3)
                return new Uri(uri, relativeUrl).ToString();

            var owner = segments[0];
            var repo = segments[1];
            var branch = segments[2];
            var pathParts = segments.Skip(3).ToArray();

            // Get the directory of the current file
            var directory = pathParts.Length > 0
                ? string.Join("/", pathParts.Take(pathParts.Length - 1))
                : string.Empty;

            // Resolve the relative path
            var resolvedPath = ResolveRelativePath(directory, relativeUrl);

            // Convert to GitHub display URL
            // For .md files: https://github.com/{owner}/{repo}/blob/{branch}/{path}
            var displayUrl = $"https://github.com/{owner}/{repo}/blob/{branch}/{resolvedPath}";

            return displayUrl;
        }
        catch (Exception)
        {
            // Fallback to standard URL resolution
            var baseUri = new Uri(sourceUrl);
            var resolvedUri = new Uri(baseUri, relativeUrl);
            return resolvedUri.ToString();
        }
    }

    private static string ResolveRelativePath(string basePath, string relativePath)
    {
        // Split paths into segments
        var baseSegments = string.IsNullOrEmpty(basePath)
            ? new List<string>()
            : basePath.Split('/').ToList();

        var relativeSegments = relativePath.Split('/');

        foreach (var segment in relativeSegments)
            if (segment == "." || string.IsNullOrEmpty(segment))
            {
                // Current directory, skip
            }
            else if (segment == "..")
            {
                // Parent directory, pop last segment
                if (baseSegments.Count > 0)
                    baseSegments.RemoveAt(baseSegments.Count - 1);
            }
            else
            {
                // Add segment
                baseSegments.Add(segment);
            }

        return string.Join("/", baseSegments);
    }
}