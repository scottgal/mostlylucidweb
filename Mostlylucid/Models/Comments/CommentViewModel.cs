using Mostlylucid.Shared;

namespace Mostlylucid.Models.Comments;

public record CommentViewModel(
    int Id,
    DateTime Date,
    string Author,
    CommentStatus Status,
    string Content,
    int BlogPostId,
    int ParentId = 0,
    int Depth = 0)
{
    public bool IsAdmin { get; set; } = false;
}