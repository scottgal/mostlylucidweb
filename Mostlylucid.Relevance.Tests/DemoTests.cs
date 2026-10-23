using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mostlylucid.Relevance.Tests;

public sealed class DemoTests
{
    [Fact]
    public async Task ShopAndDiagnosticInterventionsUseTheSameLocalEngine()
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Demo:DataPath", Path.Combine(Path.GetTempPath(), "relevance-web-test-" + Guid.NewGuid().ToString("N"))));
        using var client = app.CreateClient();
        var products = await client.GetFromJsonAsync<JsonElement[]>("/api/shop/products");
        Assert.Equal(84, products!.Length);
        var product = products.First(p => p.GetProperty("available").GetBoolean() && p.GetProperty("category").GetString() == "outdoors");
        var session = Guid.NewGuid().ToString();
        var viewed = await client.PostAsJsonAsync("/api/shop/events", new { session, product = product.GetProperty("id").GetString(), type = "view" });
        viewed.EnsureSuccessStatusCode();
        var recommendations = await client.GetFromJsonAsync<RecommendationResult>($"/api/shop/recommendations?session={session}&category=outdoors&budget=120");
        Assert.NotEmpty(recommendations!.Items);
        Assert.All(recommendations.Items, item =>
        {
            Assert.Equal("outdoors", item.Data.GetProperty("category").GetString());
            Assert.True(item.Data.GetProperty("available").GetBoolean());
            Assert.True(item.Data.GetProperty("price").GetDecimal() <= 120);
            Assert.Equal(3, item.Reasons.Count);
        });
        var purchased = await client.PostAsJsonAsync("/api/shop/events", new { session, product = recommendations.Items[0].Id, type = "purchase", recommendation = recommendations.Id });
        purchased.EnsureSuccessStatusCode();
        var purchaseResult = await purchased.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("observed_destination", purchaseResult.GetProperty("prediction").GetProperty("status").GetString());

        var incident = await client.PostAsJsonAsync("/api/operations/scenarios/traffic", new { });
        incident.EnsureSuccessStatusCode();
        var ops = await client.GetFromJsonAsync<JsonElement>("/api/operations");
        Assert.Equal("degraded", ops.GetProperty("telemetry").GetProperty("state").GetString());
        var top = ops.GetProperty("telemetry").GetProperty("suggestions").GetProperty("items")[0];
        Assert.Equal("tighten-rate-limit", top.GetProperty("id").GetString());
        Assert.Contains(ops.GetProperty("telemetry").GetProperty("logs").EnumerateArray(), log => log.GetProperty("synthetic").GetBoolean());
        var applied = await client.PostAsJsonAsync("/api/operations/actions/tighten-rate-limit", new { });
        applied.EnsureSuccessStatusCode();
        ops = await client.GetFromJsonAsync<JsonElement>("/api/operations");
        Assert.Equal(60, ops.GetProperty("policy").GetProperty("limit").GetInt32());
        Assert.Single(ops.GetProperty("policy").GetProperty("interventions").EnumerateArray());

        var limited = false;
        for (var i = 0; i < 65; i++)
        {
            using var request = await client.GetAsync("/api/shop/products");
            if (request.StatusCode == HttpStatusCode.TooManyRequests) { limited = true; break; }
        }
        Assert.True(limited);
        (await client.PostAsJsonAsync("/api/operations/reset", new { })).EnsureSuccessStatusCode();
        (await client.GetAsync("/api/shop/products")).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/operations/scenarios/recovery", new { })).EnsureSuccessStatusCode();
        ops = await client.GetFromJsonAsync<JsonElement>("/api/operations");
        Assert.Equal("recovered", ops.GetProperty("telemetry").GetProperty("state").GetString());
        Assert.Equal(JsonValueKind.Null, ops.GetProperty("telemetry").GetProperty("suggestions").ValueKind);
        Assert.Equal(240, ops.GetProperty("policy").GetProperty("limit").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/operations/actions/tighten-rate-limit", new { })).StatusCode);
    }
}
