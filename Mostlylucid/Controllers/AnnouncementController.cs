using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services.Announcement;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Models;
using Umami.Net;
using Umami.Net.Models;

namespace Mostlylucid.Controllers;

[Route("announcement")]
public class AnnouncementController : Controller
{
    private const string DismissedCookiePrefix = "announcement_dismissed_";
    private readonly IAnnouncementService _announcementService;
    private readonly ISiteContentService _siteContentService;
    private readonly UmamiBackgroundSender _umamiBackgroundSender;
    private readonly ILogger<AnnouncementController> _logger;

    public AnnouncementController(
        IAnnouncementService announcementService,
        ISiteContentService siteContentService,
        UmamiBackgroundSender umamiBackgroundSender,
        ILogger<AnnouncementController> logger)
    {
        _announcementService = announcementService;
        _siteContentService = siteContentService;
        _umamiBackgroundSender = umamiBackgroundSender;
        _logger = logger;
    }

    /// <summary>
    /// Get the announcement banner partial for the current language
    /// </summary>
    [HttpGet("banner")]
    public async Task<IActionResult> Banner([FromQuery] string language = "en", CancellationToken cancellationToken = default)
    {
        var announcement = await _announcementService.GetActiveAnnouncementAsync(language, cancellationToken);

        // A database announcement wins; otherwise fall back to Markdown/site/announcement.md
        var dto = announcement != null ? MapToDto(announcement) : GetSiteAnnouncement(language);
        if (dto == null)
        {
            return Content(string.Empty);
        }

        // Check if user has dismissed this announcement
        var cookieName = DismissedCookiePrefix + dto.Key;
        if (Request.Cookies.ContainsKey(cookieName))
        {
            return Content(string.Empty);
        }

        // Track announcement view
        await _umamiBackgroundSender.Track("announcement_view", new UmamiEventData
        {
            { "key", dto.Key },
            { "language", dto.Language }
        });

        return PartialView("_Announcement", dto);
    }

    /// <summary>
    /// Dismiss an announcement (sets a cookie)
    /// </summary>
    [HttpPost("dismiss")]
    public async Task<IActionResult> Dismiss([FromQuery] string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return BadRequest();
        }

        var cookieName = DismissedCookiePrefix + key;
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };

        Response.Cookies.Append(cookieName, "1", cookieOptions);
        _logger.LogDebug("User dismissed announcement {Key}", key);

        // Track announcement dismissal
        await _umamiBackgroundSender.Track("announcement_dismiss", new UmamiEventData
        {
            { "key", key }
        });

        return Ok();
    }

    private AnnouncementDto? GetSiteAnnouncement(string language)
    {
        var content = _siteContentService.Get(SiteContentService.Announcement);
        if (content == null) return null;

        return new AnnouncementDto
        {
            // Keyed on the content hash so editing the file re-shows it to people who dismissed it
            Key = $"site-{content.Hash}",
            HtmlContent = content.Html,
            Language = language,
            IsActive = true
        };
    }

    private static AnnouncementDto MapToDto(AnnouncementEntity entity)
    {
        return new AnnouncementDto
        {
            Id = entity.Id,
            Key = entity.Key,
            Markdown = entity.Markdown,
            HtmlContent = entity.HtmlContent,
            Language = entity.Language,
            IsActive = entity.IsActive,
            Priority = entity.Priority,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
