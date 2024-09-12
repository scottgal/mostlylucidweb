namespace Mostlylucid.OpenSearch.Config;

public class OpenSearchConfig : IConfigSection
{
    public static string Section => "OpenSearch";
    
    public string Endpoint { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
    
}