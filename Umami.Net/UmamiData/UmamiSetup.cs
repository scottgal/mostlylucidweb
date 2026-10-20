using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Config;
using Umami.Net.Helpers;

namespace Umami.Net.UmamiData;

public static class UmamiSetup
{
    private static bool ValidateSetup(UmamiDataSettings umamiSettings)
    {
        if (!Uri.TryCreate(umamiSettings.UmamiPath, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return false;
        if (string.IsNullOrWhiteSpace(umamiSettings.ApiKey) && (string.IsNullOrEmpty(umamiSettings.Username) || string.IsNullOrEmpty(umamiSettings.Password))) return false;
        if (!Guid.TryParseExact(umamiSettings.WebsiteId, "D", out _)) return false;
        if (umamiSettings.UseCloudApi && string.IsNullOrWhiteSpace(umamiSettings.ApiKey)) return false;
        return true;
    }

    public static void SetupUmamiData(this IServiceCollection services, IConfiguration config)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var umamiSettings = services.ConfigurePOCO<UmamiDataSettings>(config.GetSection(UmamiClientSettings.Section));
        if (!ValidateSetup(umamiSettings)) throw new Exception("Invalid UmamiDataSettings");
        var httpClientBuilder = services.AddHttpClient<AuthService>(options =>
            {
                options.BaseAddress = new Uri(umamiSettings.UmamiPath.TrimEnd('/') + "/");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5)) //Set lifetime to five minutes
            .AddPolicyHandler(request => request.Method == HttpMethod.Get
                ? RetryPolicyExtension.GetRetryPolicy() : Polly.Policy.NoOpAsync<HttpResponseMessage>());

        if (isDevelopment)
        {
            services.AddTransient<HttpLogger>();
            httpClientBuilder.AddLogger<HttpLogger>();
        }

        services.AddScoped<UmamiDataService>();
    }
}