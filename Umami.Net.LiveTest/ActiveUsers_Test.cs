using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.LiveTest;

public class ActiveUsers_Test
{
    [Fact]
    public async Task Stats_StartEndForUrl()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var activeUsers = await websiteDataService.GetActiveUsers();
        Assert.NotNull(activeUsers);
        Assert.Equal( HttpStatusCode.OK, activeUsers.Status);

    }
}