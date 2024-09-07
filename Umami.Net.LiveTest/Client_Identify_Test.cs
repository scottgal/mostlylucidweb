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
       var resp= await umamiClient.IdentifyAndDecode(new UmamiPayload(){UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36"});
        Assert.NotNull(resp);
    }
    
    
    [Fact]
    public async Task PageView_ReturnsUserInfo()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp= await umamiClient.TrackPageViewAndDecode( url: "/test", payload: new UmamiPayload(){UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36"});
        Assert.NotNull(resp);
    }
}