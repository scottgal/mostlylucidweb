using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;

namespace Umami.Net.LiveTest;

public class UmamiData_LoginTest
{
    [Fact]
    public async Task SetupTest_Good()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var authService = serviceProvider.GetRequiredService<AuthService>();
        var result = await authService.Login();
        Assert.True(result);
    }

    [Fact]
    public void SetupTest_Bad()
    {
        var setup = new SetupUmamiData();
        Assert.Throws<Exception>(() => setup.Setup("appsettings_bad.json"));
    }
}