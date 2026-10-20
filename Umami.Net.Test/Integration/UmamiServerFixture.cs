using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umami.Net.Config;
using Umami.Net.Models;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.Test.Integration;

public sealed class DisposableUmamiFactAttribute : FactAttribute
{
    public DisposableUmamiFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UMAMI_TEST_URL")))
            Skip = "Run scripts/test-umami-integration.sh to create the disposable Umami/PostgreSQL stack.";
    }
}

public sealed class DisposableUmamiTheoryAttribute : TheoryAttribute
{
    public DisposableUmamiTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UMAMI_TEST_URL")))
            Skip = "Run scripts/test-umami-integration.sh to create the disposable Umami/PostgreSQL stack.";
    }
}

[CollectionDefinition("Disposable Umami", DisableParallelization = true)]
public class UmamiIntegrationCollection : ICollectionFixture<UmamiServerFixture>;

public sealed class UmamiServerFixture : IAsyncLifetime
{
    public Uri Endpoint { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;
    private readonly ConcurrentBag<string> _users = [];
    private readonly ConcurrentBag<string> _keys = [];

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("UMAMI_TEST_URL");
        if (string.IsNullOrWhiteSpace(configured)) return;
        Endpoint = new Uri(configured);
        if (!Endpoint.IsLoopback || Endpoint.Scheme != "http" || Endpoint.Port != 18086 || Endpoint.AbsolutePath != "/")
            throw new InvalidOperationException("Integration tests only accept the disposable loopback HTTP server on port 18086.");
        Admin = new HttpClient { BaseAddress = Endpoint, Timeout = TimeSpan.FromSeconds(15) };
        using var login = await Admin.PostAsJsonAsync("api/auth/login", new { username = "admin", password = "umami" });
        login.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Admin.DefaultRequestHeaders.Authorization = new("Bearer", json.RootElement.GetProperty("token").GetString());
    }

    public async Task<UmamiTestSite> CreateSiteAsync(bool seed = false)
    {
        using var created = await Admin.PostAsJsonAsync("api/websites", new { name = "SDK integration " + Guid.NewGuid(), domain = "integration.test" });
        created.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var site = new UmamiTestSite(this, json.RootElement.GetProperty("id").GetString()!);
        try { if (seed) await site.SeedAsync(); return site; }
        catch { await site.DisposeAsync(); throw; }
    }

    public async Task<(string Id, string Username, string Password)> CreateUserAsync()
    {
        var username = "sdk-" + Guid.NewGuid().ToString("N");
        const string password = "local-sdk-user-only";
        using var response = await Admin.PostAsJsonAsync("api/users", new { username, password, role = "user" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetString()!;
        _users.Add(id);
        return (id, username, password);
    }

    public async Task<(string Id, string Key)> CreateKeyAsync()
    {
        using var response = await Admin.PostAsJsonAsync("api/me/api-keys", new { name = "SDK integration " + Guid.NewGuid() });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetString()!;
        _keys.Add(id);
        return (id, json.RootElement.GetProperty("key").GetString()!);
    }

    public async Task DisposeAsync()
    {
        if (Admin == null) return;
        foreach (var id in _keys) using (await Admin.DeleteAsync("api/me/api-keys/" + id)) { }
        foreach (var id in _users) using (await Admin.DeleteAsync("api/users/" + id)) { }
        Admin.Dispose();
    }
}

public sealed class UmamiTestSite : IAsyncDisposable
{
    public const string Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
    private readonly UmamiServerFixture _server;
    public string WebsiteId { get; }
    public ServiceProvider Services { get; }
    public UmamiClient Client => Services.GetRequiredService<UmamiClient>();
    public UmamiDataService Data => Services.GetRequiredService<UmamiDataService>();
    public IEnumerable<(HttpMethod Method, string Path)> DataRequests => Services.GetRequiredService<IntegrationRequestRecorder>().Requests;
    public DateTime Start { get; } = DateTime.UtcNow.AddDays(-7);
    public DateTime End => DateTime.UtcNow.AddMinutes(1);
    public string? VisitorSessionId { get; private set; }
    private bool _senderStarted;

    public UmamiTestSite(UmamiServerFixture server, string websiteId)
    {
        _server = server; WebsiteId = websiteId;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analytics:UmamiPath"] = server.Endpoint.ToString(), ["Analytics:WebsiteId"] = websiteId,
            ["Analytics:Username"] = "admin", ["Analytics:Password"] = "umami"
        }).Build();
        var services = new ServiceCollection();
        services.SetupUmamiClient(configuration);
        services.SetupUmamiData(configuration);
        services.AddSingleton<IntegrationRequestRecorder>();
        services.AddTransient<IntegrationRecordingHandler>();
        services.AddHttpClient<AuthService>().AddHttpMessageHandler<IntegrationRecordingHandler>();
        Services = services.BuildServiceProvider();
    }

