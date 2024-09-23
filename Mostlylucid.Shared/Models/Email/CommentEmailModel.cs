namespace Mostlylucid.Shared.Models.Email;

public class CommentEmailModel : BaseEmailModel
{
    
    
    public string PostUrl { get; set; } = "";
    
    public int CommentId { get; set; }
    protected override string Subject => "New Comment";


}