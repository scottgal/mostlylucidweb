namespace Umami.Net.Config;

public class UmamiClientSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string UmamiPath { get; set; } = string.Empty;

    public string WebsiteId { get; set; } = string.Empty;
}

public class UmamiDataSettings : UmamiClientSettings
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "password";
}