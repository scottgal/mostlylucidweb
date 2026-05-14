using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Shared.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;

namespace Mostlylucid.Services.Images;

/// <summary>
/// Service for downloading external images from blog posts and serving them locally
/// </summary>
public partial class ExternalImageDownloadService
{
    private readonly MostlylucidDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalImageDownloadService> _logger;
    private readonly string _imageStoragePath;
    private readonly HashSet<string> _allowedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add domains to whitelist for downloading (optional safety measure)
        // Empty means all domains allowed
    };

    public ExternalImageDownloadService(
        MostlylucidDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalImageDownloadService> logger,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _imageStoragePath = Path.Combine(environment.WebRootPath, "externalimages");

        // Ensure directory exists
        Directory.CreateDirectory(_imageStoragePath);
    }

    [GeneratedRegex(@"<img\s+([^>]*\s+)?src=[""']([^""']+)[""']([^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"\s+(width|height)=[""']?\d+[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex SizeAttributeRegex();

    /// <summary>
    /// Process a blog post to download external images
    /// </summary>
    public async Task ProcessPostAsync(BlogPostEntity post, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(post.HtmlContent))
        {
            _logger.LogDebug("Post {Slug} has no HTML content, skipping", post.Slug);
            return;
        }

        // First, revert any previously-inlined entries that are now invalid:
        //  - the OriginalUrl is now in the badge skip list, or
        //  - the local file is missing, or
        //  - the local file's bytes don't look like a real image.
        // Reverting rewrites the post HTML back to the OriginalUrl and removes the DB row.
        var revertedHtml = await RevertInvalidLocalImagesAsync(post, cancellationToken);
        if (!ReferenceEquals(revertedHtml, post.HtmlContent))
        {
            post.HtmlContent = revertedHtml;
        }

        var externalImages = ExtractExternalImages(post.HtmlContent);
        if (externalImages.Count == 0)
        {
            _logger.LogDebug("Post {Slug} has no external images", post.Slug);
            await MarkAllImagesVerified(post.Slug, cancellationToken);
            return;
        }

        _logger.LogInformation("Processing {Count} external images for post {Slug}", externalImages.Count, post.Slug);

        var verifiedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedHtml = post.HtmlContent;

        foreach (var imageUrl in externalImages)
        {
            try
            {
                verifiedUrls.Add(imageUrl);

                // Check if already downloaded
                var existing = await _dbContext.DownloadedImages
                    .FirstOrDefaultAsync(x => x.PostSlug == post.Slug && x.OriginalUrl == imageUrl, cancellationToken);

                if (existing != null)
                {
                    // Update verification date
                    existing.LastVerifiedDate = DateTimeOffset.UtcNow;
                    _logger.LogDebug("Image already downloaded: {Url} -> {LocalFile}", imageUrl, existing.LocalFileName);

                    // Update HTML to use local URL
                    updatedHtml = ReplaceImageUrl(updatedHtml, imageUrl, $"/externalimages/{existing.LocalFileName}");
                    continue;
                }

                // Download new image
                var downloadedImage = await DownloadImageAsync(imageUrl, post.Slug, cancellationToken);
                if (downloadedImage != null)
                {
                    _dbContext.DownloadedImages.Add(downloadedImage);
                    updatedHtml = ReplaceImageUrl(updatedHtml, imageUrl, $"/externalimages/{downloadedImage.LocalFileName}");
                    _logger.LogInformation("Downloaded image: {Url} -> {LocalFile}", imageUrl, downloadedImage.LocalFileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image {Url} for post {Slug}", imageUrl, post.Slug);
            }
        }

        // Update post HTML if changed
        if (updatedHtml != post.HtmlContent)
        {
            post.HtmlContent = updatedHtml;
            _logger.LogInformation("Updated HTML for post {Slug} with local image URLs", post.Slug);
        }

        // Mark images as verified
        await MarkImagesVerified(post.Slug, verifiedUrls, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Download an external image and save it locally, with archive.org fallback
    /// </summary>
    private async Task<DownloadedImageEntity?> DownloadImageAsync(string url, string postSlug, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Try original URL first
            var response = await TryDownloadFromUrl(client, url, cancellationToken);

            // If original fails, try archive.org Wayback Machine
            if (response == null || !response.IsSuccessStatusCode)
            {
                var archiveUrl = $"https://web.archive.org/web/0/{url}";
                _logger.LogInformation("Original URL failed, trying archive.org: {ArchiveUrl}", archiveUrl);
                response = await TryDownloadFromUrl(client, archiveUrl, cancellationToken);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image {Url} (also tried archive.org)", url);
                    return null;
                }
                _logger.LogInformation("Successfully retrieved image from archive.org for {Url}", url);
            }

            // Check Content-Type header first
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            // Reject obvious non-image content types up front
            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("URL {Url} returned non-image content type: {ContentType} - likely a 404 or error page", url, contentType);
                return null;
            }

            // Check content length - reject files over 10MB
            if (response.Content.Headers.ContentLength.HasValue)
            {
                var sizeInMB = response.Content.Headers.ContentLength.Value / (1024.0 * 1024.0);
                if (sizeInMB > 10)
                {
                    _logger.LogWarning("Image {Url} is too large: {Size:F2}MB - skipping", url, sizeInMB);
                    return null;
                }
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // Determine actual content format. Order matters: SVG sniff first (it's XML
            // text, would otherwise be misclassified as HTML), then HTML reject, then
            // binary magic bytes.
            string detectedFormat;
            var isSvg = contentType.Contains("image/svg", StringComparison.OrdinalIgnoreCase)
                        || IsSvgContent(imageBytes);

            if (isSvg)
            {
                detectedFormat = "svg";
            }
            else if (IsHtmlContent(imageBytes))
            {
                _logger.LogWarning("URL {Url} contains HTML content (404/error page) - skipping", url);
                return null;
            }
            else if (!HasValidImageMagicBytes(imageBytes, out detectedFormat))
            {
                _logger.LogWarning("URL {Url} does not have valid image magic bytes - likely not an image file", url);
                return null;
            }

            // Always derive extension from detected content, never from the URL.
            // (A URL ending in .jpg may serve SVG/HTML/etc; trusting the URL caused
            // files to be served with the wrong content-type and break in browsers.)
            var extension = ExtensionForFormat(detectedFormat);

            // Sanitize filename
            var originalFileName = Path.GetFileName(new Uri(url).LocalPath);
            var sanitizedName = SanitizeFileName(Path.GetFileNameWithoutExtension(originalFileName));
            if (string.IsNullOrEmpty(sanitizedName)) sanitizedName = "image";
            var localFileName = $"{postSlug}-{sanitizedName}{extension}";

            // Ensure unique filename
            var fullPath = Path.Combine(_imageStoragePath, localFileName);
            var counter = 1;
            while (File.Exists(fullPath))
            {
                localFileName = $"{postSlug}-{sanitizedName}-{counter}{extension}";
                fullPath = Path.Combine(_imageStoragePath, localFileName);
                counter++;
            }

            // Validate image before saving. SVG can't be loaded by ImageSharp - we
            // already validated it via IsSvgContent so accept it without dimensions.
            int? width = null;
            int? height = null;
            if (!isSvg)
            {
                try
                {
                    using var memoryStream = new MemoryStream(imageBytes);
                    var format = Image.DetectFormat(memoryStream);
                    if (format == null)
                    {
                        _logger.LogWarning("Could not detect image format for {Url} - skipping", url);
                        return null;
                    }

                    memoryStream.Position = 0;
                    using var image = Image.Load(memoryStream);
                    width = image.Width;
                    height = image.Height;

                    if (width <= 0 || height <= 0)
                    {
                        _logger.LogWarning("Image has invalid dimensions for {Url}: {Width}x{Height} - skipping", url, width, height);
                        return null;
                    }
                }
                catch (UnknownImageFormatException ex)
                {
                    _logger.LogWarning("Unknown image format for {Url}: {Message} - skipping", url, ex.Message);
                    return null;
                }
                catch (InvalidImageContentException ex)
                {
                    _logger.LogWarning("Invalid/corrupt image content for {Url}: {Message} - skipping", url, ex.Message);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not validate image for {Url} - skipping", url);
                    return null;
                }
            }

            // Save file (only if validation passed)
            await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);
            _logger.LogInformation("Saved image to {Path} ({Size} bytes)", fullPath, imageBytes.Length);

            return new DownloadedImageEntity
            {
                PostSlug = postSlug,
                OriginalUrl = url,
                LocalFileName = localFileName,
                DownloadedDate = DateTimeOffset.UtcNow,
                LastVerifiedDate = DateTimeOffset.UtcNow,
                FileSize = imageBytes.Length,
                ContentType = contentType,
                Width = width,
                Height = height
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Extract external image URLs from HTML content
    /// </summary>
    private List<string> ExtractExternalImages(string html)
    {
        var externalUrls = new List<string>();
        var matches = ImgTagRegex().Matches(html);

        foreach (Match match in matches)
        {
            var url = match.Groups[2].Value;

            // Skip data URLs, relative URLs, and already local URLs
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("/", StringComparison.Ordinal) ||
                url.StartsWith("./", StringComparison.Ordinal) ||
                url.StartsWith("../", StringComparison.Ordinal))
            {
                continue;
            }

            // Must be absolute HTTP/HTTPS URL
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Skip badge/status hosts - they are dynamic and must never be inlined
                if (IsBadgeHost(uri))
                {
                    _logger.LogDebug("Skipping badge URL: {Url}", url);
                    continue;
                }

                externalUrls.Add(url);
            }
        }

        return externalUrls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Replace an image URL in HTML and remove width/height attributes
    /// </summary>
    private string ReplaceImageUrl(string html, string oldUrl, string newUrl)
    {
        // Find all img tags with this URL
        var pattern = $@"<img\s+([^>]*\s+)?src=[""']{Regex.Escape(oldUrl)}[""']([^>]*)>";
        return Regex.Replace(html, pattern, match =>
        {
            var fullTag = match.Value;

            // Remove width and height attributes
            fullTag = SizeAttributeRegex().Replace(fullTag, "");

            // Replace URL
            fullTag = fullTag.Replace(oldUrl, newUrl);

            return fullTag;
        }, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Walk the DB rows for this post and revert any entry whose OriginalUrl now
    /// matches the badge skip list or whose local file is missing/corrupt. Returns
    /// the (possibly updated) HTML.
    /// </summary>
    private async Task<string> RevertInvalidLocalImagesAsync(BlogPostEntity post, CancellationToken cancellationToken)
    {
        var html = post.HtmlContent ?? string.Empty;

        var rows = await _dbContext.DownloadedImages
            .Where(x => x.PostSlug == post.Slug)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return html;

        foreach (var row in rows)
        {
            var localPath = Path.Combine(_imageStoragePath, row.LocalFileName);
            var isBadgeUrl = Uri.TryCreate(row.OriginalUrl, UriKind.Absolute, out var uri) && IsBadgeHost(uri);
            var isMissing = !File.Exists(localPath);
            var isCorrupt = !isMissing && !IsLocalFileValidImage(localPath);

            if (!isBadgeUrl && !isMissing && !isCorrupt) continue;

            _logger.LogInformation(
                "Reverting inlined image for post {Slug}: {Local} -> {Original} (badge={Badge}, missing={Missing}, corrupt={Corrupt})",
                post.Slug, row.LocalFileName, row.OriginalUrl, isBadgeUrl, isMissing, isCorrupt);

            // Rewrite HTML back to the original URL
            html = ReplaceImageUrl(html, $"/externalimages/{row.LocalFileName}", row.OriginalUrl);

            // Delete the local file (if it lives under our storage path) and the DB row
            if (!isMissing)
            {
                try
                {
                    var fullStoragePath = Path.GetFullPath(_imageStoragePath);
                    var fullLocalPath = Path.GetFullPath(localPath);
                    if (fullLocalPath.StartsWith(fullStoragePath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(fullLocalPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete stale local image {Path}", localPath);
                }
            }

            _dbContext.DownloadedImages.Remove(row);
        }

        return html;
    }

    /// <summary>
    /// Sniff a saved local file to confirm it is actually an image (matching magic
    /// bytes or recognisable SVG). Used to detect previously-saved corrupt files.
    /// </summary>
    private static bool IsLocalFileValidImage(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var buffer = new byte[2048];
            var read = fs.Read(buffer, 0, buffer.Length);
            if (read <= 0) return false;
            var span = buffer.AsSpan(0, read).ToArray();

            if (IsSvgContent(span)) return true;
            if (HasValidImageMagicBytes(span, out _)) return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Mark images as verified for a post
    /// </summary>
    private async Task MarkImagesVerified(string postSlug, HashSet<string> verifiedUrls, CancellationToken cancellationToken)
    {
        var images = await _dbContext.DownloadedImages
            .Where(x => x.PostSlug == postSlug && verifiedUrls.Contains(x.OriginalUrl))
            .ToListAsync(cancellationToken);

        foreach (var image in images)
        {
            image.LastVerifiedDate = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Mark all images for a post as verified (when post has no external images)
    /// </summary>
    private async Task MarkAllImagesVerified(string postSlug, CancellationToken cancellationToken)
    {
        var images = await _dbContext.DownloadedImages
            .Where(x => x.PostSlug == postSlug)
            .ToListAsync(cancellationToken);

        foreach (var image in images)
        {
            image.LastVerifiedDate = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Clean up orphaned images (not verified in the last N days)
    /// </summary>
    public async Task CleanupOrphanedImagesAsync(int daysOld = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysOld);

        var orphanedImages = await _dbContext.DownloadedImages
            .Where(x => x.LastVerifiedDate < cutoffDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} orphaned images older than {Days} days", orphanedImages.Count, daysOld);

        foreach (var image in orphanedImages)
        {
            try
            {
                var filePath = Path.Combine(_imageStoragePath, image.LocalFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted orphaned image file: {FileName}", image.LocalFileName);
                }

                _dbContext.DownloadedImages.Remove(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete orphaned image {FileName}", image.LocalFileName);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cleanup complete. Removed {Count} orphaned images", orphanedImages.Count);
    }

    /// <summary>
    /// Try to download from a URL, returning null on failure instead of throwing
    /// </summary>
    private async Task<HttpResponseMessage?> TryDownloadFromUrl(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "HTTP request failed for {Url}", url);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Request timed out for {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Sanitize filename for safe storage
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        return sanitized.ToLowerInvariant();
    }

    /// <summary>
    /// Get statistics about downloaded images
    /// </summary>
    public async Task<(int TotalImages, long TotalSize, int PostsWithImages)> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalImages = await _dbContext.DownloadedImages.CountAsync(cancellationToken);
        var totalSize = await _dbContext.DownloadedImages.SumAsync(x => x.FileSize, cancellationToken);
        var postsWithImages = await _dbContext.DownloadedImages
            .Select(x => x.PostSlug)
            .Distinct()
            .CountAsync(cancellationToken);

        return (totalImages, totalSize, postsWithImages);
    }

    /// <summary>
    /// Check if content is HTML by looking for specific HTML markers in first 512 bytes.
    /// Intentionally narrow so we don't false-positive on SVG/XML.
    /// </summary>
    private static bool IsHtmlContent(byte[] content)
    {
        if (content.Length < 10) return false;

        var checkLength = Math.Min(512, content.Length);
        var textContent = System.Text.Encoding.UTF8.GetString(content, 0, checkLength).ToLowerInvariant();

        return textContent.Contains("<!doctype html") ||
               textContent.Contains("<html") ||
               textContent.Contains("<head>") ||
               textContent.Contains("<head ") ||
               textContent.Contains("<body>") ||
               textContent.Contains("<body ");
    }

    /// <summary>
    /// Detect SVG content. SVG is XML text with no fixed binary signature, so we sniff
    /// the start of the payload past any BOM, XML prolog, comments or DOCTYPE.
    /// </summary>
    private static bool IsSvgContent(byte[] content)
    {
        if (content.Length < 5) return false;

        var checkLength = Math.Min(2048, content.Length);
        var text = System.Text.Encoding.UTF8.GetString(content, 0, checkLength);
        text = text.TrimStart('﻿', ' ', '\t', '\r', '\n');

        while (text.Length > 0)
        {
            if (text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                var end = text.IndexOf("?>", StringComparison.Ordinal);
                if (end < 0) return false;
                text = text.Substring(end + 2).TrimStart();
                continue;
            }
            if (text.StartsWith("<!--", StringComparison.Ordinal))
            {
                var end = text.IndexOf("-->", StringComparison.Ordinal);
                if (end < 0) return false;
                text = text.Substring(end + 3).TrimStart();
                continue;
            }
            if (text.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            {
                var end = text.IndexOf('>');
                if (end < 0) return false;
                text = text.Substring(end + 1).TrimStart();
                continue;
            }
            break;
        }

        return text.StartsWith("<svg>", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<svg ", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("<svg", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<svg\t", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<svg\n", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<svg\r", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtensionForFormat(string format) => format switch
    {
        "jpeg" or "jpg" => ".jpg",
        "png" => ".png",
        "gif" => ".gif",
        "webp" => ".webp",
        "bmp" => ".bmp",
        "tiff" => ".tiff",
        "ico" => ".ico",
        "svg" => ".svg",
        _ => ".bin"
    };

    /// <summary>
    /// Hosts/paths whose responses are dynamic (badges, status images, counters) and
    /// should never be inlined as static files.
    /// </summary>
    private static bool IsBadgeHost(Uri uri)
    {
        var host = uri.Host;
        var path = uri.AbsolutePath;

        if (host.Equals("shields.io", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".shields.io", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.Equals("badge.fury.io", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.Equals("badgen.net", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".badgen.net", StringComparison.OrdinalIgnoreCase))
            return true;

        if (host.Equals("codecov.io", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".codecov.io", StringComparison.OrdinalIgnoreCase))
            return true;

        // GitHub status/badge images (workflow badges, etc.)
        if ((host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
             host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase)) &&
            (path.Contains("/badge", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith("/badge.svg", StringComparison.OrdinalIgnoreCase)))
            return true;

        // NuGet dynamic badge endpoints
        if ((host.Equals("nuget.org", StringComparison.OrdinalIgnoreCase) ||
             host.EndsWith(".nuget.org", StringComparison.OrdinalIgnoreCase)) &&
            path.Contains("/badge", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Validate image format by checking magic bytes (file signatures)
    /// Returns true if valid image format detected
    /// </summary>
    private static bool HasValidImageMagicBytes(byte[] data, out string format)
    {
        format = string.Empty;

        if (data.Length < 4) return false;

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            format = "jpeg";
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
        {
            format = "png";
            return true;
        }

        // GIF: GIF87a or GIF89a
        if (data.Length >= 6 &&
            data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 &&
            data[3] == 0x38 && (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
        {
            format = "gif";
            return true;
        }

        // WebP: RIFF....WEBP
        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
        {
            format = "webp";
            return true;
        }

        // BMP: BM
        if (data[0] == 0x42 && data[1] == 0x4D)
        {
            format = "bmp";
            return true;
        }

        // TIFF: II or MM
        if (data.Length >= 4 &&
            ((data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) ||
             (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A)))
        {
            format = "tiff";
            return true;
        }

        // ICO: 00 00 01 00
        if (data.Length >= 4 &&
            data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00)
        {
            format = "ico";
            return true;
        }

        return false;
    }
}
