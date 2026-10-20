namespace Umami.Net.UmamiData.Models;

/// <summary>
/// Represents the detected or configured Umami API version.
/// </summary>
public enum UmamiApiVersion
{
    /// <summary>
    /// Unspecified legacy version information; the data client uses its configured ApiVersion.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Umami v1 API (uses 'url' and 'host' parameters)
    /// </summary>
    V1 = 1,

    /// <summary>
    /// Umami v2 API (uses 'path' and 'hostname' parameters)
    /// </summary>
    V2 = 2,

    /// <summary>
    /// Umami v3 API (same as v2 but with additional features)
    /// </summary>
    V3 = 3
}

/// <summary>
/// Holds information about the detected Umami server version.
/// </summary>
public class UmamiVersionInfo
{
    /// <summary>
    /// The detected API version.
    /// </summary>
    public UmamiApiVersion ApiVersion { get; set; } = UmamiApiVersion.Unknown;

    /// <summary>
    /// The full version string from the server (e.g., "2.10.0").
    /// </summary>
    public string? ServerVersion { get; set; }

    /// <summary>
    /// When the version was last detected.
    /// </summary>
    public DateTime? DetectedAt { get; set; }

    /// <summary>
    /// Whether version detection was successful.
    /// </summary>
    public bool IsDetected => ApiVersion != UmamiApiVersion.Unknown;
}
