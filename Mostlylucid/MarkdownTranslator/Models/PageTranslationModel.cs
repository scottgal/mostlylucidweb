namespace Mostlylucid.MarkdownTranslator;

public class PageTranslationModel
{
    public required string OriginalMarkdown { get; set; }
    public  string Language { get; set; } = "";
    
    public required string OriginalFileName { get; set; }
    
    public string TranslatedMarkdown { get; set; } = "";
    
    public string OutFileName { get; set; } = "";
    
    public bool Persist { get; set; } = true;
}