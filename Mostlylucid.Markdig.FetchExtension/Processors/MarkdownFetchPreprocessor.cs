using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mostlylucid.Markdig.FetchExtension.Processors;

using Mostlylucid.Markdig.FetchExtension.Models;
using Mostlylucid.Markdig.FetchExtension.Services;
using Mostlylucid.Markdig.FetchExtension.Utilities;

/// <summary>
///     Preprocesses markdown text to fetch remote content before parsing
///     This ensures everything goes through the same pipeline once
/// </summary>
public partial class MarkdownFetchPreprocessor
{
    [GeneratedRegex(@"<fetch\s+(?:[^>]*?disable\s*=\s*[""'](true|false)[""'][^>]*?)?[^>]*?markdownurl\s*=\s*[""']([^""']+)[""'][^>]*?pollfrequency\s*=\s*[""'](\d+(?:\.\d+)?\s*[smhd]?)[""'](?:[^>]*?transformlinks\s*=\s*[""'](true|false)[""'])?(?:[^>]*?showsummary\s*=\s*[""'](true|false)[""'])?(?:[^>]*?summarytemplate\s*=\s*[""']([^""']+)[""'])?(?:[^>]*?cssclass\s*=\s*[""']([^""']+)[""'])?[^>]*?/\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FetchTagRegex();

    [GeneratedRegex(@"<fetch-summary\s+(?:[^>]*?disable\s*=\s*[""'](true|false)[""'][^>]*?)?[^>]*?url\s*=\s*[""']([^""']+)[""'](?:[^>]*?template\s*=\s*[""']([^""']+)[""'])?(?:[^>]*?cssclass\s*=\s*[""']([^""']+)[""'])?[^>]*?/\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FetchSummaryTagRegex();

    private readonly ILogger<MarkdownFetchPreprocessor>? _logger;

    private readonly IServiceProvider? _serviceProvider;

    public MarkdownFetchPreprocessor(IServiceProvider? serviceProvider,
        ILogger<MarkdownFetchPreprocessor>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    ///     Preprocesses markdown text by fetching remote content and replacing fetch tags
    /// </summary>
    public string Preprocess(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return markdown;

        // Dictionary to store fetch results for fetch-summary tags
        var fetchResults = new Dictionary<string, MarkdownFetchResult>();

        // Find all fetch tags
        var matches = FetchTagRegex().Matches(markdown);
        if (matches.Count == 0 && !FetchSummaryTagRegex().IsMatch(markdown))
            return markdown;

        var result = markdown;

        // Process each fetch tag in reverse order to preserve string positions
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];

            // Check if disabled
            var disabled = match.Groups[1].Success &&
                          match.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (disabled)
                continue; // Skip this tag, leave it as-is

            var url = match.Groups[2].Value;
            var pollFrequencyHours = TimeUnitParser.ParseToHours(match.Groups[3].Value);
            var transformLinks = match.Groups.Count > 4 &&
                                 match.Groups[4].Success &&
                                 match.Groups[4].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            var showSummary = match.Groups.Count > 5 &&
                              match.Groups[5].Success &&
                              match.Groups[5].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            var summaryTemplate = match.Groups.Count > 6 && match.Groups[6].Success
                ? match.Groups[6].Value
                : null;
            var cssClass = match.Groups.Count > 7 && match.Groups[7].Success
                ? match.Groups[7].Value
                : null;

            _logger?.LogInformation(
                "Preprocessor parsed <fetch> tag: url={Url}, pollFrequency={PollFrequency}h, transformLinks={TransformLinks}, showSummary={ShowSummary}, summaryTemplate={SummaryTemplate}, cssClass={CssClass}",
                url, pollFrequencyHours, transformLinks, showSummary, summaryTemplate ?? "(none)", cssClass ?? "(none)");

            var replacement = string.Empty;
            MarkdownFetchResult? fetchResult = null;

            if (_serviceProvider != null)
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var fetchService = scope.ServiceProvider.GetRequiredService<IMarkdownFetchService>();

                    fetchResult = fetchService.FetchMarkdownAsync(url, pollFrequencyHours, 0)
                        .GetAwaiter()
                        .GetResult();

                    // Store result for potential fetch-summary tags
                    fetchResults[url] = fetchResult;

                    if (fetchResult.Success)
                    {
                        replacement = fetchResult.Content;

                        // Transform relative links to absolute URLs pointing back to source
                        if (transformLinks)
                        {
                            replacement = MarkdownLinkRewriter.RewriteLinks(replacement, url);
                            _logger?.LogDebug("Transformed links in fetched markdown from {Url}", url);
                        }

                        // Append metadata summary if requested
                        if (showSummary)
                        {
                            var summary = FetchMetadataSummary.Format(fetchResult, summaryTemplate, cssClass);
                            if (!string.IsNullOrEmpty(summary))
                            {
                                replacement = replacement + "\n\n" + summary;
                                _logger?.LogDebug("Added metadata summary to fetched content from {Url}", url);
                            }
                        }

                        _logger?.LogInformation("Successfully fetched markdown from {Url}", url);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to fetch markdown from {Url}: {Error}", url,
                            fetchResult.ErrorMessage);
                        replacement = $"<!-- Failed to fetch content from {url}: {fetchResult.ErrorMessage} -->";
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error fetching markdown from {Url}", url);
                    replacement = $"<!-- Error fetching content from {url}: {ex.Message} -->";
                }
            else
                replacement = "<!-- Markdown fetch service not configured -->";

            // Replace the fetch tag with the fetched content
            result = string.Concat(result.AsSpan(0, match.Index), replacement, result.AsSpan(match.Index + match.Length));
        }

        // Now process fetch-summary tags
        var summaryMatches = FetchSummaryTagRegex().Matches(result);
        for (var i = summaryMatches.Count - 1; i >= 0; i--)
        {
            var match = summaryMatches[i];

            // Check if disabled
            var disabled = match.Groups[1].Success &&
                          match.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (disabled)
                continue; // Skip this tag, leave it as-is

            var url = match.Groups[2].Value;
            var template = match.Groups.Count > 3 && match.Groups[3].Success
                ? match.Groups[3].Value
                : null;
            var cssClass = match.Groups.Count > 4 && match.Groups[4].Success
                ? match.Groups[4].Value
                : null;

            _logger?.LogInformation(
                "Preprocessor parsed <fetch-summary> tag: url={Url}, template={Template}, cssClass={CssClass}",
                url, template ?? "(none)", cssClass ?? "(none)");

            string summaryReplacement;
            if (fetchResults.TryGetValue(url, out var fetchResult) && fetchResult.Success)
            {
                // Format the summary using the stored result
                summaryReplacement = FetchMetadataSummary.Format(fetchResult, template, cssClass);
                _logger?.LogDebug("Rendered fetch-summary for {Url}", url);
            }
            else
            {
                // No fetch result found for this URL
                summaryReplacement = $"<!-- No fetch data available for {url} -->";
                _logger?.LogWarning("No fetch result found for fetch-summary tag referencing {Url}", url);
            }

            // Replace the fetch-summary tag with the formatted summary
            result = string.Concat(result.AsSpan(0, match.Index), summaryReplacement,
                     result.AsSpan(match.Index + match.Length));
        }

        return result;
    }
}