using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mostlylucid.DbContext.EntityFramework;
using Npgsql;

namespace Mostlylucid.DbContext;

public static class Setup
{
    public static void SetupDatabase(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment env, string applicationName="mostlylucid")
    {
        services.AddDbContext<IMostlylucidDBContext, MostlylucidDbContext>(options =>
        {
            if (env.IsDevelopment())
            {
                options.EnableDetailedErrors(true);
                options.EnableSensitiveDataLogging(true);
            }
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                ApplicationName = applicationName
            };
            options.UseNpgsql(connectionStringBuilder.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MostlylucidDbContext).Assembly.FullName); // Set your migration assembly here
            });
        });
    }
}