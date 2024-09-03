namespace Umami.Net.UmamiData.Models.RequestObjects;

public class PageViewsRequest : BaseRequest
{
    // Required properties

    public Unit Unit { get; set; } = Unit.Day; // Time unit (year | month | hour | day)
    public string Timezone { get; set; } // Timezone (ex. America/Los_Angeles)

    // Optional properties
    public string? Url { get; set; } // Name of URL
    public string? Referrer { get; set; } // Name of referrer
    public string? Title { get; set; } // Name of page title
    public string? Host { get; set; } // Name of hostname
    public string? Os { get; set; } // Name of operating system
    public string? Browser { get; set; } // Name of browser
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    public string? Country { get; set; } // Name of country
    public string? Region { get; set; } // Name of region/state/province
    public string? City { get; set; } // Name of city
}