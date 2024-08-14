using Mostlylucid.Config;

namespace Mostlylucid.Umami;

public class UmamiSettings : IConfigSection
{
    public static string Section => "Umami";
    
    public string BaseUrl { get; set; } = "https://umamilocal.mostlylucid.net";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "password";
}