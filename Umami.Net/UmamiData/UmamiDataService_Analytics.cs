using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public enum AnalyticsEndpoint
{
    Attribution, Breakdown, FunnelStats, GoalStats, Heatmaps, Journeys, Retention,
    PerformanceStats, PerformanceChart, PerformanceMetrics, UtmMetrics,
    RevenueStats, RevenueChart, RevenueMetrics, Sessions, Events
}

public partial class UmamiDataService
{
    /// <summary>Execute a current analytics GET route, including endpoint-specific query criteria.</summary>
    public Task<UmamiResult<JsonElement>> GetAnalyticsAsync(AnalyticsEndpoint endpoint, AnalyticsRequest request,
        CancellationToken cancellationToken = default) => GetAnalyticsAsync<JsonElement>(endpoint, request, cancellationToken);

    /// <summary>Deserialize to a caller-selected result type, or use JsonElement for evolving analytics datasets.</summary>
    public Task<UmamiResult<T>> GetAnalyticsAsync<T>(AnalyticsEndpoint endpoint, AnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var route = endpoint switch
        {
            AnalyticsEndpoint.Attribution => "attribution", AnalyticsEndpoint.Breakdown => "breakdown",
            AnalyticsEndpoint.FunnelStats => "funnels/stats", AnalyticsEndpoint.GoalStats => "goals/stats",
            AnalyticsEndpoint.Heatmaps => "heatmaps", AnalyticsEndpoint.Journeys => "journeys",
            AnalyticsEndpoint.Retention => "retention", AnalyticsEndpoint.PerformanceStats => "performance/stats",
            AnalyticsEndpoint.PerformanceChart => "performance/chart", AnalyticsEndpoint.PerformanceMetrics => "performance/metrics",
            AnalyticsEndpoint.UtmMetrics => "utm/metrics", AnalyticsEndpoint.RevenueStats => "revenue/stats",
            AnalyticsEndpoint.RevenueChart => "revenue/chart", AnalyticsEndpoint.RevenueMetrics => "revenue/metrics",
            AnalyticsEndpoint.Sessions => "sessions", AnalyticsEndpoint.Events => "events",
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
        };
        return RequestAsync<T>(HttpMethod.Get, WebsitePath(route) + Query(request), cancellationToken: cancellationToken);
    }

    public Task<UmamiResult<PagedResponse<Annotation>>> GetAnnotationsAsync(DateTimeOffset? start = null,
        DateTimeOffset? end = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (start > end) throw new ArgumentException("Start must precede end");
        if (page < 1 || pageSize < 1) throw new ArgumentOutOfRangeException(nameof(page));
        var query = QueryHelpers.AddQueryString(WebsitePath("annotations"), new Dictionary<string, string?>
        {
            ["startAt"] = start?.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["endAt"] = end?.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        return RequestAsync<PagedResponse<Annotation>>(HttpMethod.Get, query, cancellationToken: cancellationToken);
    }

    public Task<UmamiResult<Annotation>> GetAnnotationAsync(string id, CancellationToken cancellationToken = default) =>
        RequestAsync<Annotation>(HttpMethod.Get, WebsitePath("annotations/" + ResourceId(id)), cancellationToken: cancellationToken);

    public Task<UmamiResult<Annotation>> CreateAnnotationAsync(AnnotationInput annotation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        annotation.Validate();
        return RequestAsync<Annotation>(HttpMethod.Post, WebsitePath("annotations"), annotation, cancellationToken);
    }

    public Task<UmamiResult<Annotation>> UpdateAnnotationAsync(string id, AnnotationInput annotation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        annotation.Validate();
        return RequestAsync<Annotation>(HttpMethod.Post, WebsitePath("annotations/" + ResourceId(id)), annotation, cancellationToken);
    }

    public Task<UmamiResult<bool>> DeleteAnnotationAsync(string id, CancellationToken cancellationToken = default) =>
        RequestAsync<bool>(HttpMethod.Delete, WebsitePath("annotations/" + ResourceId(id)), cancellationToken: cancellationToken);

    private static string ResourceId(string id)
    {
        if (!Guid.TryParseExact(id, "D", out _)) throw new ArgumentException("Resource ID must be a UUID", nameof(id));
        return id;
    }
}
