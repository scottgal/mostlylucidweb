using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>Website-scoped Umami 3.4 analytics query. Required criteria depend on the endpoint.</summary>
public class AnalyticsRequest : StatsRequest
{
    [QueryStringParameter("type")] public string? Type { get; set; }
    [QueryStringParameter("metric")] public string? Metric { get; set; }
    [QueryStringParameter("currency")] public string? Currency { get; set; }
    /// <summary>JSON array, for example [{"type":"path","value":"/pricing"}].</summary>
    [QueryStringParameter("steps")] public string? Steps { get; set; }
    [QueryStringParameter("fields")] public string? Fields { get; set; }
    [QueryStringParameter("window")] public int? Window { get; set; }
    [QueryStringParameter("model")] public string? Model { get; set; }
    [QueryStringParameter("step")] public string? Step { get; set; }
    [QueryStringParameter("value")] public string? Value { get; set; }
    [QueryStringParameter("startStep")] public string? StartStep { get; set; }
    [QueryStringParameter("endStep")] public string? EndStep { get; set; }
    [QueryStringParameter("urlPath")] public string? UrlPath { get; set; }
    [QueryStringParameter("mode")] public string? Mode { get; set; }
    [QueryStringParameter("page")] public int? Page { get; set; }
    [QueryStringParameter("pageSize")] public int? PageSize { get; set; }
    [QueryStringParameter("limit")] public int? Limit { get; set; }
    [QueryStringParameter("offset")] public int? Offset { get; set; }
    [QueryStringParameter("search")] public string? Search { get; set; }
}
