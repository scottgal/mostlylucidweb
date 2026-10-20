using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umami.Net.UmamiData.Helpers;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string Query(BaseRequest request)
    {
        request.Validate();
        return request.ToQueryString(analyticsSettings.ApiVersion);
    }

    private string WebsitePath(string resource) => $"{(analyticsSettings.UseCloudApi ? "" : "api/")}websites/{Uri.EscapeDataString(WebsiteId)}/{resource}";

    private async Task<UmamiResult<T>> RequestAsync<T>(HttpMethod method, string path, object? body = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await authService.SendAsync(() => new HttpRequestMessage(method, path)
            {
                Content = body == null ? null : JsonContent.Create(body, options: JsonOptions)
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Umami data request failed with {StatusCode}", response.StatusCode);
                return new(response.StatusCode, response.ReasonPhrase ?? "Request failed", default);
            }
            if (response.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(bool))
                return new(response.StatusCode, "Success", typeof(T) == typeof(bool) ? (T)(object)true : default);
            var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return new(response.StatusCode, "Success", content);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new(HttpStatusCode.GatewayTimeout, "Umami request timed out", default); }
        catch (HttpRequestException) { return new(HttpStatusCode.ServiceUnavailable, "Cannot connect to Umami", default); }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        { return new(HttpStatusCode.BadGateway, "Invalid response from Umami", default); }
    }
}
