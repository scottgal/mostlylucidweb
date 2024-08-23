using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mostlylucid.EntityFramework.Models;

[Table("comments")]
public class CommentEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("content")]
    public string Content { get; set; }
    
    [Column("moderated")]
    public bool Moderated { get; set; }
    
    [Column("date")]
    public DateTimeOffset Date { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
    
    [Column("avatar")]
    public string? Avatar { get; set; }
    
    [Column("slug")]
    public string Slug { get; set; }
    
    [Column("blog_post_id")]
    public int BlogPostId { get; set; }
    public BlogPostEntity BlogPostEntity { get; set; } 
}