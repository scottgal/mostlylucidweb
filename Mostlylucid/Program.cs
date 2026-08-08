using System.Security.Cryptography.X509Certificates;
using Mostlylucid.Blog.WatcherService;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Middleware;
using Mostlylucid.Services;
using Mostlylucid.Services.Images;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using Serilog.Debugging;
using Mostlylucid.EmailSubscription;
using Mostlylucid.Services.Email;
using Mostlylucid.Services.Umami;
using Mostlylucid.Shared.Config;
using Mostlylucid.SemanticSearch.Config;
using Mostlylucid.SemanticSearch.Extensions;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Services.BrokenLinks;
using Mostlylucid.Services.Announcement;

try
{  Log.Logger = new LoggerConfiguration()
             .MinimumLevel.Warning()
             .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
             .MinimumLevel.Override("Npgsql", Serilog.Events.LogEventLevel.Error)
             .WriteTo.Console()
             .WriteTo.File("logs/boot-*.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
             .CreateBootstrapLogger();
    var builder = WebApplication.CreateBuilder(args);

    var config = builder.Configuration;
    if (builder.Environment.IsDevelopment())
    {
        config.AddUserSecrets<Program>();
    }
    config.AddEnvironmentVariables();

    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
#if DEBUG
        // Don't override MinimumLevel - let appsettings.json control it
        // SelfLog disabled to prevent Seq connection errors from appearing in console
        // SelfLog.Enable(Console.Error);
#endif
    });
    var certExists = File.Exists("mostlylucid.pfx");
    var certPassword = config["CertPassword"];
    var httpsEnabled = certExists && !string.IsNullOrEmpty(certPassword);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080); // HTTP endpoint

        // HTTPS endpoint only if cert exists and password is set
        if (httpsEnabled)
        {
            try
            {
                var certificate = new X509Certificate2("mostlylucid.pfx", certPassword);
                options.ListenAnyIP(7240, listenOptions =>
                {
                    listenOptions.UseHttps(o => o.ServerCertificate = certificate);
                });
            }
            catch
            {
                // Skip HTTPS if cert loading fails
            }
        }
    });

    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
    
    builder.Configure<AnalyticsSettings>();
    var auth = builder.Configure<AuthSettings>();
    var translateServiceConfig = builder.Configure<TranslateServiceConfig>();
    builder.Configure<Mostlylucid.Shared.Config.Markdown.ImageConfig>();
    var services = builder.Services;
    services.AddOpenTelemetry()
        .WithMetrics(builder =>
        {
     
            builder.AddAspNetCoreInstrumentation();
            builder.AddRuntimeInstrumentation();
            builder.AddHttpClientInstrumentation();
            builder.AddProcessInstrumentation();
            builder.AddPrometheusExporter();
        });

