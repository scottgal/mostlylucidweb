using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.UmamiData.Helpers;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService
{
    /// <summary>
    /// Gets aggregated metrics for the website from Umami.
    /// </summary>
    /// <param name="metricsRequest">The metrics request specifying the type, date range, and optional filters.</param>
    /// <returns>
    /// An <see cref="UmamiResult{T}"/> containing an array of metric response models.
    /// Each model contains the metric name (x) and count (y).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when metricsRequest is null.
    /// Suggestion: Create a MetricsRequest object with required properties.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when required parameters are missing or invalid.
    /// Suggestion: Check that all required properties are set (StartAtDate, EndAtDate, Type, Unit).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method retrieves aggregated counts for a specific metric type (URLs, countries, browsers, etc.).
    /// Use this for getting top items by count (e.g., most viewed pages, top countries).
    /// </para>
    /// <para>
    /// Common error codes:
    /// <list type="bullet">
    /// <item><description>400 Bad Request - Missing required parameter (check Type and Unit are set)</description></item>
    /// <item><description>401 Unauthorized - Authentication failed (check username/password in configuration)</description></item>
    /// <item><description>404 Not Found - Website ID not found (verify WebsiteId in configuration)</description></item>
    /// <item><description>500 Internal Server Error - Server error (check Umami instance health)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// var request = new MetricsRequest
    /// {
    ///     StartAtDate = DateTime.UtcNow.AddDays(-7),
    ///     EndAtDate = DateTime.UtcNow,
    ///     Type = MetricType.url,
    ///     Unit = Unit.day,
    ///     Limit = 10
    /// };
    /// var result = await dataService.GetMetrics(request);
    /// if (result.Status == HttpStatusCode.OK)
    /// {
    ///     foreach (var metric in result.Data)
    ///     {
    ///         Console.WriteLine($"{metric.x}: {metric.y} views");
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
    {
        if (metricsRequest == null)
        {
            throw new ArgumentNullException(nameof(metricsRequest),
                "MetricsRequest cannot be null. " +
                "Suggestion: Create a new MetricsRequest object with StartAtDate, EndAtDate, Type, and Unit properties set.");
        }

        try
        {
            // Validate request before making API call
            metricsRequest.Validate();

            // Authenticate
            if (await authService.Login() == false)
            {
                return new UmamiResult<MetricsResponseModels[]>(
                    HttpStatusCode.Unauthorized,
                    "Failed to authenticate with Umami. " +
                    "Suggestion: Verify your username and password in the Analytics configuration section.",
                    null);
            }

            // Build query string - try v2 API first (path/hostname), fallback to v1 (url/host) if needed
            var queryString = metricsRequest.ToQueryString();
            var fullUrl = $"/api/websites/{WebsiteId}/metrics{queryString}";

            logger.LogInformation("Making metrics request to: {Url}", fullUrl);
            logger.LogInformation("Request details - Type: {Type}, Unit: {Unit}, StartAt: {StartAt}, EndAt: {EndAt}, Limit: {Limit}",
                metricsRequest.Type, metricsRequest.Unit, metricsRequest.StartAt, metricsRequest.EndAt, metricsRequest.Limit);

            // Make the HTTP request
            var response = await authService.HttpClient.GetAsync(fullUrl);

            // If we get 400 Bad Request, try with v1 parameter names
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogWarning("Got 400 Bad Request with v2 parameters, retrying with v1 parameters (url/host)");
                queryString = ConvertToV1Parameters(queryString);
                fullUrl = $"/api/websites/{WebsiteId}/metrics{queryString}";
                logger.LogInformation("Retrying with v1 URL: {Url}", fullUrl);
                response = await authService.HttpClient.GetAsync(fullUrl);
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<MetricsResponseModels[]>();
                return new UmamiResult<MetricsResponseModels[]>(
                    response.StatusCode,
                    response.ReasonPhrase ?? "Success",
                    content ?? Array.Empty<MetricsResponseModels>());
            }

            // Handle unauthorized - retry once after re-authentication
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Received 401 Unauthorized, attempting to re-authenticate");
                await authService.Login();
                return await GetMetrics(metricsRequest);
            }

            // Log detailed error information
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = BuildErrorMessage(response.StatusCode, response.ReasonPhrase, errorContent);

            logger.LogError("Failed to get metrics: {StatusCode} - {ReasonPhrase}. Response: {ErrorContent}. Request URL: {RequestUrl}",
                response.StatusCode, response.ReasonPhrase, errorContent, response.RequestMessage?.RequestUri?.ToString());

            return new UmamiResult<MetricsResponseModels[]>(response.StatusCode, errorMessage, null);
        }
        catch (ArgumentException ex)
        {
            // Validation errors - already have helpful messages
            logger.LogError(ex, "Validation error in GetMetrics");
            return new UmamiResult<MetricsResponseModels[]>(HttpStatusCode.BadRequest, ex.Message, null);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error in GetMetrics");
            return new UmamiResult<MetricsResponseModels[]>(
                HttpStatusCode.ServiceUnavailable,
                $"Failed to connect to Umami server. {ex.Message} " +
                "Suggestion: Verify the UmamiPath in your configuration and ensure the Umami instance is running.",
                null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in GetMetrics");
            return new UmamiResult<MetricsResponseModels[]>(
                HttpStatusCode.InternalServerError,
                $"An unexpected error occurred: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Builds a helpful error message based on the HTTP status code.
    /// </summary>
    private static string BuildErrorMessage(HttpStatusCode statusCode, string? reasonPhrase, string errorContent)
    {
        var baseMessage = reasonPhrase ?? "Request failed";

        var suggestion = statusCode switch
        {
            HttpStatusCode.BadRequest =>
                "Suggestion: Check that all required parameters are set correctly. " +
                "Ensure Type and Unit are specified, and dates are in the correct range.",

            HttpStatusCode.Unauthorized =>
                "Suggestion: Verify your Umami username and password in the Analytics configuration.",

            HttpStatusCode.Forbidden =>
                "Suggestion: Ensure your Umami user account has permission to access this website's data.",

            HttpStatusCode.NotFound =>
                "Suggestion: Verify the WebsiteId in your configuration matches an existing website in Umami.",

            HttpStatusCode.TooManyRequests =>
                "Suggestion: You're making too many requests. Implement caching or reduce request frequency.",

            HttpStatusCode.InternalServerError =>
                "Suggestion: Check the Umami server logs for errors. The server may be experiencing issues.",

            _ => "Suggestion: Check the Umami server status and your request parameters."
        };

        var fullMessage = $"{baseMessage}. {suggestion}";

        if (!string.IsNullOrWhiteSpace(errorContent) && errorContent.Length < 500)
        {
            fullMessage += $" Server response: {errorContent}";
        }

        return fullMessage;
    }

    /// <summary>
    /// Gets expanded metrics with detailed engagement data for the website from Umami.
    /// </summary>
    /// <param name="metricsRequest">The metrics request specifying the type, date range, and optional filters.</param>
    /// <returns>
    /// An <see cref="UmamiResult{T}"/> containing an array of expanded metric response models.
    /// Each model includes: name, pageviews, visitors, visits, bounces, and totaltime.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when metricsRequest is null.
    /// Suggestion: Create a MetricsRequest object with required properties.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Expanded metrics provide detailed engagement data beyond simple counts:
    /// <list type="bullet">
    /// <item><description>name - The dimension value (e.g., URL, country name)</description></item>
    /// <item><description>pageviews - Total number of page hits</description></item>
    /// <item><description>visitors - Number of unique visitors</description></item>
    /// <item><description>visits - Number of unique visit sessions</description></item>
    /// <item><description>bounces - Number of single-page visits</description></item>
    /// <item><description>totaltime - Total time spent in milliseconds</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use this method when you need engagement metrics like bounce rate or average time on page.
    /// For simple counts, use <see cref="GetMetrics"/> instead.
    /// </para>
    /// </remarks>
    public async Task<UmamiResult<ExpandedMetricsResponseModel[]>> GetExpandedMetrics(MetricsRequest metricsRequest)
    {
        if (metricsRequest == null)
        {
            throw new ArgumentNullException(nameof(metricsRequest),
                "MetricsRequest cannot be null. " +
                "Suggestion: Create a new MetricsRequest object with StartAtDate, EndAtDate, Type, and Unit properties set.");
        }

        try
        {
            // Validate request before making API call
            metricsRequest.Validate();

            // Authenticate
            if (await authService.Login() == false)
            {
                return new UmamiResult<ExpandedMetricsResponseModel[]>(
                    HttpStatusCode.Unauthorized,
                    "Failed to authenticate with Umami. " +
                    "Suggestion: Verify your username and password in the Analytics configuration section.",
                    null);
            }

            // Build query string
            var queryString = metricsRequest.ToQueryString();

            // Make the HTTP request
            var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/metrics/expanded{queryString}");

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully retrieved expanded metrics");
                var content = await response.Content.ReadFromJsonAsync<ExpandedMetricsResponseModel[]>();
                return new UmamiResult<ExpandedMetricsResponseModel[]>(
                    response.StatusCode,
                    response.ReasonPhrase ?? "Success",
                    content ?? Array.Empty<ExpandedMetricsResponseModel>());
            }

            // Handle unauthorized - retry once after re-authentication
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Received 401 Unauthorized, attempting to re-authenticate");
                await authService.Login();
                return await GetExpandedMetrics(metricsRequest);
            }

            // Log detailed error information
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = BuildErrorMessage(response.StatusCode, response.ReasonPhrase, errorContent);

            logger.LogError("Failed to get expanded metrics: {StatusCode} - {ReasonPhrase}. Response: {ErrorContent}. Request URL: {RequestUrl}",
                response.StatusCode, response.ReasonPhrase, errorContent, response.RequestMessage?.RequestUri?.ToString());

            return new UmamiResult<ExpandedMetricsResponseModel[]>(response.StatusCode, errorMessage, null);
        }
        catch (ArgumentException ex)
        {
            // Validation errors - already have helpful messages
            logger.LogError(ex, "Validation error in GetExpandedMetrics");
            return new UmamiResult<ExpandedMetricsResponseModel[]>(HttpStatusCode.BadRequest, ex.Message, null);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error in GetExpandedMetrics");
            return new UmamiResult<ExpandedMetricsResponseModel[]>(
                HttpStatusCode.ServiceUnavailable,
                $"Failed to connect to Umami server. {ex.Message} " +
                "Suggestion: Verify the UmamiPath in your configuration and ensure the Umami instance is running.",
                null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in GetExpandedMetrics");
            return new UmamiResult<ExpandedMetricsResponseModel[]>(
                HttpStatusCode.InternalServerError,
                $"An unexpected error occurred: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Converts v2 API parameter names to v1 API parameter names.
    /// v2 uses 'path' and 'hostname', v1 uses 'url' and 'host'.
    /// </summary>
    private static string ConvertToV1Parameters(string queryString)
    {
        return queryString
            .Replace("path=", "url=")
            .Replace("hostname=", "host=");
    }
}