using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mostlylucid.EntityFramework.Models;

public class CommentEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Content { get; set; }
    
    public bool Moderated { get; set; }
    
    public DateTimeOffset Date { get; set; }
    
    public string Name { get; set; }
    public string Email { get; set; }
    public string? Avatar { get; set; }
    public string Slug { get; set; }
    public int BlogPostId { get; set; }
    public BlogPostEntity BlogPostEntity { get; set; } 
}