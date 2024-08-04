using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Services;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var markdownConfig =builder.Configure<MarkdownConfig>();
var auth = builder.Configure<Auth>();
var services = builder.Services;

services.AddCors(options =>
{
    options.AddPolicy("AllowMostlylucid",
        builder =>
        {
            builder.WithOrigins("https://www.mostlylucid.net")
                .WithOrigins("https://mostlylucid.net")
                .WithOrigins("https://localhost:7240")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
var env = builder.Environment;
builder.Services
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
services.AddControllersWithViews();
services.AddResponseCaching();
services.AddScoped<BlogService>();

    // services.AddScoped<MarkdownTranslatorService>();
    //
    // services.AddHttpClient<MarkdownTranslatorService>(options =>
    // {
    //     options.Timeout = TimeSpan.FromSeconds(120);
    //     //options.BaseAddress = new Uri("http://localhost:24080");
    // });
    // services.AddHostedService<BackgroundTranslateService>();


services.AddProgressiveWebApp();
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors("AllowMostlylucid");
app.UseHttpsRedirection();
app.UseImageSharp();
app.UseStaticFiles();


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.UseResponseCaching();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();