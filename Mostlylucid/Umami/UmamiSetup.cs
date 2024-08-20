using System.Net;
using Mostlylucid.Config;
using Polly;
using Polly.Extensions.Http;

namespace Mostlylucid.Umami;

public static class UmamiSetup
{
    public static void SetupUmamiServices(this IServiceCollection services, IConfiguration config)
    {
        var umamiSettings = services.ConfigurePOCO<UmamiSettings>(config.GetSection(UmamiSettings.Section));
        services.AddHttpClient<AuthService>(options =>
        {
            options.BaseAddress = new Uri(umamiSettings.BaseUrl);
            
        }) .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy());;
        services.AddScoped<UmamiService>();

    }
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

}