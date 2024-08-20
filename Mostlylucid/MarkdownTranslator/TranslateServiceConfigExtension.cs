namespace Mostlylucid.MarkdownTranslator;

public static class TranslateServiceConfigExtension
{
    public static void SetupTranslateService(this IServiceCollection services)
    {
    
        services.AddHttpClient<MarkdownTranslatorService>(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddHostedService<BackgroundTranslateService>();
        services.AddScoped<BackgroundTranslateService>();
        services.AddSingleton<TranslateCacheService>();
    }

    public static async Task Translate(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<BackgroundTranslateService>();
        await cacheService.TranslateAllFilesAsync();
    }
    
    
}