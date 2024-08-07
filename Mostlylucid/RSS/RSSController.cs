using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace Mostlylucid.RSS;
[Microsoft.AspNetCore.Components.Route("rss")]
public class RssController(RSSFeedService rssFeedService, ILogger<RssController> logger) : Controller
{

    [HttpGet]
    public IActionResult Index([FromQuery] string category = null, [FromQuery] string startDate = null)
    {
        DateTime? startDateTime = null;
        if (DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime startDateTIme))
        {
            logger.LogInformation("Start date is {startDate}", startDate);
        }

        var rssFeed = rssFeedService.GenerateFeed(startDateTime, category);
        return Content(rssFeed, "application/rss+xml");
    }
}