using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Email;
using Mostlylucid.EntityFramework;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.RSS;
using Mostlylucid.Services;
using Mostlylucid.Services.EntityFramework;
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

services.AddOutputCache();


//services.SetupEntityFramework(config.GetConnectionString("DefaultConnection") ??
                            //  throw new Exception("No Connection String"));

//Set up CORS for Google Auth Use.
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

//Set up our Authentication Options
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
// Add services to the container.
services.AddOutputCache();
services.AddControllersWithViews();
services.AddResponseCaching();
services.AddScoped<IBlogService, MarkdownBlogService>();
    //services.AddSingleton<IMarkdownBlogService, MarkdownBlogService>();
services.AddScoped<CommentService>();

if (translateServiceConfig.Enabled) services.SetupTranslateService();
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
services.SetupEmail(builder.Configuration);
services.SetupRSS();
var app = builder.Build();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseOutputCache();


//await app.InitializeDatabase();

await using var scope = app.Services.CreateAsyncScope();
var context = scope.ServiceProvider.GetRequiredService<IBlogService>();
await context.Populate();

app.UseCors("AllowMostlylucid");
app.UseHttpsRedirection();
app.UseImageSharp();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl =
        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5)
        };
    context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
        new string[] { "Accept-Encoding" };

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.UseResponseCaching();
app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();