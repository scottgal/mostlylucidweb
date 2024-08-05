namespace Mostlylucid.Config;

public class TranslateServiceConfig :IConfigSection
{
    public static string Section => "TranslateService";
    
    public bool Enabled { get; set; }
    
    public string[] IPs { get; set; }
    
    public string[] Languages { get; set; }
}