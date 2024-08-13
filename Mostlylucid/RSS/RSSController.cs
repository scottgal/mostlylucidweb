using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Mostlylucid.RSS;

[Microsoft.AspNetCore.Components.Route("rss")]
public class RssController(RSSFeedService rssFeedService, ILogger<RssController> logger) : Controller
{

    [HttpGet]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new string[] { nameof(category), nameof(startDate) }, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByQueryKeys = new string[] { nameof(category), nameof(startDate) })]

public async Task< IActionResult> Index([FromQuery] string category = null, [FromQuery] string startDate = null)
    {
        DateTime? startDateTime = null;
        if (DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime startDateTIme))
        {
            logger.LogInformation("Start date is {startDate}", startDate);
        }

        var rssFeed =await  rssFeedService.GenerateFeed(startDateTime, category);
        return Content(rssFeed, "application/rss+xml", Encoding.UTF8);
    }
}