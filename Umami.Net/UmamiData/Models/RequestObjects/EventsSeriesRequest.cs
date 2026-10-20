using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Request model for the /api/websites/:websiteId/events/series endpoint.
/// API spec: Required: websiteId, startAt, endAt, unit. Optional: timezone. Filters: path, referrer, title, query, browser, os, device, country, region, city, hostname, tag, segment, cohort
/// </summary>
public class EventsSeriesRequest : BaseRequest
{
    [QueryStringParameter("limit")] public int? Limit { get; set; }
    [QueryStringParameter("language")] public string? Language { get; set; }

    /// <summary>
    /// Required: Time unit for data bucketing (minute, hour, day, month, year)
    /// </summary>
    [QueryStringParameter("unit", true)]
    public Unit Unit { get; set; } = Unit.day;

    /// <summary>
    /// Optional: Timezone for the data (e.g., "America/Los_Angeles")
    /// </summary>
    [QueryStringParameter("timezone")]
    [TimeZoneValidator]
    public string? Timezone { get; set; } = "UTC";

    /// <summary>
    /// Optional: Event name to filter by (for filtering specific events)
    /// Note: This is not in the official API docs filters list, but may be supported
    /// </summary>
    [QueryStringParameter("event")]
    public string? EventName { get; set; }

    // Filter parameters
    [QueryStringParameter("path")]
    public string? Path { get; set; }

    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; }

    [QueryStringParameter("title")]
    public string? Title { get; set; }

    [QueryStringParameter("query")]
    public string? Query { get; set; }

    [QueryStringParameter("hostname")]
    public string? Hostname { get; set; }

    [QueryStringParameter("browser")]
    public string? Browser { get; set; }

    [QueryStringParameter("os")]
    public string? Os { get; set; }

    [QueryStringParameter("device")]
    public string? Device { get; set; }

    [QueryStringParameter("country")]
    public string? Country { get; set; }

    [QueryStringParameter("region")]
    public string? Region { get; set; }

    [QueryStringParameter("city")]
    public string? City { get; set; }

    [QueryStringParameter("tag")]
    public string? Tag { get; set; }

    [QueryStringParameter("segment")]
    public string? Segment { get; set; }

    [QueryStringParameter("cohort")]
    public string? Cohort { get; set; }
}
