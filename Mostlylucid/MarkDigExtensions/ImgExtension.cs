namespace Mostlylucid.MarkDigExtensions;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

public class ImgExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += ChangeImgPath;
    }

    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            if (link.IsImage)
                link.Url = "/articleimages/" + link.Url;
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}