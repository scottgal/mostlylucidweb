using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Umami.Net.Config;
using Umami.Net.Helpers;

namespace Umami.Net;

public static class Setup
{
    public static void ValidateSettings(UmamiClientSettings settings)
    {
        if (string.IsNullOrEmpty(settings.UmamiPath)) throw new ArgumentNullException(settings.UmamiPath, "UmamiUrl is required");
        if(!Uri.TryCreate(settings.UmamiPath, UriKind.Absolute, out _))
            throw new FormatException("UmamiUrl must be a valid Uri");
        if (string.IsNullOrEmpty(settings.WebsiteId)) throw new ArgumentNullException(settings.WebsiteId, "WebsiteId is required");
        if (!Guid.TryParseExact(settings.WebsiteId, "D", out _))
            throw new FormatException("WebSiteId must be a valid Guid");
    }
    public static void SetupUmamiClient(this IServiceCollection services, IConfiguration config)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
       var umamiSettings= services.ConfigurePOCO<UmamiClientSettings>(config.GetSection(UmamiClientSettings.Section));
         ValidateSettings(umamiSettings);
          services.AddSingleton(umamiSettings);
     
        var httpClientBuilder = services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<UmamiClientSettings>();
                client.BaseAddress = new Uri(settings.UmamiPath);
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
            .AddPolicyHandler(RetryPolicyExtension.GetRetryPolicy());
        
        if (isDevelopment)
        {
            services.AddTransient<HttpLogger>();
            httpClientBuilder.AddLogger<HttpLogger>();
         
        }
        services.AddHttpContextAccessor();
        services.AddScoped<PayloadService>();
        services.AddSingleton<JwtDecoder>();
        services.AddSingleton<UmamiBackgroundSender>();
        services.AddHostedService<UmamiBackgroundSender>(provider => provider.GetRequiredService<UmamiBackgroundSender>());
    }
    
 
    
  
}