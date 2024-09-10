using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Config;
using Umami.Net.Helpers;

namespace Umami.Net.UmamiData;

public static class UmamiSetup
{
    private static bool ValidateSetup(UmamiDataSettings umamiSettings)
    {
        if (!Uri.TryCreate(umamiSettings.UmamiPath, UriKind.Absolute, out _)) return false;
        if (string.IsNullOrEmpty(umamiSettings.Username) || string.IsNullOrEmpty(umamiSettings.Password)) return false;
        return true;
    }

    public static void SetupUmamiData(this IServiceCollection services, IConfiguration config)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var umamiSettings = services.ConfigurePOCO<UmamiDataSettings>(config.GetSection(UmamiClientSettings.Section));
        if (!ValidateSetup(umamiSettings)) throw new Exception("Invalid UmamiDataSettings");
        var httpClientBuilder = services.AddHttpClient<AuthService>(options =>
            {
                options.BaseAddress = new Uri(umamiSettings.UmamiPath);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5)) //Set lifetime to five minutes
            .AddPolicyHandler(RetryPolicyExtension.GetRetryPolicy());

        if (isDevelopment)
        {
            services.AddTransient<HttpLogger>();
            httpClientBuilder.AddLogger<HttpLogger>();
        }

        services.AddScoped<UmamiDataService>();
    }
}