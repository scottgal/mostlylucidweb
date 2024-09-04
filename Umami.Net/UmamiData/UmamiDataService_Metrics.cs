using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService
{
    public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
    {
        try
        {
            if (await authService.LoginAsync() == false)
                return new UmamiResult<MetricsResponseModels[]>(HttpStatusCode.Unauthorized, "Failed to login", null);
            // Start building the query string
            var queryParams = new List<string>
            {
                $"startAt={metricsRequest.StartAt}",
                $"endAt={metricsRequest.EndAt}",
                $"type={metricsRequest.Type}"
            };

            // Add optional parameters if they are not null
            if (!string.IsNullOrEmpty(metricsRequest.Url)) queryParams.Add($"url={metricsRequest.Url}");
            if (!string.IsNullOrEmpty(metricsRequest.Referrer)) queryParams.Add($"referrer={metricsRequest.Referrer}");
            if (!string.IsNullOrEmpty(metricsRequest.Title)) queryParams.Add($"title={metricsRequest.Title}");
            if (!string.IsNullOrEmpty(metricsRequest.Query)) queryParams.Add($"query={metricsRequest.Query}");
            if (!string.IsNullOrEmpty(metricsRequest.Host)) queryParams.Add($"host={metricsRequest.Host}");
            if (!string.IsNullOrEmpty(metricsRequest.Os)) queryParams.Add($"os={metricsRequest.Os}");
            if (!string.IsNullOrEmpty(metricsRequest.Browser)) queryParams.Add($"browser={metricsRequest.Browser}");
            if (!string.IsNullOrEmpty(metricsRequest.Device)) queryParams.Add($"device={metricsRequest.Device}");
            if (!string.IsNullOrEmpty(metricsRequest.Country)) queryParams.Add($"country={metricsRequest.Country}");
            if (!string.IsNullOrEmpty(metricsRequest.Region)) queryParams.Add($"region={metricsRequest.Region}");
            if (!string.IsNullOrEmpty(metricsRequest.City)) queryParams.Add($"city={metricsRequest.City}");
            if (!string.IsNullOrEmpty(metricsRequest.Language)) queryParams.Add($"language={metricsRequest.Language}");
            if (!string.IsNullOrEmpty(metricsRequest.Event)) queryParams.Add($"event={metricsRequest.Event}");
            if (metricsRequest.Limit.HasValue) queryParams.Add($"limit={metricsRequest.Limit}");

            // Combine the query parameters into a query string
            var queryString = string.Join("&", queryParams);


            // Make the HTTP request
            var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/metrics?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<MetricsResponseModels[]>();
                return new UmamiResult<MetricsResponseModels[]>(response.StatusCode, response.ReasonPhrase ?? "Success",
                    content ?? Array.Empty<MetricsResponseModels>());
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await authService.LoginAsync();
                return await GetMetrics(metricsRequest);
            }

            logger.LogError("Failed to get metrics");
            return new UmamiResult<MetricsResponseModels[]>(response.StatusCode,
                response.ReasonPhrase ?? "Failed to get metrics", null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get metrics");
            return new UmamiResult<MetricsResponseModels[]>(HttpStatusCode.InternalServerError, "Failed to get metrics",
                null);
        }
    }
}