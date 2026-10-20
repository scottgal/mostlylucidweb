using Umami.Net.UmamiData.Helpers;

namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Request model for retrieving metrics from the Umami API.
/// Metrics provide aggregated counts for specific dimensions (URLs, countries, browsers, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Metrics return counts of unique values for the specified type. For example:
/// - Type = <see cref="MetricType.url"/> returns page view counts per URL
/// - Type = <see cref="MetricType.country"/> returns visitor counts per country
/// - Type = <see cref="MetricType.@event"/> returns custom event counts
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var request = new MetricsRequest
/// {
///     StartAtDate = DateTime.UtcNow.AddDays(-7),
///     EndAtDate = DateTime.UtcNow,
///     Type = MetricType.url,
///     Unit = Unit.day,
///     Limit = 50
/// };
/// </code>
/// </para>
/// </remarks>
public class MetricsRequest : BaseRequest
{
    [QueryStringParameter("search")] public string? Search { get; set; }

    private int? _limit = 500;

    /// <summary>
    /// Gets or sets the type of metric to retrieve (required).
    /// </summary>
    /// <remarks>
    /// Common types:
    /// <list type="bullet">
    /// <item><description><see cref="MetricType.url"/> - Page URLs (most common for page views)</description></item>
    /// <item><description><see cref="MetricType.@event"/> - Custom event names</description></item>
    /// <item><description><see cref="MetricType.country"/> - Geographic distribution</description></item>
    /// <item><description><see cref="MetricType.browser"/> - Browser analytics</description></item>
    /// <item><description><see cref="MetricType.referrer"/> - Traffic sources</description></item>
    /// </list>
    /// See <see cref="MetricType"/> for all available options.
    /// </remarks>
    [QueryStringParameter("type", true)]
    public MetricType Type { get; set; }

    /// <summary>
    /// Gets or sets the time unit for data aggregation (optional).
    /// Default is <see cref="Unit.day"/>.
    /// </summary>
    /// <remarks>
    /// Choose based on your date range:
    /// <list type="bullet">
    /// <item><description><see cref="Unit.hour"/> - For recent data (last 24-48 hours)</description></item>
    /// <item><description><see cref="Unit.day"/> - For weekly to monthly ranges (default)</description></item>
    /// <item><description><see cref="Unit.month"/> - For quarterly or yearly data</description></item>
    /// <item><description><see cref="Unit.year"/> - For multi-year analysis</description></item>
    /// </list>
    /// </remarks>
    [QueryStringParameter("unit", false)]
    public Unit? Unit { get; set; }

    /// <summary>
    /// Gets or sets a path filter to return metrics for a specific page.
    /// This is the URL path, not including the domain.
    /// </summary>
    /// <example>
    /// "/blog/post-title" or "/products" or "/"
    /// </example>
    [QueryStringParameter("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets a referrer filter to analyze traffic from specific sources.
    /// </summary>
    /// <example>
    /// "google.com" or "https://twitter.com"
    /// </example>
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; }

    /// <summary>
    /// Gets or sets a page title filter.
    /// </summary>
    /// <example>
    /// "Home Page" or "Product Details"
    /// </example>
    [QueryStringParameter("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a query parameter filter.
    /// </summary>
    /// <example>
    /// "utm_source=newsletter" or "ref=homepage"
    /// </example>
    [QueryStringParameter("query")]
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets a hostname filter for multi-domain tracking.
    /// This is the full domain name.
    /// </summary>
    /// <example>
    /// "www.example.com" or "blog.example.com"
    /// </example>
    [QueryStringParameter("hostname")]
    public string? Hostname { get; set; }

    /// <summary>
    /// Gets or sets an operating system filter.
    /// </summary>
    /// <example>
    /// "Windows" or "macOS" or "Linux" or "Android" or "iOS"
    /// </example>
    [QueryStringParameter("os")]
    public string? Os { get; set; }

    /// <summary>
    /// Gets or sets a browser filter.
    /// </summary>
    /// <example>
    /// "Chrome" or "Firefox" or "Safari" or "Edge"
    /// </example>
    [QueryStringParameter("browser")]
    public string? Browser { get; set; }

    /// <summary>
    /// Gets or sets a device type filter.
    /// </summary>
    /// <example>
    /// "Mobile" or "Desktop" or "Tablet"
    /// </example>
    [QueryStringParameter("device")]
    public string? Device { get; set; }

    /// <summary>
    /// Gets or sets a country filter using ISO 3166-1 alpha-2 country codes.
    /// </summary>
    /// <example>
    /// "US" or "GB" or "FR" or "JP"
    /// </example>
    [QueryStringParameter("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets a region/state/province filter.
    /// </summary>
    /// <example>
    /// "California" or "New York" or "Ontario"
    /// </example>
    [QueryStringParameter("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets a city filter.
    /// </summary>
    /// <example>
    /// "San Francisco" or "London" or "Tokyo"
    /// </example>
    [QueryStringParameter("city")]
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets a language filter using ISO 639-1 language codes.
    /// </summary>
    /// <example>
    /// "en" or "es" or "fr" or "de"
    /// </example>
    [QueryStringParameter("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets an event name filter for custom tracked events.
    /// Optional filter when listing event names.
    /// </summary>
    /// <example>
    /// "button-click" or "form-submit" or "video-play"
    /// </example>
    [QueryStringParameter("event")]
    public string? Event { get; set; }

    /// <summary>
    /// Gets or sets a tag filter for categorized content.
    /// </summary>
    /// <example>
    /// "blog" or "product" or "landing-page"
    /// </example>
    [QueryStringParameter("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets a segment filter for advanced filtering.
    /// </summary>
    [QueryStringParameter("segment")]
    public string? Segment { get; set; }

    /// <summary>
    /// Gets or sets a cohort filter for cohort analysis.
    /// </summary>
    [QueryStringParameter("cohort")]
    public string? Cohort { get; set; }

    /// <summary>
    /// Gets or sets the timezone for date calculations.
    /// Uses IANA timezone identifiers (e.g., "America/New_York").
    /// </summary>
    /// <example>
    /// "America/Los_Angeles" or "Europe/London" or "Asia/Tokyo"
    /// </example>
    [QueryStringParameter("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// Default is 500, maximum is typically 500.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when limit is less than 1 or greater than 500.
    /// Suggestion: Use a value between 1 and 500.
    /// </exception>
    [QueryStringParameter("limit")]
    public int? Limit
    {
        get => _limit;
        set
        {
            if (value.HasValue && (value < 1 || value > 500))
            {
                throw new ArgumentException(
                    $"Limit must be between 1 and 500, got {value}. " +
                    "Suggestion: Use a reasonable limit (e.g., 50 for top items, 500 for comprehensive data).",
                    nameof(Limit));
            }
            _limit = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of results to skip (for pagination).
    /// Default is 0.
    /// </summary>
    [QueryStringParameter("offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// Validates the metrics request parameters.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Event is required but not provided, or other validation fails.
    /// </exception>
    public override void Validate()
    {
        base.Validate();

        if (!Enum.IsDefined(Type)) throw new ArgumentException("Unknown metric type", nameof(Type));
        if (Offset < 0) throw new ArgumentOutOfRangeException(nameof(Offset));
    }
}