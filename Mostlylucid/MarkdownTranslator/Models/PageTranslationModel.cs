namespace Mostlylucid.MarkdownTranslator;

public class PageTranslationModel : MarkdownTranslationModel
{
    public required string OriginalFileName { get; set; }
    
    public bool Persist { get; set; } = true;
}

public class MarkdownTranslationModel
{
    public required string OriginalMarkdown { get; set; }
    public  string Language { get; set; } = "";
}