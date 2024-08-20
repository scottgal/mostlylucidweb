using System.Threading.Tasks.Dataflow;
using Mostlylucid.Blog;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.MarkdownTranslator;

public class BackgroundTranslateService(
    MarkdownConfig markdownConfig,
    TranslateServiceConfig translateServiceConfig,
    MarkdownTranslatorService markdownTranslatorService,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundTranslateService> logger) : IHostedLifecycleService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service started");
    }

    private readonly BufferBlock<(PageTranslationModel, TaskCompletionSource<(BlogPostViewModel? model,bool complete)>)> _translations = new();
    private Task _sendTask = Task.CompletedTask;

    public async Task<Task<(BlogPostViewModel? model,bool complete)>> Translate(PageTranslationModel message)
    {
        var tcs = new TaskCompletionSource<(BlogPostViewModel? model,bool complete)>();
        await _translations.SendAsync((message, tcs));
        return tcs.Task;
    }

    public async Task<List<Task<(BlogPostViewModel? model, bool complete)>>> TranslateForAllLanguages(PageTranslationModel message)
    {
        var tasks = new List<Task<(BlogPostViewModel? model, bool complete)>>();
        foreach (var language in translateServiceConfig.Languages)
        {
            var translateMessage = new PageTranslationModel
            {
                Language = language,
                OriginalFileName = message.OriginalFileName,
                OriginalMarkdown = message.OriginalMarkdown
            };
            var tcs = new TaskCompletionSource<(BlogPostViewModel? model, bool complete)>();
            await _translations.SendAsync((translateMessage, tcs));
            tasks.Add(tcs.Task);
        }

        return tasks;
    }

    public string OutFileName(string originalFileName, string language) => Path.GetFileNameWithoutExtension(originalFileName) + "." + language + ".md";
    
    public async Task TranslateAllFilesAsync()
    {
        var markdownFiles = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");
        foreach (var file in markdownFiles)
        {
            var fileChanged = await file.IsFileChanged(file);
            var text = await File.ReadAllTextAsync(file);
            
            await TranslateForAllLanguages(new PageTranslationModel
            {
                OriginalMarkdown = await File.ReadAllTextAsync(file),
                OriginalFileName = Path.GetFileName(file),
            });
        }
    }

    private async Task TranslateFilesAsync(CancellationToken cancellationToken)
    {
        if (!await markdownTranslatorService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }

        while (!cancellationToken.IsCancellationRequested && _translations.TryReceive(out var item))

        {
            var translateModel = item.Item1;
            var tcs = item.Item2;
            var outFileName = translateModel.OutFileName;
            logger.LogInformation("Translating {File} to {Language}", translateModel.OriginalFileName,
                translateModel.Language);
            try
            {
                var translatedMarkdown =
                    await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                        translateModel.Language, cancellationToken);

                BlogPostViewModel? postModel = null;
                if (item.Item1.Persist)
                {


                    var blogService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IBlogService>();
                     postModel = await blogService.SavePost(translateModel.OriginalFileName, translateModel.Language,
                        translatedMarkdown);
                }
                else
                {
                    var populatorService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<MarkdownRenderingService>();
                    postModel =  populatorService.GetPageFromMarkdown(translatedMarkdown, DateTime.Now,  translateModel.OutFileName);
                }

                tcs.SetResult((postModel, true));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error translating {File} to {Language}", translateModel.OriginalFileName,
                    translateModel.Language);
                tcs.SetException(e);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service stopped");
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if (!await markdownTranslatorService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }

        _sendTask = TranslateFilesAsync(cancellationToken);


        logger.LogInformation("Background translation service started");
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service starting");
    }

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service stopped");
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service stopping");
    }
}