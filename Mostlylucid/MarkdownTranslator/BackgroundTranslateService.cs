using System.Diagnostics;
using System.Threading.Channels;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Helpers;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Shared.Helpers;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog.Events;

namespace Mostlylucid.MarkdownTranslator;

public class BackgroundTranslateService(
    MarkdownConfig markdownConfig,
    TranslateServiceConfig translateServiceConfig,
    IMarkdownTranslatorService markdownTranslatorService,
    IServiceScopeFactory scopeFactory,
    ILogger<IBackgroundTranslateService> logger) :  IBackgroundTranslateService
{
    private readonly
        Channel<(PageTranslationModel, TaskCompletionSource<TaskCompletion>)>
        _translations = Channel.CreateUnbounded<(PageTranslationModel, TaskCompletionSource<TaskCompletion>)>();

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private Task _healthCheckTask = Task.CompletedTask;

    public bool TranslationServiceUp { get; set; }
    private Task _sendTask = Task.CompletedTask;
    private Task _startTask = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startTask = Task.Run(() => StartChecks(cancellationToken));
        return Task.CompletedTask;
    }


    private async Task StartChecks(CancellationToken cancellationToken)
    {
        await StartupHealthCheck(cancellationToken);

        if (TranslationServiceUp)
        {
            _sendTask = TranslateFilesAsync(cancellationTokenSource.Token);
            if (translateServiceConfig.Enabled)
                await TranslateAllFilesAsync();
        }
        else
        {
            logger.LogError("Translation service is not available");
            _translations.Writer.Complete();
            await cancellationTokenSource.CancelAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cancel the token to signal the background task to stop
        await cancellationTokenSource.CancelAsync();
        _translations.Writer.Complete();
        // Wait until the background task completes or the cancellation token triggers
        await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task StartupHealthCheck(CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => !result) // Retry when Ping returns false (service not available)
            .WaitAndRetryAsync(10, // Retry 3 times
                attempt => TimeSpan.FromSeconds(10), // Wait 10 seconds between retries
                (result, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning("Translation service is not available, retrying attempt {RetryCount}",
                        retryCount);
                });

        try
        {
            var isUp = await retryPolicy.ExecuteAsync(async () => await Ping(cancellationToken));

            if (isUp)
            {
                logger.LogInformation("Translation service is available");
                TranslationServiceUp = true;
            }
            else
            {
                logger.LogError("Translation service is not available after retries");
                await HandleTranslationServiceFailure();
                TranslationServiceUp = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while checking the translation service availability");
            await HandleTranslationServiceFailure();
            TranslationServiceUp = false;
        }
    }

    private async Task HandleTranslationServiceFailure()
    {
        _translations.Writer.Complete();
        await cancellationTokenSource.CancelAsync();
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
        using var activity = Log.Logger.StartActivity("Translate to {Language} for File {FileName}",
            translateModel.Language,
            string.IsNullOrEmpty(translateModel.OriginalFileName) ? "No File" : translateModel.OriginalFileName);
        var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 3); // 3 retries with jittered delay
        var retryPolicy = Policy
            .Handle<TranslateException>()
            .WaitAndRetryAsync(
                delay,
                (exception, timeSpan, retryCount, context) =>
                {
                    if (retryCount >= 3)
                    {
                        activity?.Activity?.SetTag("Error", exception.Message);
                        tcs.SetException(new Exception("Translation failed available after 3 attempts"));
                        activity?.Complete(LogEventLevel.Error, exception);
                        logger.LogError(exception, "Translation failed after {RetryCount} attempts",
                            retryCount);
                    }
                    else
                    {
                        activity?.Activity?.SetTag("Retry Attempt for ", retryCount);
                        activity?.Activity?.SetTag("For language", translateModel.Language);
                        logger.LogWarning(exception,
                            "Translation error, retrying attempt {RetryCount}", retryCount);
                    }
                });


        if (string.IsNullOrEmpty(translateModel.OriginalMarkdown))
        {
            tcs.SetResult(new TaskCompletion(null, translateModel.OriginalMarkdown, translateModel.Language, true,
                DateTime.Now));
            activity?.Activity?.SetStatus(ActivityStatusCode.Ok, "No markdown to translate");
            activity?.Complete();
            return;
        }

        await retryPolicy.ExecuteAsync(async () =>
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
                    tcs.SetResult(new TaskCompletion(null, translateModel.OriginalMarkdown, translateModel.Language,
                        true, DateTime.Now));
                    return;
                }
            }

            logger.LogInformation("Translating {File} to {Language}", translateModel.OriginalFileName,
                translateModel.Language);

            try
            {
                if(!TranslationServiceUp)
                {
                    activity?.Activity?.SetTag("Error", "Translation service is not available");
                    throw new Exception("Translation service is not available");
                }
                var translatedMarkdown =
                    await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                        translateModel.Language, cancellationToken, activity.Activity);
                logger.LogInformation("Translated to {Language}", translateModel.Language);
                if (item.Item1.Persist)
                {
                    await PersistTranslation(scope, slug, translateModel, translatedMarkdown, activity);
                }

                activity?.Complete();
                tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.OriginalMarkdown,
                    translateModel.Language, true, DateTime.Now));
            }
            catch (Exception e) when (e is not TranslateException)
            {
                activity?.Activity?.SetTag("Error", e.Message);
                activity.Complete(LogEventLevel.Error, e);
                tcs.SetException(e);
                logger.LogError(e, "Error translating markdown");
                return;
            }
        });
    }

    private async Task<bool> EntryChanged(IServiceScope scope, string slug, PageTranslationModel translateModel)
    {
        var fileBlogService = scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>();
        var entryExists = await fileBlogService.EntryExists(slug, translateModel.Language);
        var entryChanged = await fileBlogService.EntryChanged(slug, translateModel.Language,
            translateModel.OriginalMarkdown.ContentHash());
        return !entryExists || entryChanged;
    }

    private async Task PersistTranslation(IServiceScope scope, string slug, PageTranslationModel translateModel,
        string translatedMarkdown, LoggerActivity? activity)
    {
        activity?.Activity?.SetTag("Persisting", slug);
        try
        {
            var blogService = translateServiceConfig.Mode == AutoTranslateMode.SaveToDisk
                ? scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>()
                : scope.ServiceProvider.GetRequiredService<IBlogViewService>();
            _ = await blogService.SavePost(slug, translateModel.Language,
                translatedMarkdown);
        }
        catch (Exception e)
        {
            activity?.Activity?.SetTag("Error", e.Message);
            throw;
        }
    }
}

public record TaskCompletion(
    string? TranslatedMarkdown,
    string OriginalMarkdown,
    string Language,
    bool Complete,
    DateTime? EndTime);