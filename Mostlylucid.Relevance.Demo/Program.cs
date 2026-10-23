using System.Diagnostics;
using Mostlylucid.Relevance;
using Mostlylucid.Relevance.Demo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<DemoCatalog>();
// Both domain engines use this one hosted worker. Register it before seeding and log processing.
builder.Services.AddSingleton<RelevanceWorker>();
builder.Services.AddHostedService(p => p.GetRequiredService<RelevanceWorker>());
builder.Services.AddSingleton<DemoEngines>();
builder.Services.AddHostedService<DemoSeeder>();
builder.Services.AddSingleton<OperationsMonitor>();
builder.Services.AddHostedService(p => p.GetRequiredService<OperationsMonitor>());
builder.Services.AddSingleton<DemoPolicy>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/shop")) { await next(context); return; }
    var timer = Stopwatch.StartNew();
    var policy = context.RequestServices.GetRequiredService<DemoPolicy>();
    if (!policy.Acquire())
    {
        context.Response.StatusCode = 429;
        context.Response.Headers.RetryAfter = "60";
        await context.Response.WriteAsJsonAsync(new { error = "The demo shop rate limit was reached. Reset it in Operations." });
    }
    else await next(context);
    await context.RequestServices.GetRequiredService<OperationsMonitor>().PublishAsync(new DemoLog(DateTimeOffset.UtcNow,
        "http", $"{context.Request.Method} {context.Request.Path}", context.Response.StatusCode, timer.Elapsed.TotalMilliseconds,
        0, ThreadPool.PendingWorkItemCount), cancellationToken: context.RequestAborted);
});

// The handler sits inside the request monitor so handled HTTP 500 responses become measurements too.
app.UseExceptionHandler();

var api = app.MapGroup("/api");
api.AddEndpointFilter(async (context, next) =>
{
    try { return await next(context); }
    catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: 400); }
});
api.MapGet("/health", () => new { status = "ready" });
api.MapGet("/shop/products", (string? category, decimal? budget, DemoCatalog catalog) => catalog.Products
    .Where(p => (category is null or "all" || p.Category == category) && (!budget.HasValue || p.Price <= budget)));
api.MapPost("/shop/events", async (ShopEvent request, DemoCatalog catalog, DemoEngines engines, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Session) || request.Session.Length > 100) return Results.BadRequest(new { error = "Session is required." });
    var product = catalog.Get(request.Product);
    if (request.Type is not ("view" or "compare" or "cart" or "remove" or "purchase" or "cancel")) return Results.BadRequest(new { error = "Unknown interaction." });
    if (!product.Available && request.Type is "cart" or "purchase") return Results.BadRequest(new { error = "Product is unavailable." });
    var receipt = await engines.Shop.SaveAsync("events", new { id = request.Id ?? Guid.NewGuid().ToString(), subject = request.Session, candidate = product.Id, type = request.Type }, ct);
    if (request.Type == "purchase" && request.Recommendation is { } recommendation)
        await engines.Shop.SaveAsync("outcomes", new { id = "purchase:" + receipt.Id, recommendation, candidate = product.Id, reward = 1.0 }, ct);
    return Results.Ok(new { receipt, prediction = await engines.Shop.PredictAsync(request.Session, ct) });
});
api.MapGet("/shop/recommendations", async (string session, string? category, double? budget, DemoEngines engines, CancellationToken ct) =>
{
    var placement = category switch { "outdoors" => "outdoors", "electronics" => "electronics", "home" => "homewares", null or "all" => "home", _ => throw new ArgumentException("Unknown category.") };
    return await engines.Shop.RecommendAsync(session, placement, budget.HasValue ? Filter.Lte("price", budget.Value) : null, 4, ct);
});
api.MapGet("/operations", (OperationsMonitor monitor, DemoPolicy policy) => new { telemetry = monitor.Snapshot, policy = policy.View });
api.MapPost("/operations/scenarios/{name}", async (string name, OperationsMonitor monitor, CancellationToken ct) => { await monitor.InjectAsync(name, ct); return Results.Ok(new { state = monitor.Snapshot.State }); });
api.MapPost("/operations/actions/{name}", async (string name, DemoPolicy policy, CancellationToken ct) => Results.Ok(await policy.ApplyAsync(name, ct)));
api.MapPost("/operations/reset", (DemoPolicy policy) => { policy.Reset(); return Results.Ok(policy.View); });
app.Run();

/// <summary>A shopping interaction; provide a stable Id for retries and a returned Recommendation ID for attribution.</summary>
/// <remarks>Session identifies this visit; Type is a configured event, not a free-form behavioural label.</remarks>
public sealed record ShopEvent(string Session, string Product, string Type, string? Recommendation = null, string? Id = null);
/// <summary>Entry point exposed to WebApplicationFactory for real HTTP integration tests.</summary>
public partial class Program;
