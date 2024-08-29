using System.Net;
using System.Net.Http.Json;

namespace Umami.Net.Test;

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
}