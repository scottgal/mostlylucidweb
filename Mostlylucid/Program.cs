using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Email;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.RSS;
using Mostlylucid.Services;
using Mostlylucid.Services.Markdown;
using Serilog;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var config = builder.Configuration;
builder.Configuration.AddEnvironmentVariables();
builder.Configure<MarkdownConfig>();
builder.Configure<AnalyticsSettings>();

var auth = builder.Configure<AuthSettings>();
var translateServiceConfig = builder.Configure<TranslateServiceConfig>();
var services = builder.Services;

// Add services to the container.
services.AddOutputCache(); // Remove duplicate call later in your code
services.AddControllersWithViews();
services.AddResponseCaching();
services.AddScoped<IBlogService, MarkdownBlogService>();
services.AddScoped<CommentService>();
if (translateServiceConfig.Enabled) services.SetupTranslateService();
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
services.SetupEmail(builder.Configuration);
services.SetupRSS();

// Setup CORS for Google Auth Use.
services.AddCors(options =>
{
    options.AddPolicy("AllowMostlylucid",
        builder =>
        {
            builder.WithOrigins("https://www.mostlylucid.net")
                .WithOrigins("https://mostlylucid.net")
                .WithOrigins("https://localhost:7240")
                .WithOrigins("https://local.mostlylucid.net")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Setup Authentication Options
services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = auth.GoogleClientId;
        options.ClientSecret = auth.GoogleClientSecret;
    });

var app = builder.Build();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseResponseCaching();
app.UseImageSharp();

await using var scope = app.Services.CreateAsyncScope();
var context = scope.ServiceProvider.GetRequiredService<IBlogService>();
await context.Populate();

app.MapGet("/robots.txt", async httpContext =>
{
    var robotsContent = $"User-agent: *\nDisallow: \nDisallow: /cgi-bin/\nSitemap: https://{httpContext.Request.Host}/sitemap.xml";
    httpContext.Response.ContentType = "text/plain";
    await httpContext.Response.WriteAsync(robotsContent);
}).CacheOutput(policy: policyBuilder =>
{
    policyBuilder.Expire(TimeSpan.FromDays(60));
    policyBuilder.Cache();
});

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });
app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();