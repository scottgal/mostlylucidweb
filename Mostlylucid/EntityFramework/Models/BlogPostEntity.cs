using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace Mostlylucid.EntityFramework.Models;

[Table("blogposts")]
public class BlogPostEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; }
    
    [Column("slug")]
    public string Slug { get; set; }
    
    [Column("updated_date")]
    public DateTimeOffset UpdatedDate { get; set; } = DateTimeOffset.UtcNow;
    
    [Column("markdown")]
    public string Markdown { get; set; } = string.Empty;
    
    [Column("html_content")]
    public string HtmlContent { get; set; }
    
    [Column("plain_text_content")]
    public string PlainTextContent { get; set; }
    
    [Column("content_hash")]
    public string ContentHash { get; set; }
    
    [Column("word_count")]
    public int WordCount { get; set; }
    
    [Column("language_id")]
    public int LanguageId { get; set; }
    
    
    public LanguageEntity LanguageEntity { get; set; }
    
    
    public ICollection<CommentEntity> Comments { get; set; }
    public ICollection<CategoryEntity> Categories { get; set; }
    
    [Column("published_date")]
    public DateTimeOffset PublishedDate { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    
    [Column("search_vector")]
    public NpgsqlTsVector SearchVector { get; set; }
  
    
}