// Add services to
// the container.
    services.AddOutputCache(options =>
    {
        // Default policy for most endpoints - exclude HTMX requests from caching
        options.AddBasePolicy(builder => builder
            .Expire(TimeSpan.FromMinutes(5))
            .With(ctx => !ctx.HttpContext.Request.Headers.ContainsKey("HX-Request")));

        // Blog post policy - shorter duration with tag-based eviction
        options.AddPolicy("BlogPost", builder => builder
            .Expire(TimeSpan.FromMinutes(10))
            .Tag("blog")
            .With(ctx => !ctx.HttpContext.Request.Headers.ContainsKey("HX-Request")));

        // Blog list policy
        options.AddPolicy("BlogList", builder => builder
            .Expire(TimeSpan.FromMinutes(5))
            .Tag("blog")
            .SetVaryByQuery("page", "pageSize", "startDate", "endDate", "language", "orderBy", "orderDir")
            .With(ctx => !ctx.HttpContext.Request.Headers.ContainsKey("HX-Request")));

        // Category policy
        options.AddPolicy("BlogCategory", builder => builder
            .Expire(TimeSpan.FromMinutes(10))
            .Tag("blog")
            .SetVaryByQuery("category", "page", "pageSize")
            .With(ctx => !ctx.HttpContext.Request.Headers.ContainsKey("HX-Request")));
    });
    services.AddResponseCaching();
    services.AddOpenApi();
    services.SetupTranslateService();
    services.SetupOpenSearch(config);
    services.AddHealthChecks();
    services.SetupUmamiData(config);
    services.AddScoped<IUmamiDataSortService, UmamiDataSortService>();
    services.AddScoped<IUmamiUserInfoService, UmamiUserInfoService>();
    services.AddSingleton<IPopularPostsService, PopularPostsService>();
    services.AddScoped<BaseControllerService>();

    // External image download service
    services.AddScoped<ExternalImageDownloadService>();
    services.AddHostedService<ImageDownloadBackgroundService>();

    // Popular posts polling service
    services.AddHostedService<PopularPostsPollingService>();

    // Broken link detection and archive.org replacement service
    services.AddHttpClient("BrokenLinkChecker");
    services.AddScoped<IBrokenLinkService, BrokenLinkService>();
    services.AddHostedService<BrokenLinkCheckerBackgroundService>();

    // Announcement service
    builder.Configure<AnnouncementConfig>();
    services.AddScoped<IAnnouncementService, AnnouncementService>();

    services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
    services.SetupEmail(config);
    services.SetupRSS();
    services.SetupBlog(config, builder.Environment);
    services.SetupUmamiClient(config);
    services.AddSemanticSearch(config);
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    services.ConfigureEmailProcessor(config);
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
        RegisterWebmanifest = false, // (Manually register in Layout file)
        // Our own copy of NetworkFirst that ignores cross-origin requests. The stock strategy
        // intercepted them too, which broke every external image on the site (shields.io
        // badges, avatars). CustomStrategy is required for the file to be picked up at all -
        // any other Strategy value silently serves the embedded worker instead.
        Strategy = ServiceWorkerStrategy.CustomStrategy,
        CustomServiceWorkerStrategyFileName = "serviceworker-networkfirst-sameorigin.js",
        OfflineRoute = "Offline.html"
    });
    var app = builder.Build();

    // Configure Markdig FetchMarkdownExtension with service provider
    Mostlylucid.Markdig.FetchExtension.FetchMarkdownExtension.ConfigureServiceProvider(app.Services);

    app.UseResponseCompression();
    app.UseContentSecurityPolicy();
    // Must run before OutputCache so the Vary header is stored with the cached response.
    app.UseHtmxVary();
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
            await rss.Track("RSS", useDefaultUserAgent: true);
        }

        await next();
    });

    app.UseOutputCache();
    app.UseResponseCaching();
    app.UseImageSharp();

    var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();


    app.UseStaticFiles(new StaticFileOptions
    {
        ServeUnknownFileTypes = true, // This is necessary if serving uncommon file types
        DefaultContentType = "application/octet-stream", // Fallback for unknown types
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append(
                "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
            var fileExt = Path.GetExtension(ctx.File.Name);
            if (fileExt.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.ContentType = "application/pdf";
            }
            else if (fileExt.Equals(".docx", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
        }
    });

    app.UseStatusCodePagesWithReExecute("/error/{0}");

    app.UseRouting();
    app.UseCors("AllowMostlylucid");
    app.UseAuthentication();
    app.UseAuthorization();

    // Broken link archive middleware - AFTER OutputCache so processed pages get cached
    // On cache miss: BrokenLink processes response, then OutputCache caches the processed result
    // On cache hit: OutputCache serves already-processed content directly (BrokenLink doesn't run)
    app.UseBrokenLinkArchive();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    await app.PopulateBlog();

    // Initialize semantic search (if enabled and Qdrant is available)
    try
    {
        using var scope = app.Services.CreateScope();
        var semanticConfig = scope.ServiceProvider.GetRequiredService<SemanticSearchConfig>();
        Log.Information("Semantic search config loaded - Enabled: {Enabled}, Qdrant URL: {QdrantUrl}",
            semanticConfig.Enabled, semanticConfig.QdrantUrl);
        if (semanticConfig.Enabled)
        {
            var semanticSearch = scope.ServiceProvider.GetRequiredService<ISemanticSearchService>();
            await semanticSearch.InitializeAsync();
            Log.Information("Semantic search initialized");
        }
        else
        {
            Log.Information("Semantic search is disabled");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to initialize semantic search - continuing without it");
    }

    app.MapGet("/robots.txt", async httpContext =>
    {
        var robotsContent =
            $"User-agent: *\nDisallow: \nDisallow: /cgi-bin/\nSitemap: https://{httpContext.Request.Host}/sitemap.xml";
        httpContext.Response.ContentType = "text/plain";
        await httpContext.Response.WriteAsync(robotsContent);
    }).CacheOutput(policyBuilder =>
    {
        policyBuilder.Expire(TimeSpan.FromDays(60));
        policyBuilder.Cache();
    });


    app.MapControllerRoute(
        "sitemap",
        "sitemap.xml",
        new { controller = "Sitemap", action = "Index" });
    app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}

catch (Exception ex)
{
    if(args.Contains("migrate"))
    {
        Log.Information("Migration complete");
        return;
    }
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}