using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mostlylucid.Relevance.Tests;

public sealed class EmailDemoTests
{
    [Fact]
    public async Task ActualShopObservationsDriveEmailChoiceAndObservedResponse()
    {
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Demo:DataPath", Path.Combine(Path.GetTempPath(), "email-web-" + Guid.NewGuid().ToString("N"))));
        using var client = app.CreateClient();
        var visitResponse = await client.PostAsJsonAsync("/api/learning/visits/comparison", new { });
        visitResponse.EnsureSuccessStatusCode();
        var visit = await visitResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, visit.GetProperty("views").GetInt32()); Assert.Equal(8, visit.GetProperty("comparisons").GetInt32());
        var id = Guid.NewGuid().ToString("N");
        var chosen = await client.PostAsJsonAsync("/api/learning/choose", new { id, session = visit.GetProperty("session").GetString() });
        chosen.EnsureSuccessStatusCode();
        var choice = await chosen.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("detected-", choice.GetProperty("decision").GetProperty("group").GetString());
        var email = choice.GetProperty("email");
        Assert.NotEmpty(email.GetProperty("structure").GetProperty("links").EnumerateArray());
        var eventId = Guid.NewGuid().ToString("N");
        (await client.PostAsJsonAsync("/api/learning/events", new { id = eventId, decision = id, stage = "purchase", reward = 99 })).EnsureSuccessStatusCode();
        var snapshot = await client.GetFromJsonAsync<JsonElement>("/api/learning");
        var evidence = snapshot.GetProperty("snapshot").GetProperty("evidence").EnumerateArray().Single(e => e.GetProperty("assigned").GetInt64() == 1);
        Assert.Equal(1, evidence.GetProperty("completed").GetInt64()); Assert.Equal(0, evidence.GetProperty("viewed").GetInt64());
        Assert.InRange(evidence.GetProperty("meanReward").GetDouble(), 0, 1); // Browser-supplied reward was ignored.
        var draft = email.GetProperty("draft");
        var request = new { subject = draft.GetProperty("subject").GetString() + " / human edit", html = draft.GetProperty("html").GetString(),
            originalPrice = draft.GetProperty("originalPrice").GetDecimal(), offerPrice = draft.GetProperty("offerPrice").GetDecimal(),
            delayMinutes = draft.GetProperty("delayMinutes").GetInt32(), reviewed = true, author = "human" };
        (await client.PostAsJsonAsync("/api/learning/register", request)).EnsureSuccessStatusCode();
        snapshot = await client.GetFromJsonAsync<JsonElement>("/api/learning");
        Assert.Equal(49, snapshot.GetProperty("snapshot").GetProperty("variants").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/learning/choose", new { id = "another", session = visit.GetProperty("session").GetString() })).StatusCode);
    }
}
