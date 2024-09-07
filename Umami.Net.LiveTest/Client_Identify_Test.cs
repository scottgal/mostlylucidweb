using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.Models;

namespace Umami.Net.LiveTest;

public class Client_Identify_Test
{
    [Fact]
    public async Task IdentifySession_ReturnsUserInfo()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
       var resp= await umamiClient.IdentifyAndDecode();
        Assert.NotNull(resp);
    }
    
    
    [Fact]
    public async Task PageView_ReturnsUserInfo()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp= await umamiClient.TrackPageViewAndDecode( url: "/test");
        Assert.NotNull(resp);
    }
}