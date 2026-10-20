using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Models.Error;
using Mostlylucid.Services;
using Mostlylucid.Services.Blog;

namespace Mostlylucid.Controllers;

public class ErrorController(
    BaseControllerService baseControllerService,
    ILogger<ErrorController> logger,
    ISlugSuggestionService? slugSuggestionService = null) : BaseController(baseControllerService, logger)
{
    [Route("/error/{statusCode}")]
    [HttpGet]
    public async Task<IActionResult> HandleError(int statusCode, CancellationToken cancellationToken = default)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
            ViewData["StatusCode"] = statusCode;
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
                // HTMX does not swap error responses. Load the normal 404 page for navigation
                // requests so visitors can actually see and follow the suggested destinations.
                if (HttpMethods.IsGet(Request.Method) && Request.Headers["HX-Request"] == "true" &&
                    Request.Headers["HX-Target"] == "contentcontainer" && statusCodeReExecuteFeature != null)
                    Response.Headers["HX-Redirect"] = statusCodeReExecuteFeature.OriginalPath +
                        statusCodeReExecuteFeature.OriginalQueryString;
                // Check for high-confidence auto-redirect for first-time typos
                var autoRedirectResult = await TryAutoRedirectAsync(statusCodeReExecuteFeature, cancellationToken);
                if (autoRedirectResult != null)
                {
                    return autoRedirectResult;
                }

                var model = await CreateNotFoundModel(statusCodeReExecuteFeature, cancellationToken);
                return View("NotFound", model);
            case 500:
                return View("ServerError");
            default:
                return View("Error");
        }
    }

    private async Task<IActionResult?> TryAutoRedirectAsync(
        IStatusCodeReExecuteFeature? statusCodeReExecuteFeature,
        CancellationToken cancellationToken)
    {
        if (slugSuggestionService == null || statusCodeReExecuteFeature == null)
        {
            return null;
        }

        var originalPath = statusCodeReExecuteFeature.OriginalPath ?? string.Empty;

        // First, try to handle legacy archive URLs (e.g., /archite/2002/01/01/445.html)
        var archiveResult = await TryArchiveRedirectAsync(originalPath, cancellationToken);
        if (archiveResult != null)
        {
            return archiveResult;
        }

        var pathSegments = originalPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Only handle blog posts: /blog/{slug} or /blog/{language}/{slug}
        if (pathSegments.Length < 2 || !pathSegments[0].Equals("blog", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Handle legacy /blog/kategorie/{category} URLs -> redirect to /blog?category={category}
        if (pathSegments.Length >= 3 &&
            pathSegments[1].Equals("kategorie", StringComparison.OrdinalIgnoreCase))
        {
            var category = pathSegments[2];
            var redirectUrl = $"/blog?category={Uri.EscapeDataString(category)}";
            logger.LogInformation(
                "Kategorie redirect (301): {OriginalPath} -> {RedirectUrl}",
                originalPath, redirectUrl);
            return RedirectPermanent(redirectUrl);
        }

        string slug;
        var language = "en";

        // Known language codes for validation
        var validLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "en", "es", "fr", "de", "it", "nl", "pt", "pl", "sv", "fi", "ar", "el", "hi", "uk", "zh"
        };

        if (pathSegments.Length == 2)
        {
            slug = pathSegments[1];
        }
        else if (pathSegments.Length >= 3 && validLanguages.Contains(pathSegments[1]))
        {
            // Only treat as language/slug if the second segment is a known language
            language = pathSegments[1];
            slug = pathSegments[2];
        }
        else if (pathSegments.Length >= 3)
        {
            // Unknown second segment - treat the whole thing as a potentially malformed path
            slug = pathSegments[1];
        }
        else
        {
            return null;
        }

        try
        {
            // Check if the slug is a pure numeric ID (legacy blog post ID)
            if (int.TryParse(slug, out _))
            {
                logger.LogInformation("Detected numeric slug: {Slug}, attempting archive ID lookup", slug);
                var suggestions = await slugSuggestionService.GetSuggestionsForArchiveIdAsync(slug, language, 2, cancellationToken);

                if (suggestions.Count > 0)
                {
                    var topMatch = suggestions[0];

                    // Auto-redirect if score is high enough and there's a clear winner
                    if (topMatch.Score >= 0.85)
                    {
                        var hasSignificantGap = suggestions.Count == 1 || (suggestions[1].Score < topMatch.Score - 0.15);

                        if (hasSignificantGap)
                        {
                            var redirectUrl = language == "en"
                                ? $"/blog/{topMatch.Post.Slug}"
                                : $"/blog/{language}/{topMatch.Post.Slug}";

                            logger.LogInformation(
                                "Numeric slug auto-redirect (302): {OriginalPath} -> {RedirectUrl} (score: {Score:F2})",
                                originalPath, redirectUrl, topMatch.Score);

                            return Redirect(redirectUrl);
                        }
                    }
                }
            }

            // First check for learned redirects (user previously clicked a suggestion)
            // These get 301 Permanent Redirect as they're confirmed patterns
            var learnedTargetSlug = await slugSuggestionService.GetAutoRedirectSlugAsync(slug, language, cancellationToken);

            if (!string.IsNullOrWhiteSpace(learnedTargetSlug))
            {
                var redirectUrl = language == "en"
                    ? $"/blog/{learnedTargetSlug}"
                    : $"/blog/{language}/{learnedTargetSlug}";

                logger.LogInformation(
                    "Learned auto-redirect (301): {OriginalPath} -> {RedirectUrl}",
                    originalPath, redirectUrl);

                return RedirectPermanent(redirectUrl);
            }

            // Then check for high-confidence first-time matches
            // These get 302 Temporary Redirect until the pattern is confirmed by user clicks
            var firstTimeTargetSlug = await slugSuggestionService.GetFirstTimeAutoRedirectSlugAsync(slug, language, cancellationToken);

            if (!string.IsNullOrWhiteSpace(firstTimeTargetSlug))
            {
                var redirectUrl = language == "en"
                    ? $"/blog/{firstTimeTargetSlug}"
                    : $"/blog/{language}/{firstTimeTargetSlug}";

                logger.LogInformation(
                    "First-time auto-redirect (302): {OriginalPath} -> {RedirectUrl}",
                    originalPath, redirectUrl);

                return Redirect(redirectUrl);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking for auto-redirect for path: {Path}", originalPath);
        }

        return null;
    }

    private async Task<IActionResult?> TryArchiveRedirectAsync(string originalPath, CancellationToken cancellationToken)
    {
        // Extract archive identifier from legacy URLs like /archite/2002/01/01/445.html or /archive/post.aspx
        var archiveId = ExtractArchiveIdentifier(originalPath);
        if (string.IsNullOrEmpty(archiveId))
        {
            return null;
        }

        logger.LogInformation("Attempting archive redirect for path: {Path}, extracted ID: {ArchiveId}",
            originalPath, archiveId);

        try
        {
            // First, check if the extracted identifier is a valid slug that exists directly
            // This handles URLs like /archive/2003/06/06/more-passport-woes.aspx -> /blog/more-passport-woes
            if (!int.TryParse(archiveId, out _))
            {
                // It's not a numeric ID, so treat it as a potential slug
                var normalizedSlug = archiveId.ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
                var slugExists = await BlogViewService.EntryExists(normalizedSlug, "en");

                if (slugExists)
                {
                    var redirectUrl = $"/blog/{normalizedSlug}";
                    logger.LogInformation(
                        "Archive direct slug redirect (301): {OriginalPath} -> {RedirectUrl}",
                        originalPath, redirectUrl);

                    return RedirectPermanent(redirectUrl);
                }
            }

            // Fall back to fuzzy matching for numeric IDs or if direct slug not found
            var suggestions = await slugSuggestionService!.GetSuggestionsForArchiveIdAsync(archiveId, "en", 2, cancellationToken);

            if (suggestions.Count == 0)
            {
                logger.LogDebug("No archive matches found for ID: {ArchiveId}", archiveId);
                return null;
            }

            var topMatch = suggestions[0];

            // Auto-redirect if score is high enough and there's a clear winner
            if (topMatch.Score >= 0.85)
            {
                var hasSignificantGap = suggestions.Count == 1 || (suggestions[1].Score < topMatch.Score - 0.15);

                if (hasSignificantGap)
                {
                    var redirectUrl = $"/blog/{topMatch.Post.Slug}";
                    logger.LogInformation(
                        "Archive auto-redirect (302): {OriginalPath} -> {RedirectUrl} (score: {Score:F2})",
                        originalPath, redirectUrl, topMatch.Score);

                    return Redirect(redirectUrl);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during archive redirect for path: {Path}", originalPath);
        }

        return null;
    }

    /// <summary>
    /// Extract the archive identifier from a legacy URL
    /// E.g., "/archive/2002/01/01/445.html" -> "445"
    /// E.g., "/archive/2003/06/06/more-passport-woes.aspx" -> "more-passport-woes"
    /// E.g., "/archive/2011/01/24.aspx" -> null (date-only, no slug)
    /// </summary>
    private static string? ExtractArchiveIdentifier(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Check if it's a legacy URL (ends with .html or .aspx)
        if (!path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
            !path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Check if this is an archive URL pattern
        var isArchivePath = path.StartsWith("/archive", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith("/archite", StringComparison.OrdinalIgnoreCase);

        if (!isArchivePath)
        {
            return null;
        }

        // Find the last slash
        var lastSlashIndex = path.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex >= path.Length - 1)
        {
            return null;
        }

        // Extract the filename (between last / and .html/.aspx)
        var filename = path[(lastSlashIndex + 1)..];

        // Remove the extension
        var dotIndex = filename.LastIndexOf('.');
        if (dotIndex > 0)
        {
            filename = filename[..dotIndex];
        }

        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        // Check if this looks like a date-only pattern (e.g., /archive/2011/01/24.aspx)
        // If the filename is just a 1-2 digit number and the path has 4 segments like /archive/YYYY/MM/DD
        // then it's likely a date, not a post ID
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 4 &&
            int.TryParse(filename, out var num) &&
            num >= 1 && num <= 31)
        {
            // This looks like /archive/YYYY/MM/DD.aspx - a date-only URL with no actual slug
            // Return null as we can't determine what post this refers to
            return null;
        }

        return filename;
    }

    private async Task<NotFoundModel> CreateNotFoundModel(
        IStatusCodeReExecuteFeature? statusCodeReExecuteFeature,
        CancellationToken cancellationToken)
    {
        var model = new NotFoundModel
        {
            OriginalPath = statusCodeReExecuteFeature?.OriginalPath ?? string.Empty,
            SuggestionsWithScores = new List<Mostlylucid.Models.Error.SuggestionWithScore>()
        };

        if (string.IsNullOrWhiteSpace(model.OriginalPath)) return model;
        if (slugSuggestionService == null)
        {
            model.SuggestionsWithScores = Mostlylucid.Helpers.MissingPageSuggestions.Create(
                await BlogViewService.GetAllPosts(), model.OriginalPath);
            return model;
        }

        try
        {
            // First, check if this is a legacy archive URL
            var archiveId = ExtractArchiveIdentifier(model.OriginalPath);
            if (!string.IsNullOrEmpty(archiveId))
            {
                logger.LogInformation("Searching for archive suggestions for ID: {ArchiveId}", archiveId);
                var archiveSuggestions = await slugSuggestionService.GetSuggestionsForArchiveIdAsync(archiveId, "en", 5, cancellationToken);
                if (archiveSuggestions.Count > 0)
                {
                    model.SuggestionsWithScores = archiveSuggestions.Select(s => new Mostlylucid.Models.Error.SuggestionWithScore
                    {
                        Post = s.Post,
                        Score = s.Score
                    }).ToList();
                    logger.LogInformation("Found {Count} archive suggestions for ID: {ArchiveId}", model.SuggestionsWithScores.Count, archiveId);
                    return model;
                }
            }

            // Extract slug from path (e.g., /blog/my-post-slug or /blog/en/my-post-slug)
            var pathSegments = model.OriginalPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            string? slug = null;
            var language = "en"; // Default language

            // Pattern: /blog/{slug} or /blog/{language}/{slug}
            if (pathSegments.Length >= 2 && pathSegments[0].Equals("blog", StringComparison.OrdinalIgnoreCase))
            {
                if (pathSegments.Length == 2)
                {
                    // /blog/{slug}
                    slug = pathSegments[1];
                }
                else if (pathSegments.Length >= 3)
                {
                    // /blog/{language}/{slug}
                    language = pathSegments[1];
                    slug = pathSegments[2];
                }
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                // Check if the slug is a pure numeric ID (legacy blog post ID)
                if (int.TryParse(slug, out _))
                {
                    logger.LogInformation("Searching for archive ID suggestions for numeric slug: {Slug}", slug);
                    var archiveSuggestionsForNumeric = await slugSuggestionService.GetSuggestionsForArchiveIdAsync(slug, language, 5, cancellationToken);
                    if (archiveSuggestionsForNumeric.Count > 0)
                    {
                        model.SuggestionsWithScores = archiveSuggestionsForNumeric.Select(s => new Mostlylucid.Models.Error.SuggestionWithScore
                        {
                            Post = s.Post,
                            Score = s.Score
                        }).ToList();
                        logger.LogInformation("Found {Count} archive ID suggestions for numeric slug: {Slug}", model.SuggestionsWithScores.Count, slug);
                        return model;
                    }
                }

                logger.LogInformation("Searching for suggestions for slug: {Slug} in language: {Language}", slug, language);
                var suggestionsWithScores = await slugSuggestionService.GetSuggestionsWithScoreAsync(slug, language, 5, cancellationToken);
                model.SuggestionsWithScores = suggestionsWithScores.Select(s => new Mostlylucid.Models.Error.SuggestionWithScore
                {
                    Post = s.Post,
                    Score = s.Score
                }).ToList();
                logger.LogInformation("Found {Count} suggestions for slug: {Slug}", model.SuggestionsWithScores.Count, slug);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating slug suggestions for path: {Path}", model.OriginalPath);
        }

        if (model.SuggestionsWithScores.Count == 0)
            model.SuggestionsWithScores = Mostlylucid.Helpers.MissingPageSuggestions.Create(
                await BlogViewService.GetAllPosts(), model.OriginalPath);
        return model;
    }
}
