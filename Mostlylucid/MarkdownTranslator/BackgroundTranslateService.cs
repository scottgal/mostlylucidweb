using System.Threading.Channels;
using Mostlylucid.Blog;
using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;

namespace Mostlylucid.MarkdownTranslator;

public class BackgroundTranslateService(
    MarkdownConfig markdownConfig,
    TranslateServiceConfig translateServiceConfig,
    MarkdownTranslatorService markdownTranslatorService,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundTranslateService> logger) : IHostedService
{
    private readonly
        Channel<(PageTranslationModel, TaskCompletionSource<TaskCompletion>)>
        _translations = Channel.CreateUnbounded<(PageTranslationModel, TaskCompletionSource<TaskCompletion>)>();

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task _healthCheckTask = Task.CompletedTask;

    public bool TranslationServiceUp { get; set; }
    private Task _sendTask = Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!await StartupHealthCheck(cancellationToken))
        {
            TranslationServiceUp = false;
            logger.LogError("Translation service is not available");
        }
        TranslationServiceUp = true;
        _healthCheckTask = PeriodicHealthCheck(cancellationToken);

        _sendTask = TranslateFilesAsync(cancellationTokenSource.Token);
        if (translateServiceConfig.Enabled)
            await TranslateAllFilesAsync();
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _translations.Writer.Complete();
        await cancellationTokenSource.CancelAsync();
        logger.LogInformation("Background translation service stopped");
    }

    private async Task<bool> StartupHealthCheck(CancellationToken cancellationToken)
    {
        var count = 1;
        var isUp = false;
        while (true)
        {
            if (await Ping(cancellationToken))
            {
                logger.LogInformation("Translation service is available");
                isUp = true;
                break;
            }

            await Task.Delay(10000, cancellationToken);
            count++;
            if (count > 3)
            {
                logger.LogError("Translation service is not available trying again (count: {Count})", count);
                _translations.Writer.Complete();
                await cancellationTokenSource.CancelAsync();
                isUp = false;
                break;
            }
        }

        return isUp;
    }

    private async Task PeriodicHealthCheck(CancellationToken cancellationToken)
    {
        // Run the health check periodically (e.g., every 60 seconds)
        const int delayMilliseconds = 60000;


        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await Ping(cancellationToken))
                {
                    logger.LogError("Translation service is not available");
                    await cancellationTokenSource.CancelAsync();
                    _translations.Writer.Complete();
                    TranslationServiceUp = false;
                }
                else
                {
                    logger.LogInformation("Translation service is healthy");
                    TranslationServiceUp = true;
                }
            }
            catch (Exception ex)
            {
                TranslationServiceUp = false;
                logger.LogError(ex, "Error during service health check");
                await cancellationTokenSource.CancelAsync();
                _translations.Writer.Complete();
            }

            // Wait before checking again
            await Task.Delay(delayMilliseconds, cancellationToken);
        }
    }

    public async Task<bool> Ping(CancellationToken cancellationToken)
    {
        if (!await markdownTranslatorService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return false;
        }

        return true;
    }

    public async Task<Task<TaskCompletion>> Translate(MarkdownTranslationModel message)
    {
        // Create a TaskCompletionSource that will eventually hold the result of the translation
        var translateMessage = new PageTranslationModel
        {
            Language = message.Language,
            OriginalFileName = "",
            OriginalMarkdown = message.OriginalMarkdown,
            Persist = false
        };

        return await Translate(translateMessage);
    }

    private async Task<Task<TaskCompletion>> Translate(PageTranslationModel message)
    {
        // Create a TaskCompletionSource that will eventually hold the result of the translation
        var tcs = new TaskCompletionSource<TaskCompletion>();
        // Send the translation request along with the TaskCompletionSource to be processed
        await _translations.Writer.WriteAsync((message, tcs));
        return tcs.Task;
    }


    public async Task<List<Task<TaskCompletion>>> TranslateForAllLanguages(
        PageTranslationModel message)
    {
        var tasks = new List<Task<TaskCompletion>>();
        foreach (var language in translateServiceConfig.Languages)
        {
            var translateMessage = new PageTranslationModel
            {
                Language = language,
                OriginalFileName = message.OriginalFileName,
                OriginalMarkdown = message.OriginalMarkdown,
                Persist = message.Persist
            };
            var tcs = new TaskCompletionSource<TaskCompletion>();
            await _translations.Writer.WriteAsync((translateMessage, tcs));
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
                await TranslateForAllLanguages(new PageTranslationModel
                {
                    OriginalMarkdown = await File.ReadAllTextAsync(file),
                    OriginalFileName = file
                });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task TranslateFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processingTasks = new List<Task>();
            while (!cancellationToken.IsCancellationRequested)
            {
                while (processingTasks.Count < markdownTranslatorService.IPCount &&
                       !cancellationToken.IsCancellationRequested)
                {
                    var item = await _translations.Reader.ReadAsync(cancellationToken);
                    var translateModel = item.Item1;
                    var tcs = item.Item2;
                    // Start the task and add it to the list
                    var task = TranslateTask(cancellationToken, translateModel, item, tcs);
                    processingTasks.Add(task);
                }

                // Wait for any of the tasks to complete
                var completedTask = await Task.WhenAny(processingTasks);

                // Remove the completed task
                processingTasks.Remove(completedTask);

                // Optionally handle the result of the completedTask here
                try
                {
                    await completedTask; // Catch exceptions if needed
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error translating markdown");
                }
            }
        }

        catch (OperationCanceledException)
        {
            logger.LogError("Translation service was cancelled");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown");
        }
    }

    private async Task TranslateTask(CancellationToken cancellationToken, PageTranslationModel translateModel,
        (PageTranslationModel, TaskCompletionSource<TaskCompletion>) item,
        TaskCompletionSource<TaskCompletion> tcs)
    {
        var scope = scopeFactory.CreateScope();

        var slug = Path.GetFileNameWithoutExtension(translateModel.OriginalFileName);
        if (translateModel.Persist)
        {
            if (await EntryChanged(scope, slug, translateModel))
            {
                logger.LogInformation("Entry {Slug} has changed, translating", slug);
            }
            else
            {
                logger.LogInformation("Entry {Slug} has not changed, skipping translation", slug);
                tcs.SetResult(new TaskCompletion(null, translateModel.OriginalMarkdown, translateModel.Language, true, DateTime.Now));
                return;
            }
        }


        logger.LogInformation("Translating {File} to {Language}", translateModel.OriginalFileName,
            translateModel.Language);
        try
        {
            var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);


            if (item.Item1.Persist)
            {
                await PersistTranslation(scope, slug, translateModel, translatedMarkdown);
            }
            
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.OriginalMarkdown, translateModel.Language, true, DateTime.Now));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating {File} to {Language}", translateModel.OriginalFileName,
                translateModel.Language);
            
            tcs.SetException(e);
        }
    }

    private async  Task<bool> EntryChanged(IServiceScope scope, string slug, PageTranslationModel translateModel)
    {
        var fileBlogService = scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>();
        var entryExists = await fileBlogService.EntryExists(slug, translateModel.Language);
        var entryChanged = await fileBlogService.EntryChanged(slug, translateModel.Language,
            translateModel.OriginalMarkdown.ContentHash());
        return !entryExists || entryChanged;
    }
    
    private async Task PersistTranslation(IServiceScope scope,string slug, PageTranslationModel translateModel, string translatedMarkdown)
    {
        var blogService = translateServiceConfig.Mode == AutoTranslateMode.SaveToDisk
            ? scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>()
            : scope.ServiceProvider.GetRequiredService<IBlogService>(); 
        _ = await blogService.SavePost(slug, translateModel.Language,
            translatedMarkdown);
    }
}

public record TaskCompletion(string? TranslatedMarkdown, string OriginalMarkdown, string Language, bool Complete, DateTime? EndTime);