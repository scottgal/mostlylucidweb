using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Mostlylucid.Blog;
using Mostlylucid.Blog.Markdown;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Config.Markdown;

namespace Mostlylucid.Test.TranslationService;

public static class Setup
{
 

    public static IServiceCollection AddMarkdownTranslatorServiceCollection(this IServiceCollection services,
        DelegatingHandler? handler = null)
    {
        var translateServiceConfig = new TranslateServiceConfig();
        services.AddSingleton<ILogger<IMarkdownTranslatorService>>(__=> new FakeLogger<IMarkdownTranslatorService>());
        services.AddTransient<DelegatingHandler>(_=> handler ?? new TranslateDelegatedHandler());
        translateServiceConfig.Languages = new[] {"es",
            "fr",
            "de",
            "it",
            "zh",
            "nl",
            "hi",
            "ar",
            "uk",
            "fi",
            "sv",
            "el"
        };

        translateServiceConfig.IPs = new[] { $"http://{Consts.GoodHost}:24080",$"http://{Consts.BadHost}:24080" };
        services.AddSingleton(translateServiceConfig);
        
        var httpClientBuilder = services.AddHttpClient<IMarkdownTranslatorService, MarkdownTranslatorService>(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(120);
        });

        // Add the handler dynamically
        if (handler != null)
        {
            // Use the provided custom DelegatingHandler
            httpClientBuilder.AddHttpMessageHandler(() => handler);
        }
        else
        {
            // Use the default TranslateDelegatedHandler if no custom handler is passed
            httpClientBuilder.AddHttpMessageHandler<DelegatingHandler>();
        }
        return services;
    }
    
    public static IServiceCollection AddTestServices(this IServiceCollection services, DelegatingHandler handler = null)
    {
        ILogger<IBackgroundTranslateService> logger = new FakeLogger<IBackgroundTranslateService>();
        services.AddSingleton(logger);
        var markdownConfig = new MarkdownConfig();

        services.AddSingleton<MarkdownConfig>(_=> markdownConfig);
        services.AddMarkdownTranslatorServiceCollection(handler);
        services.AddSingleton<IBackgroundTranslateService, BackgroundTranslateService>(); 
        services.AddSingleton<TranslateCacheService>();
        services.AddScoped<IMarkdownFileBlogService, MarkdownBlogViewService>();
        return services;
    }
}