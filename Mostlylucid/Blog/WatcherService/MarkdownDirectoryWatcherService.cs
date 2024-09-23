using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Config.Markdown;
using Polly;
using Serilog.Events;

namespace Mostlylucid.Blog.WatcherService;

public class MarkdownDirectoryWatcherService(
    MarkdownConfig markdownConfig,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MarkdownDirectoryWatcherService> logger)
    : IHostedService
{
    private Task _awaitChangeTask = Task.CompletedTask;
    private FileSystemWatcher _fileSystemWatcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _fileSystemWatcher = new FileSystemWatcher
        {
            Path = markdownConfig.MarkdownPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime |
                           NotifyFilters.Size,
            Filter = "*.md", // Watch all markdown files
            IncludeSubdirectories = true // Enable watching subdirectories
        };
        // Subscribe to events
        _fileSystemWatcher.EnableRaisingEvents = true;

        _awaitChangeTask = Task.Run(() => AwaitChanges(cancellationToken));
        logger.LogInformation("Started watching directory {Directory}", markdownConfig.MarkdownPath);


        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop watching
        _fileSystemWatcher.EnableRaisingEvents = false;
        _fileSystemWatcher.Dispose();

        Console.WriteLine($"Stopped watching directory: {markdownConfig.MarkdownPath}");

        return Task.CompletedTask;
    }

    private async Task AwaitChanges(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var fileEvent = _fileSystemWatcher.WaitForChanged(WatcherChangeTypes.All);
            if (fileEvent.ChangeType == WatcherChangeTypes.Changed ||
                fileEvent.ChangeType == WatcherChangeTypes.Created)
            {
                await OnChangedAsync(fileEvent);
            }
            else if (fileEvent.ChangeType == WatcherChangeTypes.Deleted)
            {
                OnDeleted(fileEvent);
            }
            else if (fileEvent.ChangeType == WatcherChangeTypes.Renamed)
            {
            }
        }
    }

    private async Task OnChangedAsync(WaitForChangedResult e)
    {
        if (e.Name == null) return;

        using var activity = Log.Logger.StartActivity("Markdown File Changed {Name}", e.Name);
        var retryPolicy = Policy
            .Handle<IOException>() // Only handle IO exceptions (like file in use)
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt),
                (exception, timeSpan, retryCount, context) =>
                {
                    activity?.Activity?.SetTag("Retry Attempt", retryCount);
                    // Log the retry attempt
                    logger.LogWarning("File is in use, retrying attempt {RetryCount} after {TimeSpan}", retryCount,
                        timeSpan);
                });

        try
        {
            var fileName = e.Name;
            var isTranslated = Path.GetFileNameWithoutExtension(e.Name).Contains(".");
            var language = MarkdownBaseService.EnglishLanguage;
            var directory = markdownConfig.MarkdownPath;

            if (isTranslated)
            {
                language = Path.GetFileNameWithoutExtension(e.Name).Split('.').Last();
                fileName = Path.GetFileName(fileName);
                directory = markdownConfig.MarkdownTranslatedPath;
            }

            var filePath = Path.Combine(directory, fileName);
            var scope = serviceScopeFactory.CreateScope();
            var markdownBlogService = scope.ServiceProvider.GetRequiredService<IMarkdownBlogService>();

            // Use the Polly retry policy for executing the operation
            await retryPolicy.ExecuteAsync(async () =>
            {
                var blogModel = await markdownBlogService.GetPage(filePath);
                activity?.Activity?.SetTag("Page Processed", blogModel.Slug);
                blogModel.Language = language;

                var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                await blogService.SavePost(blogModel);
                if (language == MarkdownBaseService.EnglishLanguage)
                {
                    var translateService = scope.ServiceProvider.GetRequiredService<IBackgroundTranslateService>();
                    await translateService.TranslateForAllLanguages(
                        new PageTranslationModel()
                            { OriginalFileName = filePath, OriginalMarkdown = blogModel.Markdown, Persist = true });
                }

                activity?.Activity?.SetTag("Page Saved", blogModel.Slug);
            });

            activity?.Complete();
        }
        catch (Exception exception)
        {
            activity?.Complete(LogEventLevel.Error, exception);
        }
    }

    private void OnDeleted(WaitForChangedResult e)
    {
        if (e.Name == null) return;
        var activity = Log.Logger.StartActivity("Markdown File Deleting {Name}", e.Name);
        try
        {
            var isTranslated = Path.GetFileNameWithoutExtension(e.Name).Contains(".");
            var language = MarkdownBaseService.EnglishLanguage;
            var slug = Path.GetFileNameWithoutExtension(e.Name);
            if (isTranslated)
            {
                var name = Path.GetFileNameWithoutExtension(e.Name).Split('.');
                language = name.Last();
                slug = name.First();
            }
            else
            {
                var translatedFiles = Directory.GetFiles(markdownConfig.MarkdownTranslatedPath, $"{slug}.*.*");
                _fileSystemWatcher.EnableRaisingEvents = false;
                foreach (var file in translatedFiles)
                {
                    File.Delete(file);
                }

                _fileSystemWatcher.EnableRaisingEvents = true;
            }

            var scope = serviceScopeFactory.CreateScope();
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogViewService>();
            blogService.Delete(slug, language);
            activity?.Activity?.SetTag("Page Deleted", slug);
            activity?.Complete();
            logger.LogInformation("Deleted blog post {Slug} in {Language}", slug, language);
        }
        catch (Exception exception)
        {
            activity?.Complete(LogEventLevel.Error, exception);
            logger.LogError("Error deleting blog post {Slug}", e.Name);
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"File renamed: {e.OldFullPath} to {e.FullPath}");
    }
}