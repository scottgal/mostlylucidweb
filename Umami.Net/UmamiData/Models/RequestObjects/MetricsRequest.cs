﻿using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

public class MetricsRequest : BaseRequest
{
    [QueryStringParameter("type", true)] public MetricType Type { get; set; } // Metrics type

    [QueryStringParameter("url")] public string? Url { get; set; } // Name of URL

    [QueryStringParameter("referrer")] public string? Referrer { get; set; } // Name of referrer

    [QueryStringParameter("title")] public string? Title { get; set; } // Name of page title

    [QueryStringParameter("query")] public string? Query { get; set; } // Name of query

    [QueryStringParameter("host")] public string? Host { get; set; } // Name of hostname

    [QueryStringParameter("os")] public string? Os { get; set; } // Name of operating system

    [QueryStringParameter("browser")] public string? Browser { get; set; } // Name of browser

    [QueryStringParameter("device")] public string? Device { get; set; } // Name of device (e.g., Mobile)

    [QueryStringParameter("country")] public string? Country { get; set; } // Name of country

    [QueryStringParameter("region")] public string? Region { get; set; } // Name of region/state/province

    [QueryStringParameter("city")] public string? City { get; set; } // Name of city

    [QueryStringParameter("language")] public string? Language { get; set; } // Name of language

    [QueryStringParameter("event")] public string? Event { get; set; } // Name of event

    [QueryStringParameter("limit")] public int? Limit { get; set; } = 500; // Number of events returned (default: 500)
}