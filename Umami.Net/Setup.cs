using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
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
       var umamiSettings= services.ConfigurePOCO<UmamiClientSettings>(config.GetSection(UmamiClientSettings.Section));
         ValidateSettings(umamiSettings);
          services.AddSingleton(umamiSettings);
       services.AddTransient<HttpLogger>();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
         ;
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

        services.AddHttpContextAccessor();
        services.AddScoped<PayloadService>();
        services.AddSingleton<UmamiBackgroundSender>();
        
        services.AddHostedService<UmamiBackgroundSender>(provider => provider.GetRequiredService<UmamiBackgroundSender>());
    }
    
 
    
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}