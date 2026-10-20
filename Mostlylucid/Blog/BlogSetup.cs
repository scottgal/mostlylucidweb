using Mostlylucid.Blog.Markdown;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Blog.WatcherService;
using Mostlylucid.Blog.ValidationService;
using Mostlylucid.DbContext;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.SemanticSearch.Config;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Services.SemanticSearch;
using Mostlylucid.Services.Umami;
using Mostlylucid.Markdig.FetchExtension;
using Mostlylucid.Markdig.FetchExtension.Services;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Config.Markdown;
using Mostlylucid.Shared.Interfaces;
using Mostlylucid.Shared.Services;
using Npgsql;

namespace Mostlylucid.Blog;

public static class BlogSetup
{
    public static void SetupBlog(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var config = services.ConfigurePOCO<BlogConfig>(configuration.GetSection(BlogConfig.Section));
       services.ConfigurePOCO<MarkdownConfig>(configuration.GetSection(MarkdownConfig.Section));

        // Register HttpClient factory for fetching remote markdown
        services.AddHttpClient();

        // Register blog post processing context (scoped to track current blog post during rendering)
        services.AddScoped<BlogPostProcessingContext>();

        // Register markdown fetch service
        services.AddScoped<IMarkdownFetchService, MarkdownFetchService>();

        switch (config.Mode)
        {
            case BlogMode.File:
                Log.Information("Using file based blog");
                services.AddScoped<IBlogViewService, MarkdownBlogViewService>();
                services.AddScoped<IBlogSearchService, MarkdownBlogSearchService>();
                services.AddScoped<IBlogPopulator, MarkdownBlogPopulator>();
                break;
            case BlogMode.Database:
                Log.Information("Using Database based blog");
                services.SetupDatabase(configuration, env);
                services.AddScoped<IBlogViewService, BlogPostViewService>();
                services.AddScoped<ICommentService, CommentService>();
                services.AddScoped<IBlogPopulator, BlogPopulator>();
                services.AddSingleton<SearchQueryParser>();
                services.AddSingleton<IPopularityProvider, UmamiPopularityProvider>();
                services.AddSingleton<SearchRanker>(sp =>
                {
                    var popularityProvider = sp.GetService<IPopularityProvider>();
                    return new SearchRanker(popularityProvider: popularityProvider);
                });
                services.AddScoped<BlogSearchService>();
                services.AddScoped<IBlogSearchService>(provider => provider.GetRequiredService<BlogSearchService>());
                services.AddScoped<CommentViewService>();
                services.AddSingleton<BlogUpdater>();
                services.AddScoped<IBlogService, BlogService>();
                services.AddScoped<ISlugSuggestionService, SlugSuggestionService>();
                services.AddScoped<BlogValidationService>();

                // Register startup coordinator and background services
                services.AddSingleton<IStartupCoordinator>(sp =>
                {
                    var coordinator = new StartupCoordinator(sp.GetRequiredService<ILogger<StartupCoordinator>>());
                    // Pre-register all services that will participate
                    coordinator.RegisterService(StartupServiceNames.MarkdownDirectoryWatcher);
                    coordinator.RegisterService(StartupServiceNames.MarkdownReAddPosts);
                    coordinator.RegisterService(StartupServiceNames.BlogReconciliation);
                    coordinator.RegisterService(StartupServiceNames.MarkdownFetchPolling);
                    return coordinator;
                });

                services.AddHostedService<MarkdownDirectoryWatcherService>();
                services.AddHostedService<MarkdownReAddPostsService>();
                services.AddHostedService<BlogReconciliationService>();

                // Register markdown fetch polling service (only in database mode)
                services.AddHostedService<MarkdownFetchPollingService>();

                // Register semantic indexing background service if semantic search is enabled
                var semanticConfig = configuration.GetSection(SemanticSearchConfig.Section).Get<SemanticSearchConfig>();
                if (semanticConfig?.Enabled == true)
                {
                    services.AddHostedService<SemanticIndexingBackgroundService>();
                    // Smart semantic indexing - automatically reindex if Qdrant is empty (on deployment)
                    services.AddHostedService<SmartSemanticIndexService>();
                }
                break;
        }
        services.AddScoped<IMarkdownBlogService, MarkdownBlogPopulator>();

        services.AddScoped<MarkdownRenderingService>();
    }
    
    public static async Task PopulateBlog(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
    
        var config = scope.ServiceProvider.GetRequiredService<BlogConfig>();
        var cancellationToken = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        if (config.Mode == BlogMode.File)
        {
            var context = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
            await context.Populate(cancellationToken);
        }
     
    }
    
}