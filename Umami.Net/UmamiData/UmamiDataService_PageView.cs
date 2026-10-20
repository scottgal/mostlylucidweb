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
        ArgumentNullException.ThrowIfNull(pageViewsRequest);
        return await RequestAsync<PageViewsResponseModel>(HttpMethod.Get, WebsitePath("pageviews") + Query(pageViewsRequest));
    }
}