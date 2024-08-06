namespace Mostlylucid.Email.Models;

public class CommentEmailModel : BaseEmailModel
{
    protected override string Subject => "New Comment";
    public string PostSlug { get; set; }

}