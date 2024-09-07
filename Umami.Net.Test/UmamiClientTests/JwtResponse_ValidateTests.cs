using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test.UmamiClientTests;

public class JwtResponse_ValidateTests
{
    private UmamiClient GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var umamiClient = serviceProvider.GetRequiredService<UmamiClient>();
        return umamiClient;
    }

    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page");
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Identify_WithSessionId()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.IdentifyAndDecode(sessionId: "1234");
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }
    
    [Fact]
    public async Task Identify_Empty()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.IdentifyAndDecode();
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }

    [Fact]
    public async Task Track_NamedEvent()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackAndDecode("RSS");
        Assert.NotNull(response);
        Assert.Equal("Chrome", response.Browser);
        Assert.Equal("Windows 10", response.Os);
    }
}