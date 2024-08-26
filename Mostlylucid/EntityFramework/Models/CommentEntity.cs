using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mostlylucid.EntityFramework.Models;

[Table("comments")]
public class CommentEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("status")]
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    
    [Column("author")]
    public string Author { get; set; }
    
    [Column("html_content")]
    public string? HtmlContent { get; set; }
    
    [Column("content")]
    public string Content { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("post_id")]
    // Foreign key for the post this comment belongs to
    public int PostId { get; set; }
    
    public BlogPostEntity Post { get; set; }

    public int? ParentCommentId { get; set; }
    public CommentEntity ParentComment { get; set; } 
    
    // Navigation property for the closure table relationships
    public ICollection<CommentClosure> Ancestors { get; set; } = new List<CommentClosure>();
    public ICollection<CommentClosure> Descendants { get; set; }= new List<CommentClosure>();
}

public enum CommentStatus
{
    Pending,
    Approved,
    Rejected,
    Deleted
}

[Table("comment_closures")]
public class CommentClosure
{
    [Column("ancestor_id")]
    public int AncestorId { get; set; }
    [Column("descendant_id")]
    public CommentEntity Ancestor { get; set; }

    [Column("descendant_id")]
    public int DescendantId { get; set; }
    public CommentEntity Descendant { get; set; }

    [Column("depth")]
    public int Depth { get; set; } // 0 means direct parent-child, 1 means grandparent-grandchild, etc.
}