using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Email;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Services.Markdown;
using Serilog;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Configuration.AddEnvironmentVariables();
builder.Configure<MarkdownConfig>();
var auth = builder.Configure<AuthSettings>();
var translateServiceConfig = builder.Configure<TranslateServiceConfig>();
var services = builder.Services;

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

if (translateServiceConfig.Enabled) services.SetupTranslateService();
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
services.SetupEmail(builder.Configuration);

var app = builder.Build();
app.UseSerilogRequestLogging();

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
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.Run();