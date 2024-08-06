using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Email;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Services;
using Mostlylucid.Services.Markdown;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var markdownConfig =builder.Configure<MarkdownConfig>();
var auth = builder.Configure<AuthSettings>();
var translateServiceConfig = builder.Configure<TranslateServiceConfig>();
var services = builder.Services;
var env = builder.Environment;
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
services.AddScoped<CommentService>();

if (translateServiceConfig.Enabled)
{
services.SetupTranslateService();
}
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
services.SetupEmail(builder.Configuration);

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

//T0 test email service
// await using  var scope = app.Services.CreateAsyncScope();
// var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
// await emailService.SendCommentEmail("test@test.com", "Test", "Test", "test");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();



app.UseResponseCaching();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();