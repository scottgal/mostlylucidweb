using System.Text;
using System.Xml;
using System.Xml.Linq;
using Mostlylucid.RSS.Models;
using Mostlylucid.Services.Markdown;

namespace Mostlylucid.RSS;

public class RSSFeedService(BlogService blogService, IHttpContextAccessor httpContextAccessor, ILogger<RSSFeedService> logger)
{
    public string GetSiteUrl()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            logger.LogError("Request is null");
            return string.Empty;
        }
        return $"{request.Scheme}s://{request.Host}";
    }
    
    public string GenerateFeed(DateTime? startDate=null, string? category = null)
    {
        var items = blogService.GetPosts(startDate, category);
        List<RssFeedItem> rssFeedItems = new();
        foreach (var item in items)
        {
            rssFeedItems.Add(new RssFeedItem()
            {
                Title = item.Title,
                Link = $"{GetSiteUrl()}/blog/{item.Slug}",
                Description = item.Title,
                PubDate = item.PublishedDate,
                Categories = item.Categories
            });
        }
        return GenerateFeed(rssFeedItems, category);
    }
    public string GenerateFeed(IEnumerable<RssFeedItem> items, string categoryName = "")
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var feed = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",    new XAttribute(XNamespace.Xmlns + "atom", atom.NamespaceName), new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", !string.IsNullOrEmpty(categoryName) ? $"mostlylucid.net for {categoryName}" : $"mostlylucid.net"),
                    new XElement("link", $"{GetSiteUrl()}/rss"),
                    new XElement("description", "The latest posts from mostlylucid.net"),
                    new XElement("pubDate", DateTime.UtcNow.ToString("R")),
                    new XElement(atom + "link", 
                        new XAttribute("href", $"{GetSiteUrl()}/rss"), 
                        new XAttribute("rel", "self"), 
                        new XAttribute("type", "application/rss+xml")),
                    from item in items
                    select new XElement("item",
                        new XElement("title", item.Title),
                        new XElement("link", item.Link),
                        new XElement("guid", item.Guid, new XAttribute("isPermaLink", "false")),
                        new XElement("description", item.Description),
                        new XElement("pubDate", item.PubDate.ToString("R")),
                        from category in item.Categories
                        select new XElement("category", category)
                    )
                )
            )
        );

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false) // UTF-8 without BOM
        };

        using (var memoryStream = new MemoryStream())
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            feed.Save(writer);
            writer.Flush();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}