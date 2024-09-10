using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;

namespace Umami.Net.LiveTest;

public class Client_Identify_Test
{
    [Fact]
    public async Task IdentifySession_ReturnsUserInfo()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.IdentifyAndDecode();
        Assert.NotNull(resp);
    }


    [Fact]
    public async Task PageView_ReturnsUserInfo()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.TrackPageViewAndDecode("/test");
        Assert.NotNull(resp);
    }
}