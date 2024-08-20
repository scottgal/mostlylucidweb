﻿using Microsoft.EntityFrameworkCore;
using Mostlylucid.Blog.EntityFramework;
using Mostlylucid.Blog.Markdown;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.EntityFramework;
using Serilog;

namespace Mostlylucid.Blog;

public static class BlogSetup
{
    public static void SetupBlog(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var config = services.ConfigurePOCO<BlogConfig>(configuration.GetSection(BlogConfig.Section));
       services.ConfigurePOCO<MarkdownConfig>(configuration.GetSection(MarkdownConfig.Section));
       services.AddScoped<CommentService>();
        switch (config.Mode)
        {
            case BlogMode.File:
                Log.Information("Using file based blog");
                services.AddScoped<IBlogService, MarkdownBlogService>();
                services.AddScoped<IBlogPopulator, MarkdownBlogPopulator>();
                break;
            case BlogMode.Database:
                Log.Information("Using Database based blog");
                services.AddDbContext<MostlylucidDbContext>(options =>
                {
                    if (env.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging(true);
                    }
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                });
                services.AddScoped<IBlogService, EFBlogService>();
            
                services.AddScoped<IBlogPopulator, EFBlogPopulator>();
                services.AddHostedService<BackgroundEFBlogUpdater>();
                break;
        }
        services.AddScoped<IMarkdownBlogService, MarkdownBlogPopulator>();

        services.AddScoped<MarkdownRenderingService>();
    }
    
    public static async Task PopulateBlog(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
    
        var config = scope.ServiceProvider.GetRequiredService<BlogConfig>();
        if(config.Mode == BlogMode.Database)
        {
        
           var blogContext = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
           Log.Information("Migrating database");
           await blogContext.Database.MigrateAsync();
        }

        if (config.Mode == BlogMode.File)
        {
            var context = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
            await context.Populate();
        }
     
    }
}