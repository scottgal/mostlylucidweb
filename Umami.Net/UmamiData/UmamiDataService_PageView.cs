using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;
/// <summary>
/// This is the partial class for the PageViews method in the UmamiDataService
/// </summary>
/// <param name="analyticsSettings"></param>
/// <param name="authService"></param>
/// <param name="logger"></param>
public partial class UmamiDataService
{
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(DateTime startDate, DateTime endDate, Unit unit = Unit.Day)
    {
        var pageViewsRequest = new PageViewsRequest
        {
            StartAtDate = startDate,
            EndAtDate = endDate,
            Unit = unit
        };
        return await GetPageViews(pageViewsRequest);
    }
    
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(PageViewsRequest pageViewsRequest)
{
    if(await authService.LoginAsync() == false)
    {
        return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
    }
    // Start building the query string
    var queryParams = new List<string>
    {
        $"startAt={pageViewsRequest.StartAt}",
        $"endAt={pageViewsRequest.EndAt}",
        $"unit={pageViewsRequest.Unit.ToLowerString()}"
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
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new PageViewsResponseModel());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
       
        await authService.LoginAsync();
        return await GetPageViews(pageViewsRequest);
    }

    logger.LogError("Failed to get page views");
    return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Failed to get page views", null);
}

}