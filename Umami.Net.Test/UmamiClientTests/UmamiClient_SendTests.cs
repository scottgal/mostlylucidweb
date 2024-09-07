using System.Net;
using Umami.Net.Test.Extensions;

namespace Umami.Net.Test.UmamiClientTests;

public class UmamiClient_SendTests
{
    [Fact]
    public async Task Send_Wrong_Type()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        await Assert.ThrowsAsync<ArgumentException>(async () => await umamiClient.Send(type: "boop"));
    }

    [Fact]
    public async Task Send_Empty_Success()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Send();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}