using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

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

    [Fact]
    public async Task Send_WithDistinctId_FlowsToPayloadIdField()
    {
        var payload = new UmamiPayload { DistinctId = Consts.DistinctId };
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Send(payload);


        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.NotNull(content.Payload);
        Assert.Equal(Consts.DistinctId, content.Payload.DistinctId);
    }
}