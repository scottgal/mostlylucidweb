using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Request model for retrieving summary statistics from Umami.
/// </summary>
/// <remarks>
/// According to Umami API docs, stats endpoint has:
/// - Required: websiteId, startAt, endAt
/// - Optional: unit, timezone
/// - Filters: path, referrer, title, query, browser, os, device, country, region, city, hostname, tag, segment, cohort
/// </remarks>
public class StatsRequest : BaseRequest
{
    [QueryStringParameter("compare")] public string? Compare { get; set; }
    [QueryStringParameter("language")] public string? Language { get; set; }
    [QueryStringParameter("event")] public string? Event { get; set; }
    /// <summary>
    /// Path filter (not "url"). Must use "path" as per Umami API specification.
    /// </summary>
    [QueryStringParameter("path")]
    public string? Path { get; set; }

    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; }

    [QueryStringParameter("title")]
    public string? Title { get; set; }

    [QueryStringParameter("query")]
    public string? Query { get; set; }

    /// <summary>
    /// Hostname filter (not "host"). Must use "hostname" as per Umami API specification.
    /// </summary>
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

    /// <summary>
    /// Optional unit parameter for stats grouping.
    /// </summary>
    [QueryStringParameter("unit")]
    public Unit? Unit { get; set; }

    /// <summary>
    /// Optional timezone for date calculations.
    /// </summary>
    [QueryStringParameter("timezone")]
    public string? Timezone { get; set; }
}