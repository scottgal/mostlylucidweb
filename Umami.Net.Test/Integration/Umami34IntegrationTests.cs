using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umami.Net;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.Models;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;


namespace Umami.Net.Test.Integration;

public sealed class DisposableUmamiFactAttribute : FactAttribute
{
    public DisposableUmamiFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UMAMI_TEST_URL")))
            Skip = "Opt in with UMAMI_TEST_URL=http://127.0.0.1:18086/ and the disposable Umami 3.4.0 test stack.";
    }
}

public class Umami34IntegrationTests
{
    [DisposableUmamiFact]
    public async Task CurrentClientWorksWithRealUmami34AndPostgres()
    {
        var endpoint = new Uri(Environment.GetEnvironmentVariable("UMAMI_TEST_URL")!);
        if (!endpoint.IsLoopback || endpoint.Scheme != "http" || endpoint.Port != 18086)
            throw new InvalidOperationException("This destructive integration check is restricted to the disposable local Umami server on port 18086.");
        using var setup = new HttpClient { BaseAddress = endpoint };
        using var login = await setup.PostAsJsonAsync("api/auth/login", new { username = "admin", password = "umami" });
        login.EnsureSuccessStatusCode();
        using var authenticated = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        setup.DefaultRequestHeaders.Authorization = new("Bearer", authenticated.RootElement.GetProperty("token").GetString());
        using var created = await setup.PostAsJsonAsync("api/websites", new { name = "Disposable .NET integration", domain = "integration.test" });
        created.EnsureSuccessStatusCode();
        using var website = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var websiteId = website.RootElement.GetProperty("id").GetString()!;
        var settings = new UmamiDataSettings { WebsiteId = websiteId, UmamiPath = endpoint.ToString(), Username = "admin", Password = "umami" };
        using var collectionHttp = new HttpClient { BaseAddress = endpoint };
        using var dataHttp = new HttpClient { BaseAddress = endpoint };
        var client = new UmamiClient(collectionHttp, new PayloadService(new HttpContextAccessor(), settings, NullLogger<PayloadService>.Instance),
            new JwtDecoder(NullLogger<JwtDecoder>.Instance), NullLogger<UmamiClient>.Instance, settings);
        var data = new UmamiDataService(settings, new AuthService(dataHttp, settings, NullLogger<AuthService>.Instance), NullLogger<UmamiDataService>.Instance);
        const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
        var payload = new UmamiPayload { Url = "/landing?utm_source=contract", Hostname = "integration.test", UserAgent = agent, IpAddress = "203.0.113.10", DistinctId = "integration-customer", Tag = "integration" };
        var tracked = await client.TrackPageViewAndDecode(payload: payload);
        if (tracked?.Status != UmamiDataResponse.ResponseStatus.Success || tracked.SessionId == Guid.Empty || string.IsNullOrEmpty(tracked.Cache)) throw new Exception("Tracking JSON contract failed");
        using var performance = await client.TrackPerformanceAsync(new UmamiPayload { Url = "/landing", Hostname = "integration.test", UserAgent = agent, IpAddress = payload.IpAddress,
            DistinctId = payload.DistinctId, CacheToken = tracked.Cache, Lcp = 1200, Inp = 80, Cls = 0.05, Fcp = 750, Ttfb = 150 });
        performance.EnsureSuccessStatusCode();
        using var namedEvent = await client.Track(new UmamiPayload { Name = "integration-purchase", Url = "/landing",
            Hostname = "integration.test", UserAgent = agent, IpAddress = payload.IpAddress,
            DistinctId = payload.DistinctId, Data = new() { ["amount"] = 15 } });
        namedEvent.EnsureSuccessStatusCode();
        var start = DateTime.UtcNow.AddDays(-1); var end = DateTime.UtcNow.AddDays(1);
        var stats = await data.GetStats(start, end);
        if (stats.Status != HttpStatusCode.OK || stats.Data!.pageviews.value < 1) throw new Exception("Stats normalization failed: " + stats.Message);
        var active = await data.GetActiveUsers();
        if (active.Data?.ActiveUsers < 1 || active.Status != HttpStatusCode.OK) throw new Exception("Active visitors failed");
        var metrics = await data.GetMetrics(new() { StartAtDate = start, EndAtDate = end, Type = MetricType.url });
        if (metrics.Status != HttpStatusCode.OK || metrics.Data!.Length == 0) throw new Exception("V3 metrics mapping failed");
        var expanded = await data.GetExpandedMetrics(new() { StartAtDate = start, EndAtDate = end, Type = MetricType.path });
        if (expanded.Status != HttpStatusCode.OK || expanded.Data!.Length == 0) throw new Exception("Expanded metrics failed");
        var series = await data.GetEventsSeries(start, end, Unit.minute, "UTC");
        if (series.Status != HttpStatusCode.OK || !series.Data!.Any(item => item.x == "integration-purchase")) throw new Exception("Minute event series failed");
        var pageviews = await data.GetPageViews(new() { StartAtDate = start, EndAtDate = end, Unit = Unit.minute, Timezone = "UTC" });
        if (pageviews.Status != HttpStatusCode.OK || pageviews.Data!.pageviews.Length == 0) throw new Exception("Minute pageviews failed");
        var annotations = await data.CreateAnnotationAsync(new AnnotationInput(DateTimeOffset.UtcNow, "Integration deployment", false));
        if (annotations.Status != HttpStatusCode.OK || string.IsNullOrEmpty(annotations.Data?.Id)) throw new Exception("Annotation create failed");
        var annotationId = annotations.Data!.Id;
        var list = await data.GetAnnotationsAsync();
        if (!list.Data!.Data.Any(note => note.Id == annotationId)) throw new Exception("Annotation list failed");
        var updated = await data.UpdateAnnotationAsync(annotationId, new AnnotationInput(DateTimeOffset.UtcNow, "Updated integration", true));
        if (updated.Data?.Note != "Updated integration") throw new Exception("Annotation update failed");
        if (!(await data.DeleteAnnotationAsync(annotationId)).Data) throw new Exception("Annotation delete failed");
        foreach (var type in new[] { AnalyticsEndpoint.PerformanceStats, AnalyticsEndpoint.PerformanceChart, AnalyticsEndpoint.PerformanceMetrics, AnalyticsEndpoint.UtmMetrics, AnalyticsEndpoint.Sessions })
        {
            var result = await data.GetAnalyticsAsync(type, new AnalyticsRequest { StartAtDate = start, EndAtDate = end, Unit = Unit.day, Timezone = "UTC", Metric = "lcp", Type = type == AnalyticsEndpoint.UtmMetrics ? "utm_source" : "path" });
            if (result.Status != HttpStatusCode.OK) throw new Exception(type + ": " + result.Status + " " + result.Message);
        }
        using var keyCreated = await setup.PostAsJsonAsync("api/me/api-keys", new { name = "Disposable SDK key" });
        keyCreated.EnsureSuccessStatusCode();
        using var keyJson = JsonDocument.Parse(await keyCreated.Content.ReadAsStringAsync());
        settings.ApiKey = keyJson.RootElement.GetProperty("key").GetString();
        using var keyHttp = new HttpClient { BaseAddress = endpoint };
        var keyData = new UmamiDataService(settings, new AuthService(keyHttp, settings, NullLogger<AuthService>.Instance), NullLogger<UmamiDataService>.Instance);
        if ((await keyData.GetStats(start, end)).Status != HttpStatusCode.OK) throw new Exception("Real API key auth failed");
        Console.WriteLine("PASS: real Umami 3.4.0 tracking JSON/cache, Web Vitals, stats, active visitors, metrics mapping, annotation CRUD, performance datasets, UTM, sessions and API-key authentication.");
    }
}
