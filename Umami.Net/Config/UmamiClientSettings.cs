namespace Umami.Net.Config;

public class UmamiClientSettings : IConfigSection
{
    public string UmamiPath { get; set; } = string.Empty;

    public string WebsiteId { get; set; } = string.Empty;
    public static string Section => "Analytics";
}

public class UmamiDataSettings : UmamiClientSettings
{
    /// <summary>Self-hosted Umami 3.4 API key (Bearer) or Cloud API key.</summary>
    public string? ApiKey { get; set; }
    public bool UseCloudApi { get; set; }
    public Umami.Net.UmamiData.Models.UmamiApiVersion ApiVersion { get; set; } = Umami.Net.UmamiData.Models.UmamiApiVersion.V3;

    private string? _username;
    public string Username { get => _username ?? User ?? "admin"; set => _username = value; }
    /// <summary>Legacy configuration alias. Explicit Username takes precedence.</summary>
    public string? User { get; set; }
    public string Password { get; set; } = "password";
}