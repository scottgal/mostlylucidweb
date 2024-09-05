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
        if (await authService.Login() == false)
            return new UmamiResult<StatsResponseModels>(HttpStatusCode.Unauthorized, "Failed to login", null);

        var queryString = statsRequest.ToQueryString();

        // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/stats{queryString}");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<StatsResponseModels>();
            return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new StatsResponseModels());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.Login();
            return await GetStats(statsRequest);
        }

        logger.LogError("Failed to get stats");
        return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get stats",
            null);
    }
}