using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mostlylucid.EntityFramework.Models;

public class Comments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Comment { get; set; }
    
    public string Slug { get; set; }
    
    
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } 
}