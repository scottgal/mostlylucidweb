using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.UmamiData.Helpers;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

/// <summary>
///     This is the partial class for the PageViews method in the UmamiDataService
/// </summary>
/// <param name="analyticsSettings"></param>
/// <param name="authService"></param>
/// <param name="logger"></param>
public partial class UmamiDataService
{
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(DateTime startDate, DateTime endDate,
        Unit unit = Unit.day)
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
        if (await authService.Login() == false)
            return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
 
        var queryString = pageViewsRequest.ToQueryString();


      // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews{queryString}");

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully got page views");
            var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
            return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new PageViewsResponseModel());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.Login();
            return await GetPageViews(pageViewsRequest);
        }

        logger.LogError("Failed to get page views");
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode,
            response.ReasonPhrase ?? "Failed to get page views", null);
    }
}