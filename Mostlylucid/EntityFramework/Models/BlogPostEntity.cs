using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mostlylucid.EntityFramework.Models;

public class BlogPostEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Slug { get; set; }
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; }
    public string ContentHash { get; set; }
    
    public int WordCount { get; set; }
    
    public int LanguageId { get; set; }
    public LanguageEntity LanguageEntity { get; set; }
    public ICollection<CommentEntity> Comments { get; set; }
    public ICollection<CategoryEntity> Categories { get; set; }
    
    public DateTimeOffset PublishedDate { get; set; }
    
}