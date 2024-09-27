using Hangfire;
using Hangfire.PostgreSql;
using Mostlylucid.DbContext;
using Mostlylucid.SchedulerService.API;
using Mostlylucid.SchedulerService.Services;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Email;
using Mostlylucid.Services.EmailSubscription;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Config;
using Serilog;
using Serilog.Debugging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Seq(Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341")
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
var env = builder.Environment;
var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddHealthChecks();
services.AddScoped<NewsletterManagementService>();
services.AddScoped<NewsletterSendingService>();

services.ConfigurePOCO<NewsletterConfig>(config);
services.AddScoped<IBlogService, BlogService>();
services.SetupEmail(configuration);
services.AddScoped<MarkdownRenderingService>();
var connectionString = configuration.GetConnectionString("DefaultConnection");
services.AddHangfire(x =>
    x.UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(connectionString);
    }));
services.AddHangfireServer();

services.AddSingleton<RecurringJobManager>();
services.SetupDatabase(configuration, env,"mostlylucid-scheduler");

var app = builder.Build();
app.UseHealthChecks("/healthz");
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

app.UseSwagger();
app.UseSwaggerUI();

var root = app.MapGroup("api").MapTodosApi().WithTags("email");

app.Run();