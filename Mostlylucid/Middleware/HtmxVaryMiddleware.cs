using Microsoft.Net.Http.Headers;

namespace Mostlylucid.Middleware;

/// <summary>
/// Adds <c>Vary: HX-Request</c> to HTML responses.
///
/// Controllers return either a full document or a layout-less partial for the same URL, depending
/// on the HX-Request header. Without Vary, any shared cache - Cloudflare, a proxy, the browser's
/// own HTTP cache, the service worker - is entitled to serve whichever it stored first to
/// everyone, so a normal visitor can receive a bare fragment with no layout.
/// </summary>
public class HtmxVaryMiddleware(RequestDelegate next)
{
    private const string HtmxHeader = "HX-Request";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            // Only HTML varies by HX-Request; static assets are identical either way.
            var contentType = context.Response.ContentType;
            if (contentType is not null &&
                contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                var vary = context.Response.Headers.Vary;
                if (!vary.Contains(HtmxHeader, StringComparer.OrdinalIgnoreCase))
                {
                    context.Response.Headers.Append(HeaderNames.Vary, HtmxHeader);
                }
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

public static class HtmxVaryMiddlewareExtensions
{
    public static IApplicationBuilder UseHtmxVary(this IApplicationBuilder builder) =>
        builder.UseMiddleware<HtmxVaryMiddleware>();
}
