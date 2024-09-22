using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mostlylucid.Services.Markdown.MarkDigExtensions;

public class ImgExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += ChangeImgPath;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }

    private void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
            {
                var url = link.Url;
                if(url.StartsWith("http:") || url.StartsWith("https:")) continue;
                
                if (!url.Contains("?"))
                {
                   url += "?format=webp&quality=50";
                }

                url = "/articleimages/" + url;
                link.Url = url;
            }
               
    }
}