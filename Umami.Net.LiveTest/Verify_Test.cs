using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;

namespace Umami.Net.LiveTest;

public class Verify_Test
{
    [Fact]
    public async Task Verify_Test_Fail()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var authService = serviceProvider.GetRequiredService<AuthService>();

        var activeUsers = await authService.VerifyToken();
        Assert.False(activeUsers);

    }
    
    [Fact]
    public async Task Verify_Test_LoginSuccess()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var authService = serviceProvider.GetRequiredService<AuthService>();

        var activeUsers = await authService.VerifyToken();
        Assert.False(activeUsers);
        var login = await authService.Login();
        Assert.True(login);
        var activeUsers2 = await authService.VerifyToken();
        Assert.True(activeUsers2);

    }
}