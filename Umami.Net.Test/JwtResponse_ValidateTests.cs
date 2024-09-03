using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test;

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
}