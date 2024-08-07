namespace Mostlylucid.Models.Comments;

public class CommentViewModel
{
    public DateTime CreatedDate { get; set; }
    public bool IsModerated { get; set; }
    
    public string Email { get; set; }
    
    public string Slug { get; set; }
    
    public string Comment { get; set; }
}