using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Request model for page views over time endpoint.
/// API spec: Required: websiteId, startAt, endAt, unit. Optional: timezone, compare. Filters: path, referrer, title, query, browser, os, device, country, region, city, hostname, tag, segment, cohort
/// </summary>
public class PageViewsRequest : BaseRequest
{
    [QueryStringParameter("language")] public string? Language { get; set; }

    /// <summary>
    /// Required: Time unit for grouping data.
    /// </summary>
    [QueryStringParameter("unit", true)]
    public Unit Unit { get; set; } = Unit.day;

    /// <summary>
    /// Optional: Timezone for date calculations.
    /// </summary>
    [QueryStringParameter("timezone")]
    [TimeZoneValidator]
    public string? Timezone { get; set; }

    /// <summary>
    /// Optional: Comparison mode (prev = previous period, yoy = year over year).
    /// </summary>
    [QueryStringParameter("compare")]
    public string? Compare { get; set; }

    // Filter properties - use "path" not "url", "hostname" not "host"
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

    [QueryStringParameter("os")]
    public string? Os { get; set; }

    [QueryStringParameter("browser")]
    public string? Browser { get; set; }

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