using Mostlylucid.Blog;
using Mostlylucid.Blog.Markdown;

namespace Mostlylucid.MarkdownTranslator;

public static class TranslateServiceConfigExtension
{
    public static void SetupTranslateService(this IServiceCollection services)
    {
    
        services.AddHttpClient<MarkdownTranslatorService>(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddSingleton<BackgroundTranslateService>(); 
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundTranslateService>());
        
       services.AddSingleton<TranslateCacheService>();
        services.AddScoped<IMarkdownFileBlogService, MarkdownBlogService>();
    }

  
    
    
}