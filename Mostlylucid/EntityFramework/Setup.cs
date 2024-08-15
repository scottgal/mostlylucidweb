using Microsoft.EntityFrameworkCore;
using Mostlylucid.Blog;
using Serilog;

namespace Mostlylucid.EntityFramework;

public static class Setup
{
    public static void SetupEntityFramework(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MostlylucidDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        try
        {
            await using var scope = 
                app.Services.CreateAsyncScope();
            
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDbContext>();
            await context.Database.MigrateAsync();
            
            var markdownBlogPopulator = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
            await markdownBlogPopulator.Populate();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to migrate database");
        }        
    }
}