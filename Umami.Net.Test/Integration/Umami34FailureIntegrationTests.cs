using System.Net;
using System.Net.Sockets;
using Umami.Net.Config;
using Umami.Net.Models;
using System.Text.Json;
using Umami.Net.UmamiData;

namespace Umami.Net.Test.Integration;

public partial class Umami34IntegrationTests
{
    [DisposableUmamiFact]
    public async Task PasswordLoginVerifyAndRefreshUseRealAuthenticationEndpoints()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        using var other = site.CreateDataClient();
        Assert.True(await other.Auth.Login());
        Assert.True(await other.Auth.VerifyToken());
        other.Http.DefaultRequestHeaders.Authorization = new("Bearer", "expired-or-invalid-token");
        Assert.Equal(HttpStatusCode.OK, (await other.Data.GetStats(site.Range())).Status);
        Assert.Equal(2, other.Requests.Count(item => item.Path == "/api/auth/login"));
        Assert.Contains(other.Requests, item => item.Method == HttpMethod.Post && item.Path == "/api/auth/verify");
    }

    [DisposableUmamiFact]
    public async Task IncorrectCredentialsTerminateWithoutAttemptingADataRead()
    {
        await using var site = await server.CreateSiteAsync();
        using var invalid = site.CreateDataClient(password: "wrong-local-password");
        var result = await invalid.Data.GetStats(site.Range()).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Single(invalid.Requests);
        Assert.Equal("/api/auth/login", invalid.Requests[0].Path);
    }

    [DisposableUmamiFact]
    public async Task ValidUserWithoutWebsiteAccessHasBoundedReauthentication()
    {
        await using var site = await server.CreateSiteAsync();
        var user = await server.CreateUserAsync();
        using var restricted = site.CreateDataClient(user.Username, user.Password);
        var result = await restricted.Data.GetStats(site.Range()).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(2, restricted.Requests.Count(item => item.Path == "/api/auth/login"));
        Assert.Equal(2, restricted.Requests.Count(item => item.Path.EndsWith("/stats")));
        Assert.Equal(HttpStatusCode.Unauthorized, (await restricted.Data.CreateAnnotationAsync(new(DateTimeOffset.UtcNow, "Denied"))).Status);
        Assert.Empty((await site.Data.GetAnnotationsAsync()).Data!.Data);
    }

    [DisposableUmamiFact]
    public async Task ApiKeySupportsReadsWritesVerificationAndRevocationWithoutPasswordFallback()
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        var key = await server.CreateKeyAsync();
        using var keyed = site.CreateDataClient(password: "intentionally-unusable", key: key.Key);
        Assert.Equal(HttpStatusCode.OK, (await keyed.Data.GetStats(site.Range())).Status);
        Assert.True(await keyed.Auth.VerifyToken());
        var annotation = await keyed.Data.CreateAnnotationAsync(new(DateTimeOffset.UtcNow, "Created using API key"));
        Assert.Equal(HttpStatusCode.OK, annotation.Status);
        Assert.True((await keyed.Data.DeleteAnnotationAsync(annotation.Data!.Id)).Data);
        using (var revoked = await server.Admin.DeleteAsync("api/me/api-keys/" + key.Id)) revoked.EnsureSuccessStatusCode();
        Assert.False(await keyed.Auth.VerifyToken());
        var before = keyed.Requests.Count;
        Assert.Equal(HttpStatusCode.Unauthorized, (await keyed.Data.GetStats(site.Range())).Status);
        Assert.Equal(before + 1, keyed.Requests.Count);
        Assert.DoesNotContain(keyed.Requests, item => item.Path.StartsWith("/api/auth/"));
    }

    [DisposableUmamiFact]
    public async Task ServerValidationErrorsNotFoundAndCancellationRemainDistinct()
    {
        await using var site = await server.CreateSiteAsync();
        var incomplete = site.Analytics();
        var invalid = await site.Data.GetAnalyticsAsync(AnalyticsEndpoint.FunnelStats, incomplete);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.Status);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Data.GetAnnotationAsync(Guid.NewGuid().ToString())).Status);
        var missing = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, "missing-sdk-resource");
        Assert.Equal(HttpStatusCode.NotFound, missing.Status);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => site.Data.GetAnalyticsAsync(AnalyticsEndpoint.Events, site.Analytics(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => site.Client.SendAsync(site.Payload(), cancellationToken: cancellation.Token));
        Assert.Equal(0, (await site.Data.GetStats(site.Range())).Data!.pageviews.value);
    }
    [DisposableUmamiFact]
    public async Task CollectorValidationFailureIsReturnedAndNotCounted()
    {
        await using var site = await server.CreateSiteAsync();
        var invalid = site.Payload(name: "=formula-not-allowed");
        using var response = await site.Client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Failed, (await site.Client.SendAndDecode(invalid))!.Status);
        Assert.Equal(0, (await site.Data.GetStats(site.Range())).Data!.pageviews.value);
    }

    [DisposableUmamiFact]
    public async Task RefusedConnectionProducesServiceUnavailable()
    {
        // Reserve then release a loopback port rather than assuming a particular port is unused.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var address = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        listener.Stop();
        using var offline = new TestDataClient(address, new UmamiDataSettings { WebsiteId = Guid.NewGuid().ToString() });
        var result = await offline.Data.GetActiveUsers().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Status);
    }

    [DisposableUmamiFact]
    public async Task RealSocketTimeoutAndCallerCancellationAreNotConflated()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var address = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        // Accept a real TCP connection and deliberately withhold its HTTP response.
        using (var timeout = new TestDataClient(address, new UmamiDataSettings { WebsiteId = Guid.NewGuid().ToString() }))
        {
            timeout.Http.Timeout = TimeSpan.FromMilliseconds(150);
            var request = timeout.Data.GetActiveUsers();
            using var accepted = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.GatewayTimeout, (await request.WaitAsync(TimeSpan.FromSeconds(5))).Status);
        }
        using var cancelled = new TestDataClient(address, new UmamiDataSettings { WebsiteId = Guid.NewGuid().ToString() });
        using var token = new CancellationTokenSource();
        var operation = cancelled.Data.GetAnalyticsAsync(AnalyticsEndpoint.Events,
            new() { StartAtDate = DateTime.UtcNow.AddDays(-1), EndAtDate = DateTime.UtcNow }, token.Token);
        using var connection = await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
        token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

}
