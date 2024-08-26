namespace Mostlylucid.Models.Comments;

public class CommentViewList
{
    public int PostId { get; set; }
    public bool IsAdmin { get; set; } = false;
    public List<CommentViewModel> Comments { get; set; } = new();
    
}