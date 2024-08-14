using System.Net;
using Mostlylucid.Config;
using Mostlylucid.Umami.Models;
using Mostlylucid.Umami.Models.RequestObjects;

namespace Mostlylucid.Umami;

public class UmamiService(AnalyticsSettings analyticsSettings, AuthService authService, ILogger<UmamiService> logger)
{

    private string WebsiteId => analyticsSettings.WebsiteId;
    
public async Task<UmamiResult<StatsResponseModels>> GetStatsAsync(StatsRequest statsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={statsRequest.StartAt}",
        $"end={statsRequest.EndAt}"
    };

    // Add optional parameters if they are not null
    if (!string.IsNullOrEmpty(statsRequest.Url)) queryParams.Add($"url={statsRequest.Url}");
    if (!string.IsNullOrEmpty(statsRequest.Referrer)) queryParams.Add($"referrer={statsRequest.Referrer}");
    if (!string.IsNullOrEmpty(statsRequest.Title)) queryParams.Add($"title={statsRequest.Title}");
    if (!string.IsNullOrEmpty(statsRequest.Query)) queryParams.Add($"query={statsRequest.Query}");
    if (!string.IsNullOrEmpty(statsRequest.Event)) queryParams.Add($"event={statsRequest.Event}");
    if (!string.IsNullOrEmpty(statsRequest.Host)) queryParams.Add($"host={statsRequest.Host}");
    if (!string.IsNullOrEmpty(statsRequest.Os)) queryParams.Add($"os={statsRequest.Os}");
    if (!string.IsNullOrEmpty(statsRequest.Browser)) queryParams.Add($"browser={statsRequest.Browser}");
    if (!string.IsNullOrEmpty(statsRequest.Device)) queryParams.Add($"device={statsRequest.Device}");
    if (!string.IsNullOrEmpty(statsRequest.Country)) queryParams.Add($"country={statsRequest.Country}");
    if (!string.IsNullOrEmpty(statsRequest.Region)) queryParams.Add($"region={statsRequest.Region}");
    if (!string.IsNullOrEmpty(statsRequest.City)) queryParams.Add($"city={statsRequest.City}");

    // Combine the query parameters into a query string
    var queryString = string.Join("&", queryParams);

    // Make the HTTP request
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/stats?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<StatsResponseModels>();
        return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new StatsResponseModels());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetStatsAsync(statsRequest);
    }

    logger.LogError("Failed to get stats");
    return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get stats", null);
}

    
public async Task<UmamiResult<PageViewsResponseModel>> GetPageViewsAsync(PageViewsRequest pageViewsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={pageViewsRequest.StartAt}",
        $"end={pageViewsRequest.EndAt}",
        $"unit={pageViewsRequest.Unit}"
    };

    // Add optional parameters if they are not null
    if (!string.IsNullOrEmpty(pageViewsRequest.Timezone)) queryParams.Add($"timezone={pageViewsRequest.Timezone}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Url)) queryParams.Add($"url={pageViewsRequest.Url}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Referrer)) queryParams.Add($"referrer={pageViewsRequest.Referrer}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Title)) queryParams.Add($"title={pageViewsRequest.Title}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Host)) queryParams.Add($"host={pageViewsRequest.Host}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Os)) queryParams.Add($"os={pageViewsRequest.Os}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Browser)) queryParams.Add($"browser={pageViewsRequest.Browser}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Device)) queryParams.Add($"device={pageViewsRequest.Device}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Country)) queryParams.Add($"country={pageViewsRequest.Country}");
    if (!string.IsNullOrEmpty(pageViewsRequest.Region)) queryParams.Add($"region={pageViewsRequest.Region}");
    if (!string.IsNullOrEmpty(pageViewsRequest.City)) queryParams.Add($"city={pageViewsRequest.City}");

    // Combine the query parameters into a query string
    var queryString = string.Join("&", queryParams);

    // Make the HTTP request
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/metrics/pageviews?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new PageViewsResponseModel());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetPageViewsAsync(pageViewsRequest);
    }

    logger.LogError("Failed to get page views");
    return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Failed to get page views", null);
}

    
public async Task<UmamiResult<MetricsResponseModels>> GetMetricsAsync(MetricsRequest metricsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={metricsRequest.StartAt}",
        $"end={metricsRequest.EndAt}",
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
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/metrics/{metricsRequest.Type}?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<MetricsResponseModels>();
        return new UmamiResult<MetricsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new MetricsResponseModels());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetMetricsAsync(metricsRequest);
    }
    
        logger.LogError("Failed to get metrics");
        return new UmamiResult<MetricsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get metrics", null);
}


    


    
}