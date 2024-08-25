using System.ComponentModel.DataAnnotations;

namespace Mostlylucid.MarkdownTranslator;

public class PageTranslationModel : MarkdownTranslationModel
{
    public required string OriginalFileName { get; set; }
    
    public bool Persist { get; set; } = true;
}

public class MarkdownTranslationModel
{
    [Required]
    public required string OriginalMarkdown { get; set; }

    [Required] public string Language { get; set; } = "";
}