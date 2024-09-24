using Mostlylucid.Blog.Markdown;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Blog.WatcherService;
using Mostlylucid.DbContext;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Config.Markdown;
using Npgsql;

namespace Mostlylucid.Blog;

public static class BlogSetup
{
    public static void SetupBlog(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var config = services.ConfigurePOCO<BlogConfig>(configuration.GetSection(BlogConfig.Section));
       services.ConfigurePOCO<MarkdownConfig>(configuration.GetSection(MarkdownConfig.Section));
        switch (config.Mode)
        {
            case BlogMode.File:
                Log.Information("Using file based blog");
                services.AddScoped<IBlogViewService, MarkdownBlogViewService>();
                services.AddScoped<IBlogPopulator, MarkdownBlogPopulator>();
                break;
            case BlogMode.Database:
                Log.Information("Using Database based blog");
           services.SetupDatabase(configuration, env);
                services.AddScoped<IBlogViewService, BlogPostViewService>();
                services.AddScoped<ICommentService, EFCommentService>();
                services.AddScoped<IBlogPopulator, EFBlogPopulator>();
                services.AddScoped<BlogSearchService>();
                services.AddScoped<CommentViewService>();
                services.AddSingleton<EFBlogUpdater>();
                services.AddScoped<IBlogService, BlogService>();
                services.AddHostedService<MarkdownDirectoryWatcherService>();
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