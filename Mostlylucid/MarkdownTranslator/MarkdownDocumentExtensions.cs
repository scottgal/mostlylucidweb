using Markdig.Syntax;

namespace Mostlylucid.MarkdownTranslator;
public static class MarkdownDocumentExtensions
{
    public static string ToMarkdownString(this MarkdownDocument document)
    {
        var writer = new StringWriter();
        var renderer = new Markdig.Renderers.Normalize.NormalizeRenderer(writer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }
}