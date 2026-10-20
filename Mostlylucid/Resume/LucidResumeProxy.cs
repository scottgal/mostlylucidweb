using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.OutputCaching;

namespace Mostlylucid.Resume;

/// <summary>Same-origin gateway for the independently hosted lucidRESUME compiler.</summary>
public static class LucidResumeProxy
{
    private const string ClientName = "LucidResumeCompiler";
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailer", "Transfer-Encoding", "Upgrade", "Host"
    };

    public static IServiceCollection AddLucidResumeProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LucidResumeProxyOptions>().Bind(configuration.GetSection("LucidResumeProxy"))
            .PostConfigure(options =>
            {
                options.AdminUserGoogleId = configuration["Auth:AdminUserGoogleId"];
                if (string.IsNullOrWhiteSpace(options.UpstreamUri) && File.Exists(EmbeddedResumeCompiler.ExecutablePath))
                {
                    options.UpstreamUri = EmbeddedResumeCompiler.UpstreamUri;
                    options.UseEmbeddedCompiler = true;
                }
            });
        services.AddHostedService<EmbeddedResumeCompiler>();
        services.AddHttpClient(ClientName, client => client.Timeout = TimeSpan.FromMinutes(15))
            .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        return services;
    }

    public static IEndpointRouteBuilder MapLucidResumeProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/resume", ["GET", "HEAD", "POST"], ForwardAsync).CacheOutput(policy => policy.NoCache());
        endpoints.MapMethods("/resume/{**path}", ["GET", "HEAD", "POST"], ForwardAsync).CacheOutput(policy => policy.NoCache());
        return endpoints;
    }

    private static async Task ForwardAsync(HttpContext context, IHttpClientFactory clients,
        Microsoft.Extensions.Options.IOptions<LucidResumeProxyOptions> configured)
    {
        // API responses must retain their status/body instead of re-executing the MVC error page.
        if (context.Features.Get<IStatusCodePagesFeature>() is { } statusPages)
            statusPages.Enabled = false;
        context.Response.Headers.CacheControl = "no-store";
        var options = configured.Value;
        if (!Uri.TryCreate(options.UpstreamUri, UriKind.Absolute, out var upstream) ||
            upstream.Scheme is not ("http" or "https") || upstream.AbsolutePath != "/")
        {
            if (HttpMethods.IsGet(context.Request.Method) &&
                context.Request.Path.Value?.TrimEnd('/') == "/resume")
            {
                context.Response.Redirect("/resumes");
                return;
            }
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Résumé compiler is not configured.");
            return;
        }

        var host = context.Request.Host.Host;
        var loopbackHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                           IPAddress.TryParse(host, out var hostAddress) && IPAddress.IsLoopback(hostAddress);
        var loopbackWriter = options.AllowLoopbackWriter && upstream.IsLoopback && loopbackHost &&
                                     context.Connection.RemoteIpAddress is { } remote &&
                                     IPAddress.IsLoopback(remote);
        // Visitors may compile from the published record; publishing/changing that record is writer-only.
        var isCompilation = context.Request.Path.Value?.TrimEnd('/') == "/resume/api/compile";
        var writer = context.User.Identity?.IsAuthenticated == true &&
            (!string.IsNullOrWhiteSpace(options.WriterEmail)
                ? string.Equals(context.User.FindFirstValue(ClaimTypes.Email), options.WriterEmail, StringComparison.OrdinalIgnoreCase)
                : !string.IsNullOrWhiteSpace(options.AdminUserGoogleId) &&
                  (context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)) == options.AdminUserGoogleId);
        if (HttpMethods.IsPost(context.Request.Method) && !isCompilation && !loopbackWriter &&
            !writer)
        {
            context.Response.StatusCode = context.User.Identity?.IsAuthenticated == true
                ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
            return;
        }

        var target = new Uri(upstream, context.Request.Path + context.Request.QueryString);
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
        if (HttpMethods.IsPost(context.Request.Method))
            request.Content = new StreamContent(context.Request.Body);
        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) || header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null)
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        // The compiler's antiforgery cookie is distinct from the blog's login cookie.
        if (context.Request.Cookies.TryGetValue("lucidresume.csrf", out var antiforgeryCookie))
            request.Headers.TryAddWithoutValidation("Cookie", $"lucidresume.csrf={antiforgeryCookie}");

        try
        {
            using var upstreamResponse = await clients.CreateClient(ClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
            foreach (var header in upstreamResponse.Headers)
                if (!HopByHopHeaders.Contains(header.Key))
                    context.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in upstreamResponse.Content.Headers)
                if (!HopByHopHeaders.Contains(header.Key))
                    context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.CacheControl = "no-store";
            if (!HttpMethods.IsHead(context.Request.Method))
                await upstreamResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (HttpRequestException)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Résumé compiler is unavailable.");
            }
        }
    }
}

public sealed class LucidResumeProxyOptions
{
    public string? UpstreamUri { get; set; }
    public string? WriterEmail { get; set; }
    public bool AllowLoopbackWriter { get; set; }
    public string? PublicBaseUri { get; set; }
    internal string? AdminUserGoogleId { get; set; }
    internal bool UseEmbeddedCompiler { get; set; }
}
