using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.SchedulerService.Services;
using Npgsql;
using Serilog;
using Serilog.Debugging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);
    
var config = builder.Configuration;
config.AddEnvironmentVariables();
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
#if DEBUG
    configuration.MinimumLevel.Debug();
    configuration.WriteTo.Console();
    SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel}");
#endif
});
var configuration = builder.Configuration;
var env= builder.Environment;
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<NewsletterManagementService>();
builder.Services.AddScoped<NewsletterSendingService>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(x =>
    x.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<RecurringJobManager>();
builder.Services.AddDbContext<IMostlylucidDBContext, MostlylucidDbContext>(options =>
{
    if (env.IsDevelopment())
    {
        options.EnableDetailedErrors(true);
        options.EnableSensitiveDataLogging(true);
    }
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        ApplicationName = "mostlylucid"
    };
    options.UseNpgsql(connectionStringBuilder.ConnectionString);
});
var app = builder.Build();

app.InitializeJobs();
app.UseHangfireDashboard("/dashboard");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();