using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Models;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.Test.Integration;

public partial class Umami34IntegrationTests
{
    private static void SetRequestContext(UmamiTestSite site)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new("integration.test");
        context.Request.PathBase = "/application"; context.Request.Path = "/overloads";
        context.Request.QueryString = new("?utm_source=context");
        context.Request.Headers.UserAgent = UmamiTestSite.Agent;
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        site.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static async Task<UmamiDataResponse> ReadTrackingAsync(Task<HttpResponseMessage> pending)
    {
        using var response = await pending;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return new(UmamiDataResponse.ResponseStatus.Success)
        {
            SessionId = Guid.Parse(json.RootElement.GetProperty("sessionId").GetString()!),
            VisitId = Guid.Parse(json.RootElement.GetProperty("visitId").GetString()!),
            Cache = json.RootElement.GetProperty("cache").GetString()
        };
    }

    [DisposableUmamiTheory]
    [InlineData("Send")]
    [InlineData("SendAsync")]
    [InlineData("SendAndDecode")]
    [InlineData("TrackPageView")]
    [InlineData("TrackPageViewAndDecode")]
    [InlineData("TrackName")]
    [InlineData("TrackPayload")]
    [InlineData("TrackNameAndDecode")]
    [InlineData("TrackPayloadAndDecode")]
    [InlineData("IdentifyEmptyAndDecode")]
    [InlineData("IdentifyPayload")]
    [InlineData("IdentifyPayloadAndDecode")]
    [InlineData("IdentifyParameters")]
    [InlineData("IdentifyParametersAndDecode")]
    [InlineData("IdentifySession")]
    [InlineData("IdentifySessionAndDecode")]
    public async Task CollectionOverloadsReturnUsableMetadataAndPersistExpectedEventType(string operation)
    {
        await using var site = await server.CreateSiteAsync();
        SetRequestContext(site);
        var client = site.Client;
        var payload = site.Payload("/overloads");
        var eventPayload = site.Payload("/overloads", name: "overload-event");
        eventPayload.Data = new() { ["amount"] = 3 };
        var result = operation switch
        {
            "Send" => await ReadTrackingAsync(client.Send(payload)),
            "SendAsync" => await ReadTrackingAsync(client.SendAsync(payload)),
            "SendAndDecode" => await client.SendAndDecode(payload),
            "TrackPageView" => await ReadTrackingAsync(client.TrackPageView(payload: payload)),
            "TrackPageViewAndDecode" => await client.TrackPageViewAndDecode(payload: payload),
            "TrackName" => await ReadTrackingAsync(client.Track("overload-event", new() { ["amount"] = 3 })),
            "TrackPayload" => await ReadTrackingAsync(client.Track(eventPayload)),
            "TrackNameAndDecode" => await client.TrackAndDecode("overload-event", new() { ["amount"] = 3 }),
            "TrackPayloadAndDecode" => await client.TrackAndDecode(eventPayload),
            "IdentifyEmptyAndDecode" => await client.IdentifyAndDecode(),
            "IdentifyPayload" => await ReadTrackingAsync(client.Identify(payload, new() { ["plan"] = "pro" })),
            "IdentifyPayloadAndDecode" => await client.IdentifyAndDecode(payload, new() { ["plan"] = "pro" }),
            "IdentifyParameters" => await ReadTrackingAsync(client.Identify(email: "person@example.test", username: "person", userId: "sdk-user", distinctId: "identified")),
            "IdentifyParametersAndDecode" => await client.IdentifyAndDecode("legacy-session", email: "person@example.test", username: "person", userId: "sdk-user", distinctId: "identified"),
            "IdentifySession" => await ReadTrackingAsync(client.IdentifySession("legacy-session", "identified")),
            "IdentifySessionAndDecode" => await client.IdentifySessionAndDecode("legacy-session"),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Assert.NotNull(result);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, result.Status);
        Assert.NotEqual(Guid.Empty, result.SessionId); Assert.NotEqual(Guid.Empty, result.VisitId);
        Assert.False(string.IsNullOrWhiteSpace(result.Cache));
        var stats = await site.Data.GetStats(site.Range());
        var isEvent = operation.StartsWith("TrackName") || operation.StartsWith("TrackPayload");
        var isIdentify = operation.StartsWith("Identify");
        Assert.Equal(isIdentify || isEvent ? 0 : 1, stats.Data!.pageviews.value);
        if (isEvent)
        {
            var events = await site.Data.GetMetrics(new() { StartAtDate = site.Start, EndAtDate = site.End, Type = MetricType.@event });
            Assert.Equal("overload-event", Assert.Single(events.Data!).x);
            Assert.Equal(1, events.Data![0].y);
        }
        if (operation.StartsWith("IdentifyPayload") || operation.StartsWith("IdentifyParameters"))
        {
            var properties = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, $"sessions/{result.SessionId}/properties");
            var key = operation.StartsWith("IdentifyPayload") ? "plan" : "email";
            var value = key == "plan" ? "pro" : "person@example.test";
            Assert.Contains(properties.Data.EnumerateArray(), item => item.GetProperty("dataKey").GetString() == key && item.GetProperty("stringValue").GetString() == value);
        }
    }

    [DisposableUmamiFact]
    public async Task ExplicitCacheAndIdentityRemainIsolatedAcrossVisitors()
    {
        await using var site = await server.CreateSiteAsync();
        var client = site.Client;
        var first = await client.TrackPageViewAndDecode(payload: site.Payload("/first", "visitor-a"));
        var secondPayload = site.Payload("/second", "visitor-a"); secondPayload.CacheToken = first!.Cache;
        var second = await client.TrackPageViewAndDecode(payload: secondPayload);
        Assert.Equal(first.SessionId, second!.SessionId); Assert.Equal(first.VisitId, second.VisitId);
        // Same IP and agent, different identity, even if a caller supplies an old cache token.
        var different = site.Payload("/third", "visitor-b"); different.CacheToken = first.Cache;
        var third = await client.TrackPageViewAndDecode(payload: different);
        Assert.NotEqual(first.SessionId, third!.SessionId);
        var stats = await site.Data.GetStats(site.Range());
        Assert.Equal(3, stats.Data!.pageviews.value); Assert.Equal(2, stats.Data.visitors.value);
    }

    [DisposableUmamiFact]
    public async Task RealBotResponseDoesNotStoreAnEvent()
    {
        await using var site = await server.CreateSiteAsync();
        var payload = site.Payload("/bot"); payload.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
        var result = await site.Client.TrackPageViewAndDecode(payload: payload);
        Assert.Equal(UmamiDataResponse.ResponseStatus.BotDetected, result!.Status);
        Assert.Equal(0, (await site.Data.GetStats(site.Range())).Data!.pageviews.value);
    }

    [DisposableUmamiFact]
    public async Task AmbientRequestMetadataAndHistoricalTimestampReachTheServer()
    {
        await using var site = await server.CreateSiteAsync();
        SetRequestContext(site);
        using (var view = await site.Client.TrackPageView()) view.EnsureSuccessStatusCode();
        var historical = site.Payload("/historical"); historical.Timestamp = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds();
        using (var response = await site.Client.SendAsync(historical)) response.EnsureSuccessStatusCode();
        var current = await site.Data.GetMetrics(new() { StartAtDate = DateTime.UtcNow.AddDays(-1), EndAtDate = site.End, Type = MetricType.path });
        Assert.Equal("/application/overloads", Assert.Single(current.Data!).x);
        var source = await site.Data.GetMetrics(new() { StartAtDate = site.Start, EndAtDate = site.End, Type = MetricType.utmSource });
        Assert.Equal("context", Assert.Single(source.Data!).x);
        var old = await site.Data.GetStats(new() { StartAtDate = DateTime.UtcNow.AddDays(-3), EndAtDate = DateTime.UtcNow.AddDays(-1) });
        Assert.Equal(1, old.Data!.pageviews.value);
    }

    [DisposableUmamiFact]
    public async Task BackgroundDeliveryPersistsAllOperationsBeforeShutdownCompletes()
    {
        await using var site = await server.CreateSiteAsync();
        SetRequestContext(site);
        var sender = await site.StartSenderAsync();
        await sender.TrackPageView("/queued", "Queued page", site.Payload("/queued", "queued-user"));
        await sender.Track("queued-purchase", new() { ["amount"] = 4 });
        await sender.Send(site.Payload("/queued", name: "queued-raw"));
        await sender.Identify(email: "queued@example.test", username: "queued", userId: "queued-id", distinctId: "queued-user");
        await sender.IdentifySession("legacy-session", new() { ["plan"] = "team" }, distinctId: "queued-user");
        var vital = site.Payload("/queued"); vital.Lcp = 1400; vital.Cls = 0.03;
        await sender.Send(vital, eventType: "performance");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await sender.StopAsync(deadline.Token);
        var stats = await site.Data.GetStats(site.Range());
        Assert.Equal(1, stats.Data!.pageviews.value);
        var names = await site.Data.GetMetrics(new() { StartAtDate = site.Start, EndAtDate = site.End, Type = MetricType.@event });
        Assert.Equal(new[] { "queued-purchase", "queued-raw" }, names.Data!.Select(row => row.x).Order());
        var sessions = await site.Data.GetAnalyticsAsync(AnalyticsEndpoint.Sessions, site.Analytics());
        var id = sessions.Data.GetProperty("data").EnumerateArray().Single(row => row.GetProperty("views").GetInt32() == 1).GetProperty("id").GetString();
        var properties = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, $"sessions/{id}/properties");
        Assert.Contains(properties.Data.EnumerateArray(), item => item.GetProperty("dataKey").GetString() == "email" && item.GetProperty("stringValue").GetString() == "queued@example.test");
        Assert.Contains(properties.Data.EnumerateArray(), item => item.GetProperty("dataKey").GetString() == "plan" && item.GetProperty("stringValue").GetString() == "team");
    }
}
