using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mostlylucid.Services;

namespace Mostlylucid.Test;

public class GitHubActivityServiceTests
{
    [Fact]
    public async Task GetAsync_FiltersRepositoriesChecksReleasesAndCachesResults()
    {
        var handler = new ActivityHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("GitHubActivity")).Returns(client);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GitHubActivityService(factory.Object, cache, NullLogger<GitHubActivityService>.Instance);

        var result = await service.GetAsync(CancellationToken.None);
        Assert.Equal(new[] { "stylobot", "new-project" }, result.Repositories.Select(repo => repo.Name));
        Assert.True(result.Repositories[0].HasRelease);
        Assert.False(result.Repositories[1].HasRelease);
        Assert.False(result.IsStale);
        Assert.Same(result, await service.GetAsync(CancellationToken.None));
        Assert.Equal(3, handler.Requests);

        cache.Remove("github:scottgal:activity");
        handler.Fail = true;
        var stale = await service.GetAsync(CancellationToken.None);
        Assert.True(stale.IsStale);
        Assert.Equal(result.Repositories, stale.Repositories);
        Assert.Same(stale, await service.GetAsync(CancellationToken.None));
        Assert.Equal(4, handler.Requests);
    }

    [Fact]
    public async Task GetAsync_FirstFailureReturnsGracefulFallback()
    {
        var handler = new ActivityHandler { Fail = true };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("GitHubActivity")).Returns(client);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = await new GitHubActivityService(factory.Object, cache, NullLogger<GitHubActivityService>.Instance)
            .GetAsync(CancellationToken.None);
        Assert.True(result.IsStale);
        Assert.Empty(result.Repositories);
    }

    private sealed class ActivityHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public bool Fail { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (Fail) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            var json = request.RequestUri!.AbsolutePath switch
            {
                "/users/scottgal/repos" => """
                    [
                      {"name":"fork","fork":true,"pushed_at":"2026-10-19T10:00:00Z"},
                      {"name":"archived","archived":true,"pushed_at":"2026-10-19T09:00:00Z"},
                      {"name":"private","private":true,"pushed_at":"2026-10-19T08:00:00Z"},
                      {"name":"stylobot","description":"Bot detection","pushed_at":"2026-10-18T08:00:00Z"},
                      {"name":"new-project","pushed_at":"2026-10-17T08:00:00Z"}
                    ]
                    """,
                "/repos/scottgal/stylobot/releases" => """[{"tag_name":"v1.0.0"}]""",
                _ => "[]"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
