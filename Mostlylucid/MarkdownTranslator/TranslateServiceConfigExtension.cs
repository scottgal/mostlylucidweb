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
        services.AddSingleton<TranslateCacheService>();
    }
    
    
}