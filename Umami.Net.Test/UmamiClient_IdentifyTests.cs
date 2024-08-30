using System.Net.Http.Json;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test;

public class UmamiClient_IdentifyTests
{
    [Fact]
    public async Task Send_Session()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.IdentifySession(Consts.SessionId);
        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.Equal(Consts.SessionId, content.Payload.SessionId);
    }
    
    [Fact]
    public async Task Send_User()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Identify(email:Consts.Email, username:Consts.UserName);
        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.NotNull(content.Payload.Data);
        Assert.Equal(Consts.Email, content.Payload.Data["email"].ToString());
        Assert.Equal(Consts.UserName, content.Payload.Data["username"].ToString());
    }
    
    [Fact]
    public async Task Send_UserId()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Identify(email:Consts.Email, username:Consts.UserName, userId:Consts.UserId);
        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.NotNull(content.Payload.Data);
        Assert.Equal(Consts.Email, content.Payload.Data["email"].ToString());
        Assert.Equal(Consts.UserName, content.Payload.Data["username"].ToString());
        Assert.Equal(Consts.UserId, content.Payload.Data["userId"].ToString());
    }
    
}