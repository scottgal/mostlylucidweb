using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Mostlylucid.Relevance;

// One bounded queue for updates and reads keeps ordering explicit. No polling, timers or manual flush API.
/// <summary>Serializes accepted reads and updates through one bounded channel with asynchronous backpressure.</summary>
public sealed class RelevanceWorker : BackgroundService
{
    private readonly Channel<IWork> channel;

    /// <summary>Creates a queue with the given capacity; the host or standalone engine must start it.</summary>
    public RelevanceWorker(int capacity = 256)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        channel = Channel.CreateBounded<IWork>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>Waits for queue space, then for this operation's completion, rather than only acknowledging admission.</summary>
    /// <typeparam name="T">Operation result type.</typeparam>
    /// <param name="action">Asynchronous work executed by the single reader; do not enqueue and await recursively.</param>
    /// <param name="cancellationToken">Cancellation for admission and execution, passed to the work itself.</param>
    /// <returns>The operation result after it completes; exceptions propagate without terminating the reader.</returns>
    /// <remarks>Accepted work needs a running worker. Once admission closes, new operations are rejected.</remarks>
    public async ValueTask<T> EnqueueAsync<T>(Func<CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken = default)
    {
        var work = new Work<T>(action, cancellationToken);
        await channel.Writer.WriteAsync(work, cancellationToken);
        // Once accepted, completion reflects commit/cancellation of the operation, not just queue admission.
        return await work.Completion.Task;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // StopAsync completes admission. Drain accepted work before stopping the store.
        await foreach (var work in channel.Reader.ReadAllAsync()) await work.ExecuteAsync();
    }

    /// <summary>Closes admission and drains accepted work before completing normal shutdown.</summary>
    /// <remarks>A cancelled shutdown deadline stops waiting; individual operations must still finish or honor their own tokens.</remarks>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private interface IWork { ValueTask ExecuteAsync(); }
    private sealed class Work<T>(Func<CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken) : IWork
    {
        public TaskCompletionSource<T> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask ExecuteAsync()
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Completion.TrySetResult(await action(cancellationToken));
            }
            catch (OperationCanceledException ex) { Completion.TrySetCanceled(ex.CancellationToken); }
            catch (Exception ex) { Completion.TrySetException(ex); }
        }
    }
}
