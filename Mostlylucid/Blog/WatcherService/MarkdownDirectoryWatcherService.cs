using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Middleware;
using Mostlylucid.SemanticSearch.Models;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Config.Markdown;
using Mostlylucid.Shared.Models;
using Mostlylucid.Shared.Services;
using Polly;
using Serilog.Events;

namespace Mostlylucid.Blog.WatcherService;

/// <summary>
/// Watches the markdown directory and syncs changes into the blog.
///
/// Events are captured by handlers and coalesced per file, then processed after a quiet period.
/// The previous implementation polled with FileSystemWatcher.WaitForChanged, which listens for a
/// single event at a time - anything that happened while a file was being processed was lost, so
/// uploading several posts at once would only import some of them.
/// </summary>
public class MarkdownDirectoryWatcherService(
    MarkdownConfig markdownConfig,
    IServiceScopeFactory serviceScopeFactory,
    IStartupCoordinator startupCoordinator,
    IMemoryCache memoryCache,
    IOutputCacheStore outputCacheStore,
    ILogger<MarkdownDirectoryWatcherService> logger)
    : IHostedService
{
    /// <summary>
    /// How long a file must be quiet before we act on it. Editors and uploads commonly write in
    /// several passes; without this we would import a half-written post and keep it.
    /// </summary>
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(750);

    private readonly ConcurrentDictionary<string, PendingChange> _pending = new(StringComparer.OrdinalIgnoreCase);

    // Capacity-1 drop-write channel used purely as a "something changed" signal.
    private readonly Channel<byte> _signal = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private readonly CancellationTokenSource _cts = new();

    private FileSystemWatcher? _fileSystemWatcher;
    private Task _processingTask = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _fileSystemWatcher = new FileSystemWatcher
        {
            Path = markdownConfig.MarkdownPath,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime |
                           NotifyFilters.Size,
            Filter = "*.md",
            IncludeSubdirectories = true,
            // Default is 8KB. Bulk uploads can overflow it, and an overflow drops events wholesale.
            InternalBufferSize = 64 * 1024
        };

        _fileSystemWatcher.Created += OnFileSystemEvent;
        _fileSystemWatcher.Changed += OnFileSystemEvent;
        _fileSystemWatcher.Deleted += OnFileSystemEvent;
        _fileSystemWatcher.Renamed += OnFileRenamed;
        _fileSystemWatcher.Error += OnWatcherError;
        _fileSystemWatcher.EnableRaisingEvents = true;

        _processingTask = Task.Run(() => ProcessLoopAsync(_cts.Token), _cts.Token);
        logger.LogInformation("Started watching directory {Directory}", markdownConfig.MarkdownPath);

        startupCoordinator.SignalReady(StartupServiceNames.MarkdownDirectoryWatcher);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_fileSystemWatcher != null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Created -= OnFileSystemEvent;
            _fileSystemWatcher.Changed -= OnFileSystemEvent;
            _fileSystemWatcher.Deleted -= OnFileSystemEvent;
            _fileSystemWatcher.Renamed -= OnFileRenamed;
            _fileSystemWatcher.Error -= OnWatcherError;
            _fileSystemWatcher.Dispose();
        }

        await _cts.CancelAsync();
        await Task.WhenAny(_processingTask, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None));

        logger.LogInformation("Stopped watching directory: {Path}", markdownConfig.MarkdownPath);
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e) => Queue(e.Name, e.ChangeType, null);

    private void OnFileRenamed(object sender, RenamedEventArgs e) =>
        Queue(e.Name, WatcherChangeTypes.Renamed, e.OldName);

    private void OnWatcherError(object sender, ErrorEventArgs e) =>
        // Usually an internal buffer overflow. BlogReconciliationService is the backstop that
        // resyncs disk and database, so surface this loudly rather than failing silently.
        logger.LogError(e.GetException(), "File watcher error - some markdown changes may have been missed");

    private void Queue(string? name, WatcherChangeTypes changeType, string? oldName)
    {
        if (string.IsNullOrEmpty(name)) return;

        _pending.AddOrUpdate(
            name,
            _ => new PendingChange(changeType, oldName),
            // Latest event wins, but never lose the original name of a rename.
            (_, existing) => new PendingChange(changeType, oldName ?? existing.OldName));

        _signal.Writer.TryWrite(0);
    }

    private async Task ProcessLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _signal.Reader.ReadAsync(cancellationToken);

                // Wait for the directory to go quiet, so a burst of writes is handled once.
                while (true)
                {
                    await Task.Delay(DebounceInterval, cancellationToken);
                    if (!_signal.Reader.TryRead(out _)) break;
                }

                await ProcessBatchAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception e)
        {
            logger.LogError(e, "Markdown watcher processing loop stopped unexpectedly");
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var batch = new List<KeyValuePair<string, PendingChange>>();
        foreach (var key in _pending.Keys.ToArray())
            if (_pending.TryRemove(key, out var change))
                batch.Add(new KeyValuePair<string, PendingChange>(key, change));

        if (batch.Count == 0) return;

        logger.LogDebug("Processing {Count} coalesced markdown change(s)", batch.Count);

        var touched = false;
        foreach (var (name, change) in batch)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                switch (change.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        await OnDeletedAsync(name);
                        touched = true;
                        break;
                    case WatcherChangeTypes.Renamed:
                        if (!string.IsNullOrEmpty(change.OldName)) await OnDeletedAsync(change.OldName);
                        await OnChangedAsync(name);
                        touched = true;
                        break;
                    default:
                        touched |= await OnChangedAsync(name);
                        break;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing markdown change for {Name}", name);
            }
        }

        if (!touched) return;

        // One eviction for the whole batch. Previously every translated file written for a post
        // flushed the entire blog output cache, so a single post cost ~15 full flushes.
        BrokenLinkArchiveMiddleware.InvalidateLinkCaches(memoryCache);
        await outputCacheStore.EvictByTagAsync("blog", CancellationToken.None);
        logger.LogDebug("Invalidated broken link cache and OutputCache after {Count} change(s)", batch.Count);
    }

    private async Task<bool> OnChangedAsync(string name)
    {
        using var activity = Log.Logger.StartActivity("Markdown File Changed {Name}", name);

        var isTranslated = Path.GetFileNameWithoutExtension(name).Contains('.');
        var language = MarkdownBaseService.EnglishLanguage;
        var directory = markdownConfig.MarkdownPath;
        var fileName = name;

        if (isTranslated)
        {
            language = Path.GetFileNameWithoutExtension(name).Split('.').Last();
            fileName = Path.GetFileName(name);
            directory = markdownConfig.MarkdownTranslatedPath;
        }

        var filePath = Path.Combine(directory, fileName);
        if (!File.Exists(filePath))
        {
            logger.LogDebug("Skipping {Name} - no longer on disk", name);
            activity?.Complete();
            return false;
        }

        var retryPolicy = Policy
            .Handle<IOException>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt),
                (exception, timeSpan, retryCount, context) =>
                {
                    activity?.Activity?.SetTag("Retry Attempt", retryCount);
                    logger.LogWarning("File is in use, retrying attempt {RetryCount} after {TimeSpan}",
                        retryCount, timeSpan);
                });

        try
        {
            using var scope = serviceScopeFactory.CreateScope();

            await retryPolicy.ExecuteAsync(async () =>
            {
                var blogService = scope.ServiceProvider.GetRequiredService<IBlogService>();
                var markdown = await File.ReadAllTextAsync(filePath);

                var slug = Path.GetFileNameWithoutExtension(fileName);
                if (isTranslated) slug = slug.Split('.').First();

                var savedModel = await blogService.SavePost(slug, language, markdown);
                activity?.Activity?.SetTag("Page Processed", savedModel.Slug);
                activity?.Activity?.SetTag("Page Saved", savedModel.Slug);

                var isRootDirectory = !name.Contains(Path.DirectorySeparatorChar) &&
                                      !name.Contains(Path.AltDirectorySeparatorChar);
                if (isRootDirectory || isTranslated)
                    await IndexPostForSemanticSearchAsync(scope, savedModel, language);

                if (language == MarkdownBaseService.EnglishLanguage && !string.IsNullOrEmpty(savedModel.Markdown))
                {
                    var translateService = scope.ServiceProvider.GetRequiredService<IBackgroundTranslateService>();
                    await translateService.TranslateForAllLanguages(new PageTranslationModel
                    {
                        OriginalFileName = filePath,
                        OriginalMarkdown = savedModel.Markdown,
                        Persist = true
                    });
                }
            });

            activity?.Complete();
            return true;
        }
        catch (Exception exception)
        {
            activity?.Complete(LogEventLevel.Error, exception);
            logger.LogError(exception, "Error processing changed markdown file {Name}", name);
            return false;
        }
    }

    private async Task OnDeletedAsync(string name)
    {
        using var activity = Log.Logger.StartActivity("Markdown File Deleting {Name}", name);
        try
        {
            var isTranslated = Path.GetFileNameWithoutExtension(name).Contains('.');
            var language = MarkdownBaseService.EnglishLanguage;
            var slug = Path.GetFileNameWithoutExtension(name);

            if (isTranslated)
            {
                var parts = slug.Split('.');
                language = parts.Last();
                slug = parts.First();
            }
            else if (Directory.Exists(markdownConfig.MarkdownTranslatedPath))
            {
                // Removing the English source removes its translations too. The resulting delete
                // events are handled normally - the watcher is never blinded, which previously
                // meant unrelated edits during this window were lost.
                foreach (var file in Directory.GetFiles(markdownConfig.MarkdownTranslatedPath, $"{slug}.*.md"))
                    File.Delete(file);
            }

            using var scope = serviceScopeFactory.CreateScope();
            var blogService = scope.ServiceProvider.GetRequiredService<IBlogViewService>();
            await blogService.Delete(slug, language);

            if (!name.Contains(Path.DirectorySeparatorChar) && !name.Contains(Path.AltDirectorySeparatorChar))
                await DeletePostFromSemanticSearchAsync(scope, slug, language);

            activity?.Activity?.SetTag("Page Deleted", slug);
            activity?.Complete();
            logger.LogInformation("Deleted blog post {Slug} in {Language}", slug, language);
        }
        catch (Exception exception)
        {
            activity?.Complete(LogEventLevel.Error, exception);
            logger.LogError(exception, "Error deleting blog post {Slug}", name);
        }
    }

    /// <summary>
    /// Index a blog post in the semantic search index
    /// </summary>
    private async Task IndexPostForSemanticSearchAsync(IServiceScope scope, BlogPostDto post, string language)
    {
        try
        {
            var semanticSearchService = scope.ServiceProvider.GetService<ISemanticSearchService>();
            if (semanticSearchService == null) return;

            // Only index English posts for semantic search
            if (language != MarkdownBaseService.EnglishLanguage)
            {
                // For translated posts, update the languages array on the existing document
                await UpdateLanguagesInSemanticSearchAsync(scope, post.Slug);
                return;
            }

            var document = new BlogPostDocument
            {
                Id = post.Slug,
                Slug = post.Slug,
                Title = post.Title,
                Content = post.PlainTextContent,
                PublishedDate = post.PublishedDate,
                Languages = GetAvailableLanguages(post.Slug),
                Categories = post.Categories
            };

            await semanticSearchService.IndexPostAsync(document);
            logger.LogInformation("Indexed post {Slug} in semantic search", post.Slug);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index post {Slug} ({Language}) in semantic search", post.Slug, language);
        }
    }

    /// <summary>
    /// Update the languages array in semantic search when a translation is added
    /// </summary>
    private async Task UpdateLanguagesInSemanticSearchAsync(IServiceScope scope, string slug)
    {
        try
        {
            var vectorStoreService = scope.ServiceProvider.GetService<IVectorStoreService>();
            if (vectorStoreService == null) return;

            var languages = GetAvailableLanguages(slug);
            await vectorStoreService.UpdateLanguagesAsync(slug, languages);
            logger.LogDebug("Updated languages for {Slug} in semantic search: {Languages}", slug,
                string.Join(", ", languages));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update languages for {Slug} in semantic search", slug);
        }
    }

    /// <summary>
    /// Get all available languages for a given post slug by scanning the translated directory
    /// </summary>
    private string[] GetAvailableLanguages(string slug)
    {
        var languages = new List<string> { "en" }; // English is always available (the source)

        var translatedPath = markdownConfig.MarkdownTranslatedPath;
        if (!Directory.Exists(translatedPath)) return languages.ToArray();

        var translatedFiles = Directory.GetFiles(translatedPath, $"{slug}.*.md", SearchOption.TopDirectoryOnly);

        foreach (var file in translatedFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('.');
            if (parts.Length >= 2)
            {
                var langCode = parts[^1];
                if (langCode.Length == 2 && langCode != "en") languages.Add(langCode);
            }
        }

        return languages.OrderBy(l => l).ToArray();
    }

    /// <summary>
    /// Remove a blog post from the semantic search index
    /// </summary>
    private async Task DeletePostFromSemanticSearchAsync(IServiceScope scope, string slug, string language)
    {
        try
        {
            var semanticSearchService = scope.ServiceProvider.GetService<ISemanticSearchService>();
            if (semanticSearchService == null) return;

            await semanticSearchService.DeletePostAsync(slug);
            logger.LogInformation("Deleted post {Slug} from semantic search index", slug);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete post {Slug} from semantic search", slug);
        }
    }

    private readonly record struct PendingChange(WatcherChangeTypes ChangeType, string? OldName);
}
