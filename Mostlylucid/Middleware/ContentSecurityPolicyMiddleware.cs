namespace Mostlylucid.Middleware;

/// <summary>
/// Middleware to add Content Security Policy headers
/// </summary>
public class ContentSecurityPolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ContentSecurityPolicyMiddleware> _logger;
    private readonly string _cspHeader;

    public ContentSecurityPolicyMiddleware(
        RequestDelegate next,
        ILogger<ContentSecurityPolicyMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // Build CSP from configuration or use defaults
        var umamiPath = configuration["Analytics:UmamiPath"] ?? "";
        var umamiHost = !string.IsNullOrEmpty(umamiPath) ? new Uri(umamiPath).Host : "";

        _cspHeader = BuildCspHeader(umamiHost);
    }

    private static string BuildCspHeader(string umamiHost)
    {
        var directives = new List<string>
        {
            // Default - fallback for unspecified directives
            "default-src 'self'",

            // Scripts - self, Google Sign-In, Umami analytics
            // 'unsafe-inline' needed for Alpine.js x-data/x-init expressions
            // 'unsafe-eval' needed for some Alpine.js features
            $"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://accounts.google.com {(string.IsNullOrEmpty(umamiHost) ? "" : $"https://{umamiHost}")}".Trim(),

            // Styles - self, Google, unsafe-inline for Tailwind
            "style-src 'self' 'unsafe-inline' https://accounts.google.com",

            // Images - self, data URIs, HTTPS images
            "img-src 'self' data: https: blob:",

            // Fonts - self only; boxicons is served from /fonts
            "font-src 'self' data:",

            // Connect (fetch/XHR) - self, Google, Umami, Hugging Face (for model downloads)
            $"connect-src 'self' https://accounts.google.com https://huggingface.co {(string.IsNullOrEmpty(umamiHost) ? "" : $"https://{umamiHost}")}".Trim(),

            // Frames - Google Sign-In uses iframes
            "frame-src 'self' https://accounts.google.com",

            // Frame ancestors - prevent clickjacking
            "frame-ancestors 'self'",

            // Form actions
            "form-action 'self' https://accounts.google.com",

            // Base URI
            "base-uri 'self'",

            // Object/embed - block plugins
            "object-src 'none'",

            // Upgrade insecure requests in production
            "upgrade-insecure-requests"
        };

        return string.Join("; ", directives);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Don't add CSP to API responses or non-HTML content
        context.Response.OnStarting(() =>
        {
            // Only add CSP header if not already present
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.Response.Headers.Append("Content-Security-Policy", _cspHeader);
            }

            // Also add X-Content-Type-Options to prevent MIME sniffing
            if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // X-Frame-Options as backup for older browsers
            if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            }

            // Referrer policy
            if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension methods for CSP middleware
/// </summary>
public static class ContentSecurityPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ContentSecurityPolicyMiddleware>();
    }
}
