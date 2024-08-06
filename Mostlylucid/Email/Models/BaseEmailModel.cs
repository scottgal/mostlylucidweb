namespace Mostlylucid.Email.Models;

public class BaseEmailModel
{
    public string SenderEmail { get; set; }
    
    public string SenderName { get; set; }
    
    public string Comment { get; set; }
    
    protected virtual string Subject { get; set; }
    
}