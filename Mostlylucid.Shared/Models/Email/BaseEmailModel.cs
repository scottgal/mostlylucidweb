namespace Mostlylucid.Shared.Models.Email;

public class BaseEmailModel
{
    public string SenderEmail { get; set; }
    
    public string SenderName { get; set; }
    
    public string Content { get; set; }
    
    protected virtual string Subject { get; set; }
    
}