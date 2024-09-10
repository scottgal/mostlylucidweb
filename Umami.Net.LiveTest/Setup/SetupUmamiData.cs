using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.UmamiData;

namespace Umami.Net.LiveTest.Setup;

public class SetupUmamiData
{
    public IServiceProvider Setup(string settingsfile = "appsettings.json")
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsfile)
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();
        services.SetupUmamiData(config);
        return services.BuildServiceProvider();
    }
}