using System.Net;
using System.Security.Claims;

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
        services.Configure<LucidResumeProxyOptions>(configuration.GetSection("LucidResumeProxy"));
        services.AddHttpClient(ClientName, client => client.Timeout = TimeSpan.FromMinutes(15))
            .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        return services;
    }

    public static IEndpointRouteBuilder MapLucidResumeProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/resume", ["GET", "HEAD", "POST"], ForwardAsync);
        endpoints.MapMethods("/resume/{**path}", ["GET", "HEAD", "POST"], ForwardAsync);
        return endpoints;
    }

    private static async Task ForwardAsync(HttpContext context, IHttpClientFactory clients,
        Microsoft.Extensions.Options.IOptions<LucidResumeProxyOptions> configured)
    {
        var options = configured.Value;
        if (!Uri.TryCreate(options.UpstreamUri, UriKind.Absolute, out var upstream) ||
            upstream.Scheme is not ("http" or "https") || upstream.AbsolutePath != "/")
        {
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
        if (HttpMethods.IsPost(context.Request.Method) && !loopbackWriter &&
            (string.IsNullOrWhiteSpace(options.WriterEmail) ||
             !string.Equals(context.User.FindFirstValue(ClaimTypes.Email), options.WriterEmail,
                 StringComparison.OrdinalIgnoreCase)))
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
}
