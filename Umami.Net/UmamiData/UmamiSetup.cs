using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Umami.Net.Config;

namespace Umami.Net.UmamiData;

public static class UmamiSetup
{
    private static bool ValidateSetup(UmamiDataSettings umamiSettings)
    {
        if(!Uri.TryCreate(umamiSettings.UmamiPath, UriKind.Absolute, out _))
        {
            return false;
        }
        if(string.IsNullOrEmpty(umamiSettings.Username) || string.IsNullOrEmpty(umamiSettings.Password))
        {
            return false;
        }
        return true;
    }
    
    public static void SetupUmamiData(this IServiceCollection services, IConfiguration config)
    {
     
        var umamiSettings = services.ConfigurePOCO<UmamiDataSettings>(config.GetSection(UmamiClientSettings.Section));
        if(!ValidateSetup(umamiSettings)) throw new Exception("Invalid UmamiDataSettings");
        services.AddHttpClient<AuthService>(options =>
        {
            options.BaseAddress = new Uri(umamiSettings.UmamiPath);
            
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