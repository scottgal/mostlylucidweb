using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Config;
using Umami.Net.UmamiData;

namespace Umami.Net.Test.UmamiData;

public static class SetupExtensions
{
    public static void SetupUmamiData(this IServiceCollection services, string username = "username",
        string password = "password")
    {
        var umamiSettings = new UmamiDataSettings
        {
            UmamiPath = Consts.UmamiPath,
            Username = username,
            Password = password,
            WebsiteId = Consts.WebSiteId
        };
        services.AddSingleton(umamiSettings);
        services.AddHttpClient<AuthService>((provider, client) =>
            {
                client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            }).AddHttpMessageHandler<UmamiDataDelegatingHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5)); //Set lifetime to five minutes

        services.AddScoped<UmamiDataDelegatingHandler>();
        services.AddScoped<UmamiDataService>();
    }
}