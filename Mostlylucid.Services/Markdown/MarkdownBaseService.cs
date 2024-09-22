using Markdig;
using Mostlylucid.Services.Markdown.MarkDigExtensions;

namespace Mostlylucid.Services.Markdown;

public class MarkdownBaseService
{
    public const string EnglishLanguage = "en";
    protected   MarkdownPipeline Pipeline() =>  new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseTableOfContent()
        .Use<ImgExtension>()
        .Build();




}