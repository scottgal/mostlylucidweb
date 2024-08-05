namespace Mostlylucid.MarkdownTranslator;

public static class TranslateServiceConfigExtension
{
    public static void SetupTranslateService(this IServiceCollection services)
    {
            

        services.AddScoped<MarkdownTranslatorService>();
    
        services.AddHttpClient<MarkdownTranslatorService>(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(120);
            //options.BaseAddress = new Uri("http://localhost:24080");
        });
        services.AddHostedService<BackgroundTranslateService>();
    }
}