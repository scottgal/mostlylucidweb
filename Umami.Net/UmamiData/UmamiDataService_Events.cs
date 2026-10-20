using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.UmamiData.Helpers;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

/// <summary>
/// Partial class for Events-related endpoints in the UmamiDataService
/// </summary>
public partial class UmamiDataService
{
    /// <summary>
    /// Gets event data series for the website within a specified time range
    /// </summary>
    /// <param name="request">Request parameters including time range and optional event filter</param>
    /// <returns>Array of event data points with timestamps and counts</returns>
    public async Task<UmamiResult<EventsSeriesResponseModel[]>> GetEventsSeries(EventsSeriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await RequestAsync<EventsSeriesResponseModel[]>(HttpMethod.Get, WebsitePath("events/series") + Query(request));
    }

    /// <summary>
    /// Gets event data series for the website within a specified time range
    /// </summary>
    /// <param name="startDate">Start date for the time range</param>
    /// <param name="endDate">End date for the time range</param>
    /// <param name="unit">Time unit for data bucketing (minute, hour, day, month, year)</param>
    /// <param name="timezone">Timezone for the data (e.g., "America/Los_Angeles")</param>
    /// <param name="eventName">Optional event name to filter by</param>
    /// <returns>Array of event data points</returns>
    public async Task<UmamiResult<EventsSeriesResponseModel[]>> GetEventsSeries(
        DateTime startDate,
        DateTime endDate,
        Unit unit = Unit.day,
        string? timezone = null,
        string? eventName = null)
    {
        var request = new EventsSeriesRequest
        {
            StartAtDate = startDate,
            EndAtDate = endDate,
            Unit = unit,
            Timezone = timezone ?? "UTC",
            EventName = eventName
        };

        return await GetEventsSeries(request);
    }
}
