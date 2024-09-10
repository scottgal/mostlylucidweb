using System.Net;
using System.Net.Http.Json;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test.UmamiClientTests;

public class UmamiClient_TrackTests
{
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.TrackPageView("https://example.com", "Example Page");

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.NotNull(content.Payload.Url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TrackPageView_WithNoUrl()
    {
        var defaultUrl = "/testpath";
        var contextAccessor = SetupExtensions.SetupHttpContextAccessor(path: "/testpath");
        var umamiClient = SetupExtensions.GetUmamiClient(contextAccessor: contextAccessor);
        var response = await umamiClient.TrackPageView();

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.Equal(content.Payload.Url, defaultUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TrackEvent()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Track(Consts.DefaultName);
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(content);
        Assert.Equal(Consts.DefaultType, content.Type);
        Assert.Equal(Consts.DefaultName, content.Payload.Name);
    }

    [Fact]
    public async Task Track_FullEvent()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var payload = new UmamiPayload { Name = Consts.DefaultName };
        var eventData = new UmamiEventData { { "string", "value" } };
        var response = await umamiClient.Track(payload, eventData);
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(content);
        Assert.Equal(Consts.DefaultType, content.Type);
        Assert.Equal(Consts.DefaultName, content.Payload.Name);
        Assert.NotNull(content.Payload.Data);
        Assert.Equal("value", content.Payload.Data["string"].ToString());
    }
}