    public UmamiPayload Payload(string path = "/landing", string visitor = "visitor-a", string? name = null) => new()
    {
        Url = path, Hostname = "integration.test", IpAddress = "203.0.113.10", UserAgent = Agent,
        DistinctId = visitor, Name = name, Tag = "sdk-integration"
    };

    public StatsRequest Range() => new() { StartAtDate = Start, EndAtDate = End };
    public AnalyticsRequest Analytics() => new() { StartAtDate = Start, EndAtDate = End, Unit = Unit.day, Timezone = "UTC" };
    public Dictionary<string, string?> DateQuery() => new()
    {
        ["startAt"] = new DateTimeOffset(Start).ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["endAt"] = new DateTimeOffset(End).ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["timezone"] = "UTC"
    };

    public TestDataClient CreateDataClient(string username = "admin", string password = "umami", string? key = null, string? websiteId = null) =>
        new(_server.Endpoint, new UmamiDataSettings { WebsiteId = websiteId ?? WebsiteId, Username = username, Password = password, ApiKey = key });

    public async Task SeedAsync()
    {
        var landed = await Client.TrackPageViewAndDecode(payload: Payload("/landing?utm_source=contract&utm_medium=email&utm_campaign=integration"));
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, landed!.Status);
        VisitorSessionId = landed.SessionId.ToString();
        using (var checkout = await Client.TrackPageView(payload: Payload("/checkout"))) checkout.EnsureSuccessStatusCode();
        var purchase = Payload("/checkout", name: "purchase");
        purchase.Data = new() { ["revenue"] = 19.95, ["currency"] = "GBP", ["order"] = "sdk-123" };
        using (var response = await Client.Track(purchase)) response.EnsureSuccessStatusCode();
        using (var other = await Client.TrackPageView(payload: Payload("/other?utm_source=other", "visitor-b"))) other.EnsureSuccessStatusCode();
        var performance = Payload("/checkout");
        performance.Lcp = 1200; performance.Inp = 80; performance.Cls = 0.05; performance.Fcp = 750; performance.Ttfb = 150;
        using var vital = await Client.TrackPerformanceAsync(performance); vital.EnsureSuccessStatusCode();
    }

    public async Task<UmamiBackgroundSender> StartSenderAsync()
    {
        var sender = Services.GetRequiredService<UmamiBackgroundSender>();
        await sender.StartAsync(CancellationToken.None); _senderStarted = true; return sender;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_senderStarted)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await Services.GetRequiredService<UmamiBackgroundSender>().StopAsync(deadline.Token);
            }
            await Services.DisposeAsync();
        }
        finally
        {
            using var deleted = await _server.Admin.DeleteAsync("api/websites/" + WebsiteId);
            deleted.EnsureSuccessStatusCode();
        }
    }
}

public sealed class TestDataClient : IDisposable
{
    private sealed class RecordingHandler : DelegatingHandler
    {
        public readonly List<(HttpMethod Method, string Path)> Requests = [];
        public RecordingHandler() : base(new SocketsHttpHandler()) { }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            return base.SendAsync(request, cancellationToken);
        }
    }
    private readonly RecordingHandler _handler = new();
    public HttpClient Http { get; }
    public AuthService Auth { get; }
    public UmamiDataService Data { get; }
    public IReadOnlyList<(HttpMethod Method, string Path)> Requests => _handler.Requests;

    public TestDataClient(Uri endpoint, UmamiDataSettings settings)
    {
        Http = new(_handler) { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(10) };
        Auth = new(Http, settings, NullLogger<AuthService>.Instance);
        Data = new(settings, Auth, NullLogger<UmamiDataService>.Instance);
    }
    public void Dispose() => Http.Dispose();
}

// Observe the real request pipeline without replacing the server or HTTP transport.
public sealed class IntegrationRequestRecorder
{
    public ConcurrentQueue<(HttpMethod Method, string Path)> Requests { get; } = new();
}

public sealed class IntegrationRecordingHandler(IntegrationRequestRecorder recorder) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        recorder.Requests.Enqueue((request.Method, request.RequestUri!.AbsolutePath));
        return base.SendAsync(request, cancellationToken);
    }
}
