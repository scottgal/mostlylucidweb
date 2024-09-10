using System.Net;
using System.Net.Http.Json;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test.UmamiClientTests;

public class UserAgent_Tests
{
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var contextAccessor = SetupExtensions.SetupHttpContextAccessor(userAgent: "Feedbin 1 subscribers");
        var umamiClient = SetupExtensions.GetUmamiClient(contextAccessor: contextAccessor);
        var response = await umamiClient.TrackPageView("https://example.com", "Example Page");

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.NotNull(content.Payload.Url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}