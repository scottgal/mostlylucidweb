namespace Mostlylucid.Config.Markdown;

public class MarkdownConfig : IConfigSection
{
    public static string Section => "Markdown";
    public string MarkdownPath { get; set; }= "Markdown";
    public string MarkdownTranslatedPath { get; set; } = "Markdown/translated";
    public string MarkdownCommentsPath { get; set; }= "Markdown/comments";

    
}