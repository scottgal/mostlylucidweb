using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Models;

namespace Umami.Net.LiveTest.Setup;

public class Client_TrackPageView
{

    [Fact]
    public async Task Client_TrackPageView_Test()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.TrackPageViewAndDecode("/test");
        Assert.NotNull(resp);
    }
}