using System.Reflection;
using Microsoft.AspNetCore.StaticFiles;
using Mostlylucid.Services;
using OpenTelemetry.Metrics;


using Prometheus;
using Umami.Net.Models;

try
{
    var builder = WebApplication.CreateBuilder(args);


    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
#if DEBUG
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
#endif
    });
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();

    var config = builder.Configuration;
    builder.Configuration.AddEnvironmentVariables();
    builder.Configure<AnalyticsSettings>();
    var auth = builder.Configure<AuthSettings>();
    var translateServiceConfig = builder.Configure<TranslateServiceConfig>();
    var services = builder.Services;
    
   services.AddOpenTelemetry()
        .WithMetrics(builder =>
        {
            builder.AddPrometheusExporter()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter();
        });

// Add services to
// the container.
    services.AddOutputCache();
    services.AddResponseCaching();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.SetupTranslateService();
    services.SetupOpenSearch(config);
    services.AddHealthChecks();
    services.SetupUmamiData(config);
    services.AddScoped<IUmamiDataSortService, UmamiDataSortService>();
    services.AddScoped<IUmamiUserInfoService, UmamiUserInfoService>();
    services.AddScoped<BaseControllerService>();
    services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
    services.SetupEmail(builder.Configuration);
    services.SetupRSS();
    services.SetupBlog(config, builder.Environment);
    services.SetupUmamiClient(config);
    services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.SuppressXFrameOptionsHeader = false;
    });
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
                    .WithOrigins("https://direct.mostlylucid.net")
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
    services.AddMvc();
    services.AddProgressiveWebApp(new PwaOptions
    {
        RegisterServiceWorker = true,
        RegisterWebmanifest = false,  // (Manually register in Layout file)
        Strategy = ServiceWorkerStrategy.NetworkFirst,
        OfflineRoute = "Offline.html"
    });
    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseHealthChecks("/healthz");
    app.MapPrometheusScrapingEndpoint();
    using (var scope = app.Services.CreateScope())
    {
        var blogContext = scope.ServiceProvider.GetRequiredService<IMostlylucidDBContext>();
        await blogContext.Database.MigrateAsync();
    }

//await app.SetupOpenSearchIndex();
// Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        //Hnadle unhandled exceptions 500 erros
        app.UseExceptionHandler("/error/500");
        //Handle 404 erros
    }
    else
    {
        app.UseDeveloperExceptionPage();
        app.UseHttpsRedirection();
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
        {
          var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
           await rss.Track("RSS",useDefaultUserAgent: true);
      
        }

        await next();
    });
    app.UseOutputCache();
    app.UseResponseCaching();
    app.UseImageSharp();

    var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();


    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append(
                "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
        }
    });
    app.UseStatusCodePagesWithReExecute("/error/{0}");

    app.UseRouting();
    app.UseCors("AllowMostlylucid");
    app.UseAuthentication();
    app.UseAuthorization();


    app.UseSwagger();
    app.UseSwaggerUI();

    await app.PopulateBlog();
    app.MapGet("/robots.txt", async httpContext =>
    {
        var robotsContent =
            $"User-agent: *\nDisallow: \nDisallow: /cgi-bin/\nSitemap: https://{httpContext.Request.Host}/sitemap.xml";
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
}

catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}