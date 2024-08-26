namespace Mostlylucid.Email.Models;

public class CommentEmailModel : BaseEmailModel
{
    
    
    public string PostUrl { get; set; } = "";
    
    public int CommentId { get; set; }
    protected override string Subject => "New Comment";


}