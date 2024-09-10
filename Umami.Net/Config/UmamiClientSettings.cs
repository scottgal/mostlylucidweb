namespace Umami.Net.Config;

public class UmamiClientSettings : IConfigSection
{
    public string UmamiPath { get; set; } = string.Empty;

    public string WebsiteId { get; set; } = string.Empty;
    public static string Section => "Analytics";
}

public class UmamiDataSettings : UmamiClientSettings
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "password";
}