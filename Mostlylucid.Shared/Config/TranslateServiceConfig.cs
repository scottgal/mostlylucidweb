namespace Mostlylucid.Shared.Config;

public class TranslateServiceConfig :IConfigSection
{
    public static string Section => "TranslateService";
    
    public bool Enabled { get; set; }
    
    public string[] IPs { get; set; }

    public string ServiceIPs
    {
        get => string.Join(";", IPs);
        set
        {
            if(string.IsNullOrEmpty(value)) return;
            if(value.Contains(";"))
            {
                IPs = value.Split(";");
            }
            else
            {
                IPs = new string[]{value};
            }
        }
    }

    public string[] Languages { get; set; }
    
    public AutoTranslateMode Mode { get; set; } = AutoTranslateMode.SaveToDisk;
}

public enum AutoTranslateMode
{
   SaveToDisk,
   SaveToDatabase
}