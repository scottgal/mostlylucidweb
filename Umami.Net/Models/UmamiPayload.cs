using System.Text.Json.Serialization;

namespace Umami.Net.Models;

public class UmamiPayload
{
    [JsonIgnore] public bool UseDefaultUserAgent { get; set; }

    [JsonIgnore] public bool PayloadPopulated { get; set; }

    public string Website { get; set; } = null!;
    public string? Hostname { get; set; }
    public string? Language { get; set; }
    public string? Referrer { get; set; }
    public string? Screen { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Name { get; set; }
    public string? Tag { get; set; }
    /// <summary>Unix timestamp in seconds (analytics queries use milliseconds).</summary>
    public long? Timestamp { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public double? Lcp { get; set; }
    public double? Inp { get; set; }
    public double? Cls { get; set; }
    public double? Fcp { get; set; }
    public double? Ttfb { get; set; }
    [JsonIgnore] public string? CacheToken { get; set; }

    public string? SessionId { get; set; }
    
    [JsonPropertyName("id")] public string? DistinctId { get; set; }

    [JsonPropertyName("ip")] public string? IpAddress { get; set; }

    [JsonIgnore] public string? UserAgent { get; set; }


    public UmamiEventData? Data { get; set; }
}

public class UmamiEventData : Dictionary<string, object>
{
}