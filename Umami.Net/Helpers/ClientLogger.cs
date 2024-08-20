using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Umami.Net.Helpers;

public class HttpLogger : IHttpClientLogger
{
    private readonly ILogger<HttpLogger> _logger;

    public HttpLogger(ILogger<HttpLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogInformation(
            "Sending '{Method}' to '{Host}{Path}'",
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery);
        return null;
    }

    public void LogRequestStop(
        object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        _logger.LogInformation(
            "Received '{StatusCodeInt} {StatusCodeString}' after {ElapsedMilliseconds}ms",
            (int)response.StatusCode,
            response.StatusCode,
            elapsed.TotalMilliseconds.ToString("F1"));
    }

    public void LogRequestFailed(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage? response,
        Exception exception,
        TimeSpan elapsed)
    {
        _logger.LogError(
            exception,
            "Request towards '{Host}{Path}' failed after {ElapsedMilliseconds}ms",
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery,
            elapsed.TotalMilliseconds.ToString("F1"));
    }
}