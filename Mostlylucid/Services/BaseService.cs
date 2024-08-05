using Markdig;
using Mostlylucid.MarkDigExtensions;

namespace Mostlylucid.Services;

public class BaseService
{
    protected  readonly MarkdownPipeline _pipeline;
    protected  BaseService()
    {   
        _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseTableOfContent().Use<ImgExtension>()
            .Build();
    }
}