namespace Umami.Net.UmamiData.Models.RequestObjects;

/// <summary>
/// Metric types supported by the Umami Analytics API.
/// These correspond to the 'type' parameter in the metrics endpoint.
/// </summary>
public enum MetricType
{
    /// <summary>Page URLs</summary>
    url,

    /// <summary>URL paths only</summary>
    path,

    /// <summary>Traffic sources/referrers</summary>
    referrer,

    /// <summary>Page titles</summary>
    title,

    /// <summary>Query parameters</summary>
    query,

    /// <summary>Browsers</summary>
    browser,

    /// <summary>Operating systems</summary>
    os,

    /// <summary>Device types</summary>
    device,

    /// <summary>Countries</summary>
    country,

    /// <summary>Regions/states</summary>
    region,

    /// <summary>Cities</summary>
    city,

    /// <summary>Languages</summary>
    language,

    /// <summary>Screen resolutions</summary>
    screen,

    /// <summary>Hostnames/domains</summary>
    hostname,

    /// <summary>Custom events</summary>
    @event,

    /// <summary>Entry pages</summary>
    entry,

    /// <summary>Exit pages</summary>
    exit,

    /// <summary>Content tags</summary>
    tag,

    /// <summary>Traffic channels</summary>
    channel,

    /// <summary>Full domain names</summary>
    domain,
    distinctId,
    utmSource,
    utmMedium,
    utmCampaign,
    utmContent,
    utmTerm
}