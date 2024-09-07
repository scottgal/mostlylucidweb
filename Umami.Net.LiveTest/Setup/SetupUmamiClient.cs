using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.UmamiData;

namespace Umami.Net.LiveTest.Setup;

public static class SetupUmamiClient
{
    public static IServiceProvider Setup(string settingsfile = "appsettings.json")
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddJsonFile(settingsfile)
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();
        var umamiSettings= services.ConfigurePOCO<UmamiClientSettings>(config.GetSection(UmamiClientSettings.Section));
        services.AddSingleton(umamiSettings);
        services.AddTransient<HttpLogger>();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
                ;
                client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddLogger<HttpLogger>();

        services.AddSingleton<IHttpContextAccessor>(SetupHttpContextAccessor());
        services.AddScoped<PayloadService>();
        services.AddSingleton<UmamiBackgroundSender>();
        
        services.AddHostedService<UmamiBackgroundSender>(provider => provider.GetRequiredService<UmamiBackgroundSender>());
        return services.BuildServiceProvider();
    }

    
    public static HttpContextAccessor SetupHttpContextAccessor(string host = "www.mostylucid.net",
        string path = "/", string ip = "127.0.0.1", string userAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome",
        string referer = "https://www.mostylucid.net")
    {
        HttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = new PathString(path);
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.Request.Headers.UserAgent = new StringValues( userAgent);
        httpContext.Request.Headers.Referer = referer;

        var context = new HttpContextAccessor { HttpContext = httpContext };
        return context;
    }
}