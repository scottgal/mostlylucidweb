using System.Diagnostics;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Helpers;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Config.Markdown;
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
    ILogger<IBackgroundTranslateService> logger) : IBackgroundTranslateService
{
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(1);

    private readonly TranslationJobQueue _queue = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();

    private Task _healthCheckTask = Task.CompletedTask;
    private Task _startTask = Task.CompletedTask;
    private Task _workerTask = Task.CompletedTask;

    public bool TranslationServiceUp { get; set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startTask = Task.Run(() => StartChecks(cancellationTokenSource.Token), cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();
        await cancellationTokenSource.CancelAsync();

        // Give the workers a moment to unwind, but never hang shutdown on them.
        await Task.WhenAny(
            Task.WhenAll(_workerTask, _healthCheckTask),
            Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None));
    }

    private async Task StartChecks(CancellationToken cancellationToken)
    {
        logger.LogInformation("BackgroundTranslateService starting - Enabled: {Enabled}, ForceRetranslation: {Force}",
            translateServiceConfig.Enabled, translateServiceConfig.ForceRetranslation);

        // Workers start regardless of service health. Work queued while the translator is down
        // waits in the queue instead of being dropped, and drains once it comes back.
        _workerTask = RunWorkersAsync(cancellationToken);
        _healthCheckTask = MonitorHealthAsync(cancellationToken);

        await StartupHealthCheck(cancellationToken);

        if (!TranslationServiceUp)
        {
            logger.LogError(
                "Translation service unavailable at startup; will keep retrying every {Interval}", HealthCheckInterval);
            return;
        }

        logger.LogInformation("Translation service is UP");

        if (translateServiceConfig.Enabled)
        {
            logger.LogInformation("Translation service enabled - starting TranslateAllFilesAsync");
            await TranslateAllFilesAsync();
        }
        else
        {
            logger.LogWarning("Translation service is UP but Enabled=false - skipping startup translation");
        }
    }

    private async Task StartupHealthCheck(CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => !result)
            .WaitAndRetryAsync(10,
                attempt => TimeSpan.FromSeconds(10),
                (result, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning("Translation service is not available, retrying attempt {RetryCount}",
                        retryCount);
                });

        try
        {
            TranslationServiceUp = await retryPolicy.ExecuteAsync(() => Ping(cancellationToken));
            if (TranslationServiceUp) logger.LogInformation("Translation service is available");
            else logger.LogError("Translation service is not available after retries");
        }
        catch (OperationCanceledException)
        {
            TranslationServiceUp = false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while checking the translation service availability");
            TranslationServiceUp = false;
        }
    }

    /// <summary>
    /// Keeps re-checking the translator so a restart of it recovers on its own. Previously a
    /// translator that was down at startup killed translation until the whole app was restarted.
    /// </summary>
    private async Task MonitorHealthAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(HealthCheckInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var wasUp = TranslationServiceUp;
                var isUp = await Ping(cancellationToken);
                TranslationServiceUp = isUp;

                if (isUp && !wasUp)
                {
                    logger.LogInformation("Translation service recovered; queued work will resume");
                    if (translateServiceConfig.Enabled) await TranslateAllFilesAsync();
                }
                else if (!isUp && wasUp)
                {
                    logger.LogWarning("Translation service went down; work will queue until it returns");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception e)
        {
            logger.LogError(e, "Translation health monitor stopped unexpectedly");
        }
    }

    public async Task<bool> Ping(CancellationToken cancellationToken)
    {
        try
        {
            return await markdownTranslatorService.IsServiceUp(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Translation service ping failed");
            return false;
        }
    }

    public Task<Task<TaskCompletion>> Translate(MarkdownTranslationModel message)
    {
        var job = _queue.Enqueue(new PageTranslationModel
        {
            Language = message.Language,
            OriginalFileName = "",
            OriginalMarkdown = message.OriginalMarkdown,
            Persist = false
        });

        return Task.FromResult(job.Completion.Task);
    }

    public Task<List<Task<TaskCompletion>>> TranslateForAllLanguages(PageTranslationModel message)
    {
        var tasks = translateServiceConfig.Languages
            .Select(language => _queue.Enqueue(new PageTranslationModel
            {
                Language = language,
                OriginalFileName = message.OriginalFileName,
                OriginalMarkdown = message.OriginalMarkdown,
                Persist = message.Persist
            }).Completion.Task)
            .ToList();

        return Task.FromResult(tasks);
    }

    public async Task TranslateAllFilesAsync()
    {
        try
        {
            var allMarkdownFiles = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");
            var markdownFiles = allMarkdownFiles.Where(IsEnglishSourceFile).ToArray();

            logger.LogInformation(
                "Found {Count} English source files to translate (filtered from {Total} total .md files)",
                markdownFiles.Length, allMarkdownFiles.Length);
            logger.LogInformation("Configured languages: {Languages}",
                string.Join(", ", translateServiceConfig.Languages));

            LogMissingTranslationsSummary(markdownFiles);

            foreach (var file in markdownFiles)
                TranslateForAllLanguages(new PageTranslationModel
                {
                    OriginalMarkdown = await File.ReadAllTextAsync(file),
                    OriginalFileName = file,
                    Persist = true
                });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error in translation batch");
            throw;
        }
    }

    /// <summary>
    /// Determines if a file is an English source file (not an already-translated file).
    /// English source files are named {slug}.md, while translated files are {slug}.{language}.md
    /// </summary>
    private bool IsEnglishSourceFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        foreach (var language in translateServiceConfig.Languages)
            if (fileName.EndsWith($".{language}", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Skipping translated file: {File} (detected language: {Language})", filePath, language);
                return false;
            }

        return true;
    }

    /// <summary>
    /// Logs how much work startup is about to queue. Reads the translated directory once rather
    /// than probing for every (file, language) pair.
    /// </summary>
    private void LogMissingTranslationsSummary(string[] markdownFiles)
    {
        var translatedPath = markdownConfig.MarkdownTranslatedPath;
        var existing = Directory.Exists(translatedPath)
            ? Directory.EnumerateFiles(translatedPath, "*.md")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var totalMissing = 0;
        foreach (var language in translateServiceConfig.Languages)
        {
            var missing = markdownFiles.Count(file =>
                !existing.Contains($"{Path.GetFileNameWithoutExtension(file)}.{language}"));

            if (missing > 0)
            {
                logger.LogInformation("Language {Language}: {Count} missing translations will be queued",
                    language, missing);
                totalMissing += missing;
            }
        }

        if (totalMissing > 0) logger.LogInformation("Total translations to process: {Total}", totalMissing);
        else logger.LogInformation("All translations are up to date");
    }

    /// <summary>
    /// Fixed-size worker pool draining the queue.
    ///
    /// The previous implementation reaped tasks with Task.WhenAny over a list it only topped up by
    /// blocking on ReadAsync, and on cancellation could call Task.WhenAny on an empty list - which
    /// throws, was swallowed, and permanently killed the loop. Independent workers have no such
    /// shared state.
    /// </summary>
    private async Task RunWorkersAsync(CancellationToken cancellationToken)
    {
        var workerCount = Math.Max(1, markdownTranslatorService.IPCount);
        logger.LogInformation("Starting {Count} translation worker(s)", workerCount);

        var workers = Enumerable.Range(0, workerCount)
            .Select(i => Task.Run(() => WorkerLoopAsync(i, cancellationToken), cancellationToken));

        await Task.WhenAll(workers);
    }

    private async Task WorkerLoopAsync(int workerId, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var job in _queue.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await ProcessJobAsync(job, cancellationToken);
                }
                catch (Exception e)
                {
                    // A single bad job must never take the worker down.
                    logger.LogError(e, "Unhandled error processing translation for {Language}", job.Model.Language);
                    job.Completion.TrySetException(e);
                }
                finally
                {
                    _queue.Release(job);
                    job.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Translation worker {WorkerId} stopping", workerId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Translation worker {WorkerId} stopped unexpectedly", workerId);
        }
    }

    private async Task ProcessJobAsync(TranslationJob job, CancellationToken cancellationToken)
    {
        var translateModel = job.Model;

        if (!_queue.TryClaim(job))
        {
            logger.LogDebug("Skipping superseded translation of {Slug} to {Language}",
                job.Key.Slug, translateModel.Language);
            return;
        }

        if (string.IsNullOrEmpty(translateModel.OriginalMarkdown))
        {
            job.Completion.TrySetResult(new TaskCompletion(
                null, translateModel.OriginalMarkdown, translateModel.Language, true, DateTime.Now));
            return;
        }

        using var linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, job.Cancellation.Token);
        var token = linked.Token;

        using var activity = Log.Logger.StartActivity("Translate to {Language} for File {FileName}",
            translateModel.Language,
            string.IsNullOrEmpty(translateModel.OriginalFileName) ? "No File" : translateModel.OriginalFileName);

        var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 3);
        var retryPolicy = Policy
            .Handle<TranslateException>()
            .WaitAndRetryAsync(delay, (exception, timeSpan, retryCount, context) =>
            {
                activity?.Activity?.SetTag("Retry Attempt", retryCount);
                activity?.Activity?.SetTag("For language", translateModel.Language);
                logger.LogDebug(exception, "Translation error, retrying attempt {RetryCount}/3", retryCount);
            });

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                token.ThrowIfCancellationRequested();

                using var scope = scopeFactory.CreateScope();
                var slug = job.Key.Slug;

                if (translateModel.Persist && !await EntryChanged(scope, slug, translateModel))
                {
                    logger.LogInformation("Entry {Slug} has not changed, skipping translation", slug);
                    job.Completion.TrySetResult(new TaskCompletion(
                        null, translateModel.OriginalMarkdown, translateModel.Language, true, DateTime.Now));
                    return;
                }

                if (!TranslationServiceUp)
                {
                    activity?.Activity?.SetTag("Error", "Translation service is not available");
                    throw new TranslateException("Translation service is not available", Array.Empty<string>());
                }

                logger.LogInformation("Translating {File} to {Language}",
                    translateModel.OriginalFileName, translateModel.Language);

                var translatedMarkdown = await markdownTranslatorService.TranslateMarkdown(
                    translateModel.OriginalMarkdown, translateModel.Language, token, activity.Activity);
                logger.LogInformation("Translated to {Language}", translateModel.Language);

                // Re-check before writing: a newer version of this post may have been queued while
                // we were translating, and its result must not be clobbered by ours.
                if (!_queue.TryClaim(job))
                {
                    logger.LogInformation("Discarding superseded translation of {Slug} to {Language}",
                        slug, translateModel.Language);
                    return;
                }

                if (translateModel.Persist)
                    await PersistTranslation(scope, slug, translateModel, translatedMarkdown, activity);

                activity?.Complete();
                job.Completion.TrySetResult(new TaskCompletion(
                    translatedMarkdown, translateModel.OriginalMarkdown, translateModel.Language, true,
                    DateTime.Now));
            });
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            logger.LogInformation("Translation of {Slug} to {Language} cancelled - superseded by newer content",
                job.Key.Slug, translateModel.Language);
            activity?.Complete();
        }
        catch (OperationCanceledException)
        {
            activity?.Complete();
            throw;
        }
        catch (TranslateException e)
        {
            activity?.Activity?.SetTag("Error", e.Message);
            activity?.Complete(LogEventLevel.Error, e);
            job.Completion.TrySetException(new Exception($"Translation failed after 3 retries: {e.Message}"));
            logger.LogError(e, "Translation failed after 3 retries for {Language}", translateModel.Language);
        }
        catch (Exception e)
        {
            activity?.Activity?.SetTag("Error", e.Message);
            activity?.Complete(LogEventLevel.Error, e);
            job.Completion.TrySetException(e);
            logger.LogError(e, "Unexpected error translating to {Language}", translateModel.Language);
        }
    }

    private async Task<bool> EntryChanged(IServiceScope scope, string slug, PageTranslationModel translateModel)
    {
        if (translateServiceConfig.ForceRetranslation)
        {
            logger.LogInformation("ForceRetranslation is enabled, retranslating {Slug} to {Language}",
                slug, translateModel.Language);
            return true;
        }

        var fileBlogService = scope.ServiceProvider.GetRequiredService<IMarkdownFileBlogService>();
        var entryExists = await fileBlogService.EntryExists(slug, translateModel.Language);
        var entryChanged = await fileBlogService.EntryChanged(slug, translateModel.Language,
            translateModel.OriginalMarkdown.ContentHash());

        logger.LogDebug("Entry {Slug} ({Language}) - Exists: {Exists}, Changed: {Changed}",
            slug, translateModel.Language, entryExists, entryChanged);

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
            _ = await blogService.SavePost(slug, translateModel.Language, translatedMarkdown);

            var vectorStoreService = scope.ServiceProvider.GetService<IVectorStoreService>();
            if (vectorStoreService != null)
            {
                await vectorStoreService.AddLanguageAsync(slug, translateModel.Language);
                logger.LogDebug("Added language {Language} to Qdrant for {Slug}", translateModel.Language, slug);
            }
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
    DateTime? EndTime = null);
