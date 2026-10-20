using System.Net;
using System.Text.Json;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.Test.Integration;

[Collection("Disposable Umami")]
public partial class Umami34IntegrationTests(UmamiServerFixture server)
{
    [DisposableUmamiFact]
    public async Task StatsAndTimeSeriesReportPersistedEventsAndVisitors()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        foreach (var stats in new[] { await site.Data.GetStats(site.Start, site.End), await site.Data.GetStats(site.Range()) })
        {
            Assert.Equal(HttpStatusCode.OK, stats.Status);
            Assert.Equal(3, stats.Data!.pageviews.value);
            Assert.Equal(2, stats.Data.visitors.value);
        }
        var pageviews = await site.Data.GetPageViews(site.Start, site.End);
        Assert.Equal(HttpStatusCode.OK, pageviews.Status);
        Assert.Equal(3, pageviews.Data!.pageviews.Sum(point => point.y));
        var minuteViews = await site.Data.GetPageViews(new() { StartAtDate = site.Start, EndAtDate = site.End, Unit = Unit.minute, Timezone = "UTC" });
        Assert.Equal(3, minuteViews.Data!.pageviews.Sum(point => point.y));
        var events = await site.Data.GetEventsSeries(site.Start, site.End, Unit.minute);
        Assert.Equal(HttpStatusCode.OK, events.Status);
        Assert.Equal(1, events.Data!.Single(point => point.x == "purchase").y);
        var eventRequest = await site.Data.GetEventsSeries(new() { StartAtDate = site.Start, EndAtDate = site.End, EventName = "purchase" });
        Assert.Equal(1, eventRequest.Data!.Sum(point => point.y));
        var active = await site.Data.GetActiveUsers();
        Assert.Equal(HttpStatusCode.OK, active.Status);
        Assert.Equal(2, active.Data!.ActiveUsers);
    }

    [DisposableUmamiFact]
    public async Task MetricsFiltersPaginationAndExpandedRowsMatchStoredValues()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        MetricsRequest Request() => new() { StartAtDate = site.Start, EndAtDate = site.End, Type = MetricType.url, Limit = 1 };
        var request = Request();
        var first = await site.Data.GetMetrics(request);
        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.Single(first.Data!);
        request.Offset = 1;
        var second = await site.Data.GetMetrics(request);
        Assert.Single(second.Data!);
        Assert.NotEqual(first.Data![0].x, second.Data![0].x);
        request = Request(); request.Limit = 10; request.Path = "/checkout";
        var filtered = await site.Data.GetMetrics(request);
        Assert.Equal("/checkout", Assert.Single(filtered.Data!).x);
        Assert.Equal(1, filtered.Data![0].y);
        request.Path = null; request.DistinctId = "visitor-b";
        Assert.Equal("/other", Assert.Single((await site.Data.GetMetrics(request)).Data!).x);
        request.DistinctId = null; request.UtmSource = "contract";
        Assert.Equal("/landing", Assert.Single((await site.Data.GetMetrics(request)).Data!).x);
        request.UtmSource = null; request.Search = "checkout";
        Assert.Equal("/checkout", Assert.Single((await site.Data.GetMetrics(request)).Data!).x);
        request.Search = null; request.Type = MetricType.@event;
        Assert.Equal("purchase", Assert.Single((await site.Data.GetMetrics(request)).Data!).x);
        request.Type = MetricType.path;
        var expanded = await site.Data.GetExpandedMetrics(request);
        Assert.Equal(HttpStatusCode.OK, expanded.Status);
        Assert.Contains(expanded.Data!, row => row.name == "/landing" && row.pageviews == 1);
    }

    [DisposableUmamiFact]
    public async Task EmptyAndOutsideDateRangesReturnEmptyDatasets()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        var empty = await site.Data.GetStats(new() { StartAtDate = site.Start.AddDays(-10), EndAtDate = site.Start.AddDays(-9) });
        Assert.Equal(HttpStatusCode.OK, empty.Status);
        Assert.Equal(0, empty.Data!.pageviews.value);
        var metrics = await site.Data.GetMetrics(new() { StartAtDate = site.Start, EndAtDate = site.End, Type = MetricType.path, Path = "/does-not-exist" });
        Assert.Equal(HttpStatusCode.OK, metrics.Status);
        Assert.Empty(metrics.Data!);
    }

    [DisposableUmamiTheory]
    [InlineData(AnalyticsEndpoint.Attribution, "attribution")]
    [InlineData(AnalyticsEndpoint.Breakdown, "breakdown")]
    [InlineData(AnalyticsEndpoint.FunnelStats, "funnels/stats")]
    [InlineData(AnalyticsEndpoint.GoalStats, "goals/stats")]
    [InlineData(AnalyticsEndpoint.Heatmaps, "heatmaps")]
    [InlineData(AnalyticsEndpoint.Journeys, "journeys")]
    [InlineData(AnalyticsEndpoint.Retention, "retention")]
    [InlineData(AnalyticsEndpoint.PerformanceStats, "performance/stats")]
    [InlineData(AnalyticsEndpoint.PerformanceChart, "performance/chart")]
    [InlineData(AnalyticsEndpoint.PerformanceMetrics, "performance/metrics")]
    [InlineData(AnalyticsEndpoint.UtmMetrics, "utm/metrics")]
    [InlineData(AnalyticsEndpoint.RevenueStats, "revenue/stats")]
    [InlineData(AnalyticsEndpoint.RevenueChart, "revenue/chart")]
    [InlineData(AnalyticsEndpoint.RevenueMetrics, "revenue/metrics")]
    [InlineData(AnalyticsEndpoint.Sessions, "sessions")]
    [InlineData(AnalyticsEndpoint.Events, "events")]
    public async Task EveryAnalyticsEndpointExecutesAgainstRealServer(AnalyticsEndpoint endpoint, string route)
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        var request = site.Analytics();
        request.Type = "path"; request.Currency = "GBP"; request.Metric = "lcp";
        request.Model = "last-click"; request.Step = "purchase";
        request.Fields = "[\"path\",\"country\"]";
        request.Window = 60;
        request.Steps = "[{\"type\":\"path\",\"value\":\"/landing\"},{\"type\":\"path\",\"value\":\"/checkout\"}]";
        request.Value = "/checkout"; request.UrlPath = "/checkout"; request.Mode = "click";
        if (endpoint == AnalyticsEndpoint.Attribution) request.Type = "event";
        if (endpoint == AnalyticsEndpoint.Journeys) request.Steps = "3";
        if (endpoint == AnalyticsEndpoint.UtmMetrics) request.Type = "utm_source";
        if (endpoint == AnalyticsEndpoint.RevenueMetrics) request.Type = "country";
        var result = await site.Data.GetAnalyticsAsync(endpoint, request);
        Assert.True(result.Status == HttpStatusCode.OK, $"{endpoint}: {result.Status} ({result.Message})");
        Assert.Contains(result.Data.ValueKind, new[] { JsonValueKind.Array, JsonValueKind.Object });
        var sent = Assert.Single(site.DataRequests, item => item.Path.StartsWith("/api/websites/"));
        Assert.Equal(HttpMethod.Get, sent.Method);
        Assert.Equal($"/api/websites/{site.WebsiteId}/{route}", sent.Path);
        if (endpoint == AnalyticsEndpoint.RevenueStats)
        {
            Assert.Equal(19.95, double.Parse(result.Data.GetProperty("sum").ToString(), System.Globalization.CultureInfo.InvariantCulture), 2);
            Assert.Equal(1, result.Data.GetProperty("count").GetInt32());
        }
        if (endpoint == AnalyticsEndpoint.PerformanceStats)
        {
            Assert.Equal(1200, result.Data.GetProperty("lcp").GetProperty("p75").GetDouble());
            Assert.Equal(0.05, result.Data.GetProperty("cls").GetProperty("p75").GetDouble(), 3);
            Assert.Equal(1, result.Data.GetProperty("count").GetInt32());
        }
        if (endpoint == AnalyticsEndpoint.Sessions) Assert.Equal(2, result.Data.GetProperty("count").GetInt32());
        if (endpoint == AnalyticsEndpoint.UtmMetrics)
            Assert.Contains(result.Data.EnumerateArray(), item => item.GetProperty("utm").GetString() == "contract" && item.GetProperty("views").GetInt32() == 1);
    }

    private sealed class SessionPage
    {
        public SessionRow[] Data { get; set; } = [];
        public int Count { get; set; }
    }
    private sealed class SessionRow
    {
        public string Id { get; set; } = "";
        public string? DistinctId { get; set; }
    }

    [DisposableUmamiFact]
    public async Task CallerTypedAnalyticsAndGenericSessionDetailDeserializeCorrectly()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        var sessions = await site.Data.GetAnalyticsAsync<SessionPage>(AnalyticsEndpoint.Sessions, site.Analytics());
        Assert.Equal(HttpStatusCode.OK, sessions.Status);
        Assert.Equal(2, sessions.Data!.Count);
        var visitor = Assert.Single(sessions.Data.Data, row => row.Id == site.VisitorSessionId);
        var detail = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, "sessions/" + visitor.Id, site.DateQuery());
        Assert.Equal(HttpStatusCode.OK, detail.Status);
        Assert.Equal(visitor.Id, detail.Data.GetProperty("id").GetString());
    }
}
