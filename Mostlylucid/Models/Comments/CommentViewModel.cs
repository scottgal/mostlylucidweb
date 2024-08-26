using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Models.Comments;

public  record CommentViewModel(int Id, DateTime Date, string Author, CommentStatus Status, string Content, int BlogPostId, int ParentId=0);