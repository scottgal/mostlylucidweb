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
    ILogger<BackgroundTranslateService> logger) : IHostedService
{
    private CancellationTokenSource cancellationTokenSource = new();
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!await markdownTranslatorService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }

        await Task.Delay(10000);
        _sendTask =  TranslateFilesAsync(cancellationTokenSource.Token);
        if(translateServiceConfig.Enabled)
            await TranslateAllFilesAsync();
   

    }

    private readonly
        BufferBlock<(PageTranslationModel, TaskCompletionSource<(BlogPostViewModel? model, bool complete)>)>
        _translations = new();

    private Task _sendTask = Task.CompletedTask;

    public async Task<Task<(BlogPostViewModel? model, bool complete)>> Translate(PageTranslationModel message)
    {
        var tcs = new TaskCompletionSource<(BlogPostViewModel? model, bool complete)>();
        await _translations.SendAsync((message, tcs));
        return tcs.Task;
    }

    public async Task<List<Task<(BlogPostViewModel? model, bool complete)>>> TranslateForAllLanguages(
        PageTranslationModel message)
    {
        var tasks = new List<Task<(BlogPostViewModel? model, bool complete)>>();
        foreach (var language in translateServiceConfig.Languages)
        {
            var translateMessage = new PageTranslationModel
            {
                Language = language,
                OriginalFileName = message.OriginalFileName,
                OriginalMarkdown = message.OriginalMarkdown,
                Persist = message.Persist
            };
            var tcs = new TaskCompletionSource<(BlogPostViewModel? model, bool complete)>();
            await _translations.SendAsync((translateMessage, tcs));
            tasks.Add(tcs.Task);
        }

        return tasks;
    }



    public async Task TranslateAllFilesAsync()
    {
        try
        {

        var markdownFiles = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");
        foreach (var file in markdownFiles)
        {
            await TranslateForAllLanguages(new PageTranslationModel
            {
                OriginalMarkdown = await File.ReadAllTextAsync(file),
                OriginalFileName = file,
            });
        }
        
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task TranslateFilesAsync(CancellationToken cancellationToken)
    {
        if (!await markdownTranslatorService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }

        try
        {
        while (!cancellationToken.IsCancellationRequested)

        {

            var scope = scopeFactory.CreateScope();
                var item = await _translations.ReceiveAsync(cancellationToken);
                var translateModel = item.Item1;
                var tcs = item.Item2;
                var slug = Path.GetFileNameWithoutExtension(translateModel.OriginalFileName);
                if (translateModel.Persist)
                {
                    var blogService=scope.ServiceProvider
                        .GetRequiredService<IBlogService>();

                    var entryExists =await blogService.EntryExists(slug, translateModel.Language);
               var entryChanged = await blogService.EntryChanged(slug, MarkdownBaseService.EnglishLanguage, translateModel.OriginalMarkdown.ContentHash());
                    if (entryExists && !entryChanged) continue;
                }

        

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
                        var blogService = scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>();
                        postModel = await blogService.SavePost(slug, translateModel.Language,
                            translatedMarkdown);
                    }
                    else
                    {
                        var populatorService = scopeFactory.CreateScope().ServiceProvider
                            .GetRequiredService<MarkdownRenderingService>();
                        postModel = populatorService.GetPageFromMarkdown(translatedMarkdown, DateTime.Now, "");
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
        
        catch (OperationCanceledException)
        {
           logger.LogError("Translation service was cancelled");
        }
        catch (Exception e)
        {
            logger.LogError(e,"Error translating markdown");
        }
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await cancellationTokenSource.CancelAsync();
        logger.LogInformation("Background translation service stopped");
    }



}