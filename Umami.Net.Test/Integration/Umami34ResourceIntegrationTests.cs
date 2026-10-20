using System.Net;
using System.Text.Json;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.Test.Integration;

public partial class Umami34IntegrationTests
{
    [DisposableUmamiFact]
    public async Task AnnotationsSupportReadUpdateDateFilteringPagingAndDeletion()
    {
        await using var site = await server.CreateSiteAsync();
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        var first = await site.Data.CreateAnnotationAsync(new(yesterday, "Initial deployment", false));
        var second = await site.Data.CreateAnnotationAsync(new(DateTimeOffset.UtcNow, "Today deployment", true));
        Assert.Equal(HttpStatusCode.OK, first.Status); Assert.Equal(HttpStatusCode.OK, second.Status);
        var id = first.Data!.Id;
        var read = await site.Data.GetAnnotationAsync(id);
        Assert.Equal("Initial deployment", read.Data!.Note); Assert.False(read.Data.AllDay);
        var updated = await site.Data.UpdateAnnotationAsync(id, new(yesterday, "Corrected deployment", true));
        Assert.Equal("Corrected deployment", updated.Data!.Note); Assert.True(updated.Data.AllDay);
        var page = await site.Data.GetAnnotationsAsync(page: 1, pageSize: 1);
        Assert.Equal(2, page.Data!.Count); Assert.Single(page.Data.Data);
        var next = await site.Data.GetAnnotationsAsync(page: 2, pageSize: 1);
        Assert.NotEqual(page.Data.Data[0].Id, Assert.Single(next.Data!.Data).Id);
        var filtered = await site.Data.GetAnnotationsAsync(yesterday.AddMinutes(-1), yesterday.AddMinutes(1));
        Assert.Equal(id, Assert.Single(filtered.Data!.Data).Id);
        Assert.True((await site.Data.DeleteAnnotationAsync(id)).Data);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Data.GetAnnotationAsync(id)).Status);
        Assert.Equal(second.Data!.Id, Assert.Single((await site.Data.GetAnnotationsAsync()).Data!.Data).Id);
    }

    [DisposableUmamiTheory]
    [InlineData("goals")]
    [InlineData("funnels")]
    public async Task GenericResourcesSupportSavedDefinitionLifecycleAndStats(string resource)
    {
        await using var site = await server.CreateSiteAsync(seed: true);
        object parameters = resource == "goals"
            ? new { type = "path", value = "/checkout" }
            : new { window = 60, steps = new[] { new { type = "path", value = "/landing" }, new { type = "path", value = "/checkout" } } };
        var created = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Post, resource,
            body: new { name = "Integration definition", description = "Created through SDK", parameters });
        Assert.Equal(HttpStatusCode.OK, created.Status);
        var id = created.Data.GetProperty("id").GetString()!;
        var list = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, resource,
            new Dictionary<string, string?> { ["page"] = "1", ["pageSize"] = "1", ["search"] = "Integration" });
        Assert.Equal(HttpStatusCode.OK, list.Status);
        Assert.Contains(list.Data.GetProperty("data").EnumerateArray(), item => item.GetProperty("id").GetString() == id);
        var read = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, resource + "/" + id);
        Assert.Equal("Integration definition", read.Data.GetProperty("name").GetString());
        var updated = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Post, resource + "/" + id,
            body: new { name = "Updated definition", parameters });
        Assert.Equal(HttpStatusCode.OK, updated.Status);
        Assert.Equal("Updated definition", updated.Data.GetProperty("name").GetString());
        var stats = await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, resource + "/" + id + "/stats", site.DateQuery());
        Assert.Equal(HttpStatusCode.OK, stats.Status);
        Assert.Contains(stats.Data.ValueKind, new[] { JsonValueKind.Array, JsonValueKind.Object });
        var deleted = await site.Data.SendWebsiteRequestAsync<bool>(HttpMethod.Delete, resource + "/" + id);
        Assert.True(deleted.Data);
        Assert.Equal(HttpStatusCode.NotFound, (await site.Data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, resource + "/" + id)).Status);
    }
}
