namespace Umami.Net.UmamiData.Models.RequestObjects;

public class MetricsRequest : BaseRequest
{
    public MetricType Type { get; set; } // Metrics type

    // Optional properties
    public string? Url { get; set; } // Name of URL
    public string? Referrer { get; set; } // Name of referrer
    public string? Title { get; set; } // Name of page title
    public string? Query { get; set; } // Name of query
    public string? Host { get; set; } // Name of hostname
    public string? Os { get; set; } // Name of operating system
    public string? Browser { get; set; } // Name of browser
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    public string? Country { get; set; } // Name of country
    public string? Region { get; set; } // Name of region/state/province
    public string? City { get; set; } // Name of city
    public string? Language { get; set; } // Name of language
    public string? Event { get; set; } // Name of event
    public int? Limit { get; set; } = 500; // Number of events returned (default: 500)
}