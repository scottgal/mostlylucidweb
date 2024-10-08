﻿using Mostlylucid.Blog.Markdown;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Services.Interfaces;

namespace Mostlylucid.MarkdownTranslator;

public static class TranslateServiceConfigExtension
{
    public static void SetupTranslateService(this IServiceCollection services)
    {
    
        services.AddHttpClient<IMarkdownTranslatorService, MarkdownTranslatorService>(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(120);
        });
        services.AddSingleton<IBackgroundTranslateService, BackgroundTranslateService>(); 
        services.AddHostedService(provider => provider.GetRequiredService<IBackgroundTranslateService>());

        services.AddSingleton<TranslateCacheService>();
        services.AddScoped<IMarkdownFileBlogService, MarkdownBlogViewService>();
    }
}