namespace Mostlylucid.Config;

public class TranslateServiceConfig :IConfigSection
{
    public static string Section => "TranslateService";
    
    public bool Enabled { get; set; }
    
    public string[] IPs { get; set; }
    
    public string[] Languages { get; set; }
    
    public AutoTranslateMode Mode { get; set; } = AutoTranslateMode.SaveToDisk;
}

public enum AutoTranslateMode
{
   SaveToDisk,
   SaveToDatabase
}