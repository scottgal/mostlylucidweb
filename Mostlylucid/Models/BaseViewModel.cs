namespace Mostlylucid.Models;

public class BaseViewModel
{
    
    public bool Authenticated { get; set; }
    public string? Name { get; set; }
    
    public string? AvatarUrl { get; set; }
}