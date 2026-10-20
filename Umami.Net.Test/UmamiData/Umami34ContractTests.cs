using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.Models;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Helpers;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.Test.UmamiData;

public class Umami34ContractTests
{
    private const string WebsiteId = "12345678-1234-1234-1234-123456789abc";
    private static readonly string SessionId = Guid.NewGuid().ToString();
    private static readonly string VisitId = Guid.NewGuid().ToString();

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private static HttpResponseMessage Json(string value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(value, Encoding.UTF8, "application/json") };

    private static (UmamiDataService Service, AuthService Auth, HttpClient Client) Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send, bool apiKey = true, bool cloud = false)
    {
        var settings = new UmamiDataSettings { WebsiteId = WebsiteId, ApiKey = apiKey ? "umami_contract_test_key" : null, UseCloudApi = cloud };
        var client = new HttpClient(new Handler(send)) { BaseAddress = new Uri("https://example.test/analytics/") };
        var auth = new AuthService(client, settings, NullLogger<AuthService>.Instance);
        return (new UmamiDataService(settings, auth, NullLogger<UmamiDataService>.Instance), auth, client);
    }

    private static StatsRequest Range() => new() { StartAtDate = DateTime.UtcNow.AddDays(-7), EndAtDate = DateTime.UtcNow };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiKeyUsesBearerWithoutCallingBlockedAuthRoutes(bool cloud)
    {
        var state = Create((request, _) =>
        {
            Assert.Equal($"/analytics/{(cloud ? "" : "api/")}websites/{WebsiteId}/active", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("umami_contract_test_key", request.Headers.Authorization?.Parameter);
            return Task.FromResult(Json("{\"visitors\":12}"));
        }, cloud: cloud);
        using var client = state.Client;
        Assert.Equal(12, (await state.Service.GetActiveUsers()).Data!.ActiveUsers);
    }

    [Fact]
    public async Task UnauthorizedWebsiteRetriesLoginOnceAndTerminates()
    {
        var logins = 0; var calls = 0;
        var state = Create((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("auth/login"))
            { logins++; return Task.FromResult(Json("{\"token\":\"session-token\"}")); }
            if (request.RequestUri.AbsolutePath.EndsWith("auth/verify"))
            { Assert.Equal(HttpMethod.Post, request.Method); return Task.FromResult(Json("{}")); }
            calls++; return Task.FromResult(Json("{}", HttpStatusCode.Unauthorized));
        }, apiKey: false);
        using var client = state.Client;
        Assert.Equal(HttpStatusCode.Unauthorized, (await state.Service.GetStats(Range())).Status);
        Assert.Equal(2, calls); Assert.Equal(2, logins);
    }

    [Fact]
    public async Task RevokedApiKeyDoesNotFallBackToPasswordOrRetry()
    {
        var calls = 0;
        var state = Create((_, _) => { calls++; return Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)); });
        using var client = state.Client;
        Assert.Equal(HttpStatusCode.Unauthorized, (await state.Service.GetActiveUsers()).Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task PartialTwoFactorTokenIsNotACompletedLogin()
    {
        var state = Create((_, _) => Task.FromResult(Json("{\"requiresTwoFactor\":true,\"partialToken\":\"secret-partial\"}")), apiKey: false);
        using var client = state.Client;
        Assert.False(await state.Auth.Login());
        Assert.Null(client.DefaultRequestHeaders.Authorization);
    }

    [Theory]
    [InlineData("{\"pageviews\":15,\"visitors\":7,\"visits\":8,\"bounces\":2,\"totaltime\":90,\"comparison\":{\"pageviews\":10,\"visitors\":5,\"visits\":6,\"bounces\":3,\"totaltime\":70}}")]
    [InlineData("{\"pageviews\":{\"value\":15,\"prev\":10},\"visitors\":{\"value\":7,\"prev\":5}}")]
    public async Task StatsReadCurrentAndLegacyShapes(string json)
    {
        var state = Create((_, _) => Task.FromResult(Json(json)));
        using var client = state.Client;
        var result = await state.Service.GetStats(Range());
        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Equal(15, result.Data!.pageviews.value);
        Assert.Equal(10, result.Data.pageviews.prev);
        Assert.Equal(7, result.Data.visitors.value);
    }

    [Fact]
    public async Task MalformedAnalyticsResponseIsBadGateway()
    {
        var state = Create((_, _) => Task.FromResult(Json("not-json")));
        using var client = state.Client;
        Assert.Equal(HttpStatusCode.BadGateway, (await state.Service.GetActiveUsers()).Status);
    }

    [Fact]
    public async Task CancellationReachesTheTransport()
    {
        var state = Create(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Json("{}"); });
        using var client = state.Client;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.Service.GetAnalyticsAsync(AnalyticsEndpoint.Sessions,
            new AnalyticsRequest { StartAtDate = DateTime.UtcNow.AddDays(-1), EndAtDate = DateTime.UtcNow }, cancellation.Token));
    }

    [Fact]
    public async Task CurrentMetricMappingPreservesCamelCaseFiltersAndRegexValues()
    {
        var request = new MetricsRequest { StartAtDate = DateTime.UtcNow.AddDays(-1), EndAtDate = DateTime.UtcNow,
            Type = MetricType.url, Path = "re./path=foo,bar", UtmSource = "c.newsletter", DistinctId = "customer-7" };
        var state = Create((message, _) =>
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(message.RequestUri!.Query);
            Assert.Equal("path", query["type"]); Assert.Equal(request.Path, query["path"]);
            Assert.Equal("c.newsletter", query["utmSource"]); Assert.Equal("customer-7", query["distinctId"]);
            return Task.FromResult(Json("[]"));
        });
        using var client = state.Client;
        Assert.Equal(HttpStatusCode.OK, (await state.Service.GetMetrics(request)).Status);
        request.Type = MetricType.utmSource;
        Assert.Contains("type=utmSource", request.ToQueryString());
    }

    [Fact]
    public void EventMetricListsAllNamesWithoutRequiringAnEventFilter()
    {
        var request = new MetricsRequest { StartAtDate = DateTime.UtcNow.AddDays(-1), EndAtDate = DateTime.UtcNow, Type = MetricType.@event };
        request.Validate();
        Assert.Contains("type=event", request.ToQueryString());
    }

    [Fact]
    public async Task AnnotationCrudUsesCurrentMethodsAndCamelCaseBody()
    {
        var id = Guid.NewGuid().ToString(); var methods = new List<HttpMethod>();
        var state = Create(async (message, _) =>
        {
            methods.Add(message.Method);
            if (message.Method == HttpMethod.Delete) return Json("{\"ok\":true}");
            if (message.Method == HttpMethod.Post)
            {
                using var data = JsonDocument.Parse(await message.Content!.ReadAsStringAsync());
                Assert.Equal("Launch", data.RootElement.GetProperty("note").GetString());
                Assert.True(data.RootElement.GetProperty("allDay").GetBoolean());
            }
            return Json($"{{\"id\":\"{id}\",\"date\":\"2026-10-20T00:00:00Z\",\"allDay\":true,\"note\":\"Launch\"}}");
        });
        using var client = state.Client;
        var note = new AnnotationInput(DateTimeOffset.UtcNow, "Launch");
        Assert.Equal(id, (await state.Service.CreateAnnotationAsync(note)).Data!.Id);
        Assert.Equal(id, (await state.Service.UpdateAnnotationAsync(id, note)).Data!.Id);
        Assert.Equal(id, (await state.Service.GetAnnotationAsync(id)).Data!.Id);
        Assert.True((await state.Service.DeleteAnnotationAsync(id)).Data);
        Assert.Equal(new[] { HttpMethod.Post, HttpMethod.Post, HttpMethod.Get, HttpMethod.Delete }, methods);
    }

    [Theory]
    [InlineData("../other")]
    [InlineData("https://attacker.test")]
    [InlineData("stats?foo=bar")]
    [InlineData("stats/%2e%2e/users")]
    public void GenericWebsiteResourcesCannotEscapeTheirScope(string path)
    {
        var state = Create((_, _) => throw new InvalidOperationException("Must not send"));
        using var client = state.Client;
        Assert.Throws<ArgumentException>(() => { _ = state.Service.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, path); });
    }

    [Fact]
    public async Task TrackingParsesJsonAndKeepsExplicitDataUserAgentAndCacheIsolated()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "Mozilla/5.0 Real Visitor";
        context.Request.Path = "/landing"; context.Request.QueryString = new("?utm_source=mail");
        var settings = new UmamiClientSettings { WebsiteId = WebsiteId };
        using var http = new HttpClient(new Handler(async (message, _) =>
        {
            Assert.Equal("Mozilla/5.0 Real Visitor", message.Headers.UserAgent.ToString());
            using var data = JsonDocument.Parse(await message.Content!.ReadAsStringAsync());
            var payload = data.RootElement.GetProperty("payload");
            Assert.Equal("/landing?utm_source=mail", payload.GetProperty("url").GetString());
            Assert.Equal("kept", payload.GetProperty("data").GetProperty("value").GetString());
            return Json($"{{\"cache\":\"opaque-cache\",\"sessionId\":\"{SessionId}\",\"visitId\":\"{VisitId}\"}}");
        })) { BaseAddress = new("https://example.test/") };
        var client = new UmamiClient(http, new PayloadService(new HttpContextAccessor { HttpContext = context }, settings,
            NullLogger<PayloadService>.Instance), new JwtDecoder(NullLogger<JwtDecoder>.Instance), NullLogger<UmamiClient>.Instance, settings);
        var result = await client.TrackAndDecode(new UmamiPayload { Name = "purchase", Data = new() { ["value"] = "kept" } });
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, result!.Status);
        Assert.Equal(Guid.Parse(SessionId), result.SessionId);
        Assert.Equal("opaque-cache", result.Cache);
        Assert.Null(http.DefaultRequestHeaders.FirstOrDefault(header => header.Key == "x-umami-cache").Value);
    }

    [Fact]
    public async Task PerformancePayloadAndCacheHeaderRemainPerVisitor()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(async (message, _) =>
        {
            calls++;
            using var body = JsonDocument.Parse(await message.Content!.ReadAsStringAsync());
            if (calls == 1)
            {
                Assert.Equal("performance", body.RootElement.GetProperty("type").GetString());
                Assert.Equal(1200, body.RootElement.GetProperty("payload").GetProperty("lcp").GetDouble());
                Assert.Equal("visitor-one-cache", message.Headers.GetValues("x-umami-cache").Single());
            }
            else Assert.False(message.Headers.Contains("x-umami-cache"));
            return Json("{}");
        })) { BaseAddress = new("https://example.test/") };
        var settings = new UmamiClientSettings { WebsiteId = WebsiteId };
        var client = new UmamiClient(http, new PayloadService(new HttpContextAccessor(), settings, NullLogger<PayloadService>.Instance),
            new JwtDecoder(NullLogger<JwtDecoder>.Instance), NullLogger<UmamiClient>.Instance, settings);
        using var first = await client.TrackPerformanceAsync(new UmamiPayload { Url = "/one", Lcp = 1200, CacheToken = "visitor-one-cache" });
        Assert.Equal("{}", await first.Content.ReadAsStringAsync());
        using var second = await client.TrackPageView("/two");
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidPerformanceTimingsFailBeforeSending(double value)
    {
        using var http = new HttpClient(new Handler((_, _) => throw new InvalidOperationException("Must not send")));
        var settings = new UmamiClientSettings { WebsiteId = WebsiteId };
        var client = new UmamiClient(http, new PayloadService(new HttpContextAccessor(), settings, NullLogger<PayloadService>.Instance),
            new JwtDecoder(NullLogger<JwtDecoder>.Instance), NullLogger<UmamiClient>.Instance, settings);
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = client.TrackPerformanceAsync(new UmamiPayload { Lcp = value }); });
    }

    [Fact]
    public async Task ExplicitApiKeyVerificationUsesAnAllowedRouteAndReportsRevocation()
    {
        var state = Create((request, _) =>
        {
            Assert.EndsWith("/api/me", request.RequestUri!.AbsolutePath);
            Assert.Equal("umami_contract_test_key", request.Headers.Authorization?.Parameter);
            return Task.FromResult(Json("{}", HttpStatusCode.Unauthorized));
        });
        using var http = state.Client;
        Assert.False(await state.Auth.VerifyToken());
    }

    [Fact]
    public void LegacyUserAliasDoesNotOverrideExplicitUsername()
    {
        var settings = new UmamiDataSettings { User = "legacy" };
        Assert.Equal("legacy", settings.Username);
        settings.Username = "preferred"; settings.User = "other";
        Assert.Equal("preferred", settings.Username);
    }

    [Theory]
    [InlineData("{\"beep\":\"boop\"}", UmamiDataResponse.ResponseStatus.BotDetected)]
    [InlineData("{\"message\":\"beep boop\"}", UmamiDataResponse.ResponseStatus.Failed)]
    [InlineData("{broken", UmamiDataResponse.ResponseStatus.Failed)]
    public async Task TrackingRecognizesOnlyTheActualBotContract(string json, UmamiDataResponse.ResponseStatus expected)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(json)))) { BaseAddress = new("https://example.test/") };
        var settings = new UmamiClientSettings { WebsiteId = WebsiteId };
        var client = new UmamiClient(http, new PayloadService(new HttpContextAccessor(), settings, NullLogger<PayloadService>.Instance),
            new JwtDecoder(NullLogger<JwtDecoder>.Instance), NullLogger<UmamiClient>.Instance, settings);
        Assert.Equal(expected, (await client.TrackAndDecode("test"))!.Status);
    }
}
