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
    public async Task<UmamiResult<StatsResponseModels>> GetStats(DateTime startDate, DateTime endDate)
    {
        var statsRequest = new StatsRequest
        {
            StartAtDate = startDate,
            EndAtDate = endDate
        };
        return await GetStats(statsRequest);
    }

    public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)
    {
        ArgumentNullException.ThrowIfNull(statsRequest);
        return await RequestAsync<StatsResponseModels>(HttpMethod.Get, WebsitePath("stats") + Query(statsRequest));
    }
}