using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Models;

namespace Mostlylucid.Services;

// Only public metadata is requested. No GitHub credential is needed or exposed to visitors.
public class GitHubActivityService(
    IHttpClientFactory clients,
    IMemoryCache cache,
    ILogger<GitHubActivityService> logger)
{
    private const string CacheKey = "github:scottgal:activity";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<RepositoryActivity> _lastGood = Array.Empty<RepositoryActivity>();

    public async Task<RepositoryActivityViewModel> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<RepositoryActivityViewModel>(CacheKey, out var cached)) return cached!;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<RepositoryActivityViewModel>(CacheKey, out cached)) return cached!;
            try
            {
                var client = clients.CreateClient("GitHubActivity");
                var repositories = await client.GetFromJsonAsync<List<GitHubRepository>>(
                    "users/scottgal/repos?sort=pushed&direction=desc&per_page=30", cancellationToken) ?? new();
                var active = repositories.Where(repo => !repo.Fork && !repo.Archived && !repo.Private
                    && repo.PushedAt.HasValue && !string.IsNullOrEmpty(repo.Name)).Take(5);
                var results = new List<RepositoryActivity>();
                foreach (var repo in active)
                {
                    // A release list also includes prereleases, matching the shields.io badge.
                    var releases = await client.GetFromJsonAsync<List<GitHubRelease>>(
                        $"repos/scottgal/{Uri.EscapeDataString(repo.Name)}/releases?per_page=1", cancellationToken) ?? new();
                    results.Add(new RepositoryActivity(repo.Name, repo.Description ?? "",
                        repo.PushedAt!.Value, releases.Count > 0));
                }
                _lastGood = results;
                var model = new RepositoryActivityViewModel(results);
                cache.Set(CacheKey, model, TimeSpan.FromMinutes(30));
                return model;
            }
            catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException
                || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Could not refresh public GitHub repository activity");
                var model = new RepositoryActivityViewModel(_lastGood, IsStale: true);
                cache.Set(CacheKey, model, TimeSpan.FromMinutes(5));
                return model;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record GitHubRepository(
        string Name, string? Description, bool Fork, bool Archived, bool Private,
        [property: JsonPropertyName("pushed_at")] DateTimeOffset? PushedAt);

    private sealed record GitHubRelease([property: JsonPropertyName("tag_name")] string TagName);
}
