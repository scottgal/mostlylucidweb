using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Mostlylucid.EntityFramework;

public static class Setup
{
    public static void SetupEntityFramework(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MostlylucidDBContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static async Task InitializeDatabase(this WebApplication app)
    {
        try
        {
            await using var scope = 
                app.Services.CreateAsyncScope();
            await using var context = scope.ServiceProvider.GetRequiredService<MostlylucidDBContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Failed to migrate database");
        }        
    }
}