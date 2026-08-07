using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Mostlylucid.MarkdownTranslator;

/// <summary>
/// Identifies a unit of translation work. Jobs sharing a key supersede one another: queueing a
/// translation for a post+language that is already pending cancels the older one.
///
/// This is what stops a re-saved post from being translated twice. Without it the queue holds a
/// snapshot of the markdown taken at enqueue time, so a stale snapshot could finish last and
/// overwrite the translation of the newer content.
/// </summary>
public readonly record struct TranslationJobKey(string Slug, string Language);

public sealed class TranslationJob : IDisposable
{
    public required TranslationJobKey Key { get; init; }
    public required PageTranslationModel Model { get; init; }
    public required long Version { get; init; }

    /// <summary>Cancelled when a newer job for the same key is queued.</summary>
    public CancellationTokenSource Cancellation { get; } = new();

    // RunContinuationsAsynchronously: without it, whatever awaits the translation resumes inline
    // on the translation worker thread and blocks the next job from starting.
    public TaskCompletionSource<TaskCompletion> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Dispose() => Cancellation.Dispose();
}

/// <summary>
/// Unbounded queue of translation work with supersede-by-key semantics.
/// </summary>
public sealed class TranslationJobQueue
{
    private readonly Channel<TranslationJob> _channel =
        Channel.CreateUnbounded<TranslationJob>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    private readonly ConcurrentDictionary<TranslationJobKey, TranslationJob> _pending = new();
    private long _version;

    public int PendingCount => _pending.Count;

    /// <summary>
    /// Queue a translation. If one is already pending or in flight for the same post+language it is
    /// cancelled and its awaiters are completed - the newer content wins.
    /// </summary>
    public TranslationJob Enqueue(PageTranslationModel model)
    {
        var key = BuildKey(model);
        var job = new TranslationJob
        {
            Key = key,
            Model = model,
            Version = Interlocked.Increment(ref _version)
        };

        var superseded = _pending.AddOrUpdate(key, job, (_, existing) =>
        {
            Supersede(existing);
            return job;
        });

        // AddOrUpdate returns the value now in the dictionary; if that is not our job another
        // thread raced us and won, so ours is already stale before it ever ran.
        if (!ReferenceEquals(superseded, job))
        {
            Supersede(job);
            return job;
        }

        // Unbounded channel: TryWrite only fails once the writer is completed (shutdown).
        if (!_channel.Writer.TryWrite(job))
        {
            _pending.TryRemove(new KeyValuePair<TranslationJobKey, TranslationJob>(key, job));
            Supersede(job);
        }

        return job;
    }

    public IAsyncEnumerable<TranslationJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Called by a worker before doing any real work. Returns false when a newer job for the same
    /// key has been queued since, in which case this job should be dropped untouched.
    /// </summary>
    public bool TryClaim(TranslationJob job) =>
        _pending.TryGetValue(job.Key, out var current) && ReferenceEquals(current, job);

    /// <summary>Release a finished job so later work for the same key is not treated as superseded.</summary>
    public void Release(TranslationJob job) =>
        _pending.TryRemove(new KeyValuePair<TranslationJobKey, TranslationJob>(job.Key, job));

    public void Complete()
    {
        _channel.Writer.TryComplete();
        foreach (var job in _pending.Values) Supersede(job);
        _pending.Clear();
    }

    private static void Supersede(TranslationJob job)
    {
        // Complete the awaiter rather than leaving it hanging - callers await these tasks.
        // Complete: false signals "no translation was produced for this request".
        job.Completion.TrySetResult(
            new TaskCompletion(null, job.Model.OriginalMarkdown, job.Model.Language, false, DateTime.Now));
        try
        {
            job.Cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Job already finished and disposed; nothing to cancel.
        }
    }

    private static TranslationJobKey BuildKey(PageTranslationModel model)
    {
        // Ad-hoc translations (the API endpoint) have no file behind them and are independent
        // requests, so give each a unique key - they must never cancel one another.
        if (!model.Persist || string.IsNullOrEmpty(model.OriginalFileName))
            return new TranslationJobKey(Guid.NewGuid().ToString("n"), model.Language);

        return new TranslationJobKey(
            Path.GetFileNameWithoutExtension(model.OriginalFileName), model.Language);
    }
}
