﻿using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Services.Markdown;

namespace Mostlylucid.Controllers;

public class SiteMapController(
    IBlogViewService blogViewService,
    UmamiBackgroundSender backgroundSender,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SiteMapController> logger) : Controller
{
    private string GetSiteUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            logger.LogError("Request is null");
            return string.Empty;
        }

        return $"https://{request.Host}";
    }

    [HttpGet]
    [ResponseCache(Duration = 43200, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 43200)]
    public async Task<IActionResult> Index()
    {
        try
        {
            var pages = await blogViewService.GetPosts();

            List<SiteMapPage> siteMapPages = new();
            siteMapPages.Add(new SiteMapPage(Url.Action("Index", "Home"), DateTime.UtcNow, 1, ChangeFrequency.Daily));
            siteMapPages.AddRange(pages.Select(x =>
                new SiteMapPage(x.Language == MarkdownBaseService.EnglishLanguage
                        ? Url.Action("Show", "Blog", new { x.Slug })
                        : Url.Action("Language", "Blog", new { x.Slug, x.Language })
                    , x.PublishedDate, x.Language == MarkdownBaseService.EnglishLanguage ? 1 : 0.8,
                    ChangeFrequency.Daily)).ToList());


            XNamespace sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var feed = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(sitemap + "urlset", // Namespace for urlset
                    from item in siteMapPages
                    select new XElement(sitemap + "url", // Apply the namespace here as well
                        new XElement(sitemap + "loc", GetSiteUrl() + item.Loc),
                        new XElement(sitemap + "lastmod", item.LastMod.ToString("yyyy-MM-dd")),
                        new XElement(sitemap + "priority", item.Priority.ToString("F1", CultureInfo.InvariantCulture)),
                        new XElement(sitemap + "changefreq", item.ChangeFrequency.ToString().ToLower())
                    )
                )
            );
            await backgroundSender.TrackPageView("/sitemap.xml", "Sitemap");
            return Content(feed.ToString(), "text/xml");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error generating sitemap");
            return StatusCode(500);
        }
    }


    private record SiteMapPage(string Loc, DateTime LastMod, double Priority, ChangeFrequency ChangeFrequency);

    private enum ChangeFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never
    }
}