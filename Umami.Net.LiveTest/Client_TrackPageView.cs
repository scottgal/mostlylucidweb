using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Umami.Net.LiveTest.Setup;
using Umami.Net.Models;

namespace Umami.Net.LiveTest;

public class Client_TrackPageView
{
    [Fact]
    public async Task Client_TrackPageView_Test()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.TrackPageViewAndDecode("/test",
            payload: new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(resp);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, resp.Status);
    }

    [Fact]
    public async Task Client_TrackPageView_Bot()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.TrackPageViewAndDecode("/test", payload: new UmamiPayload { UserAgent = "Bot" });
        Assert.NotNull(resp);
        Assert.Equal(UmamiDataResponse.ResponseStatus.BotDetected, resp.Status);
    }

    [Fact]
    public async Task Background_TrackPageView_NoDecode()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiBackgroundSender>();
        await umamiClient.TrackPageView("/test", "test", useDefaultUserAgent:true);
    }
    
    [Fact]
    public async Task Background_TrackPageView_Bot()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiBackgroundSender>();
        await umamiClient.TrackPageView("/test", "test", new UmamiPayload() { UserAgent = "Bot" });
       await Task.Delay(1000);
        
    }


    [Fact]
    public async Task Client_TrackPageView_FeedBin()
    {
        var services = SetupUmamiClient.Setup();
        var umamiClient = services.GetRequiredService<UmamiClient>();
        var resp = await umamiClient.TrackPageViewAndDecode("/test");
        Assert.NotNull(resp);
    }
}