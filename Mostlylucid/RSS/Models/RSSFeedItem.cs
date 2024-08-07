using Mostlylucid.Helpers;

namespace Mostlylucid.RSS.Models;

public class RssFeedItem
{
    public string Title { get; set; }
    public string Link { get; set; }
    public string Description { get; set; }
    public DateTime PubDate { get; set; }
    public string[] Categories { get; set; } // New property

    public Guid Guid => Title.ToGuid();
}