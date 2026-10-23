using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mostlylucid.Relevance.Sqlite;

namespace Mostlylucid.Relevance.Tests;

public sealed class WorkerTests
{
    [Fact]
    public async Task SaveAndReadWaitForTheBackgroundConversionAndCommit()
    {
        var definition = RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml")));
        definition.Points["price"].Converter = "blocking";
        var converter = new BlockingConverter();
        await using var engine = new RelevanceEngine(definition, new MemoryRelevanceStore(), [converter]);
        var save = engine.SaveAsync("products", new { id = "a", category = "outdoors", price = 100, available = true }).AsTask();
        await converter.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(save.IsCompleted);
        var read = engine.RecommendAsync("s", "home").AsTask();
        Assert.False(read.IsCompleted);
        converter.Release.TrySetResult();
        await save.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("a", Assert.Single((await read).Items).Id);
    }

    [Fact]
    public async Task BoundedChannelDrainsAcceptedWorkOnShutdownAndRejectsNewWork()
    {
        using var worker = new RelevanceWorker(1);
        await worker.StartAsync(CancellationToken.None);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<int>();
        var first = worker.EnqueueAsync(async _ => { entered.TrySetResult(); await release.Task; order.Add(1); return 1; }).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = worker.EnqueueAsync(_ => { order.Add(2); return ValueTask.FromResult(2); }).AsTask();
        var stopped = worker.StopAsync(CancellationToken.None);
        Assert.False(stopped.IsCompleted);
        release.TrySetResult();
        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
        await stopped.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { 1, 2 }, order);
        await Assert.ThrowsAsync<ChannelClosedException>(async () => await worker.EnqueueAsync(_ => ValueTask.FromResult(3)));
    }

    [Fact]
    public async Task CancellationBeforeAdmissionDoesNotSaveAnything()
    {
        await using var engine = new RelevanceEngine(RelevanceDefinition.FromYaml(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml"))), new MemoryRelevanceStore());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await engine.SaveAsync("products",
            new { id = "a", category = "outdoors", price = 100, available = true }, cancelled.Token));
        Assert.Empty((await engine.RecommendAsync("s", "home")).Items);
    }

    [Fact]
    public async Task ExistingHostOwnsTheSameWorkerUsedByTheEngine()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddRelevanceSqlite(Path.Combine(AppContext.BaseDirectory, "relevance.yaml"),
            Path.Combine(Path.GetTempPath(), $"relevance-host-{Guid.NewGuid():N}.db"));
        using var host = builder.Build();
        await host.StartAsync();
        var engine = host.Services.GetRequiredService<RelevanceEngine>();
        await engine.SaveAsync("products", new { id = "a", category = "outdoors", price = 100, available = true });
        Assert.Equal("a", Assert.Single((await engine.RecommendAsync("s", "home")).Items).Id);
        await host.StopAsync();
        await Assert.ThrowsAsync<ChannelClosedException>(async () => await engine.RecommendAsync("s", "home"));
    }

    [Fact]
    public async Task RecommendationSnapshotsNestedFilterListsBeforeQueueing()
    {
        using var worker = new RelevanceWorker();
        await worker.StartAsync(CancellationToken.None);
        await using var engine = new RelevanceEngine(RelevanceDefinition.FromYaml(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "relevance.yaml"))), new MemoryRelevanceStore(), worker: worker);
        await engine.SaveAsync("products", new { id = "a", category = "outdoors", price = 100, available = true });
        await engine.SaveAsync("products", new { id = "b", category = "electronics", price = 100, available = true });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocker = worker.EnqueueAsync(async _ => { entered.TrySetResult(); await release.Task; return 0; }).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var children = new List<Filter> { Filter.Eq("category", "outdoors") };
            var read = engine.RecommendAsync("s", "home", Filter.All(new Conjunction(children))).AsTask();
            children[0] = Filter.Eq("category", "electronics");
            release.TrySetResult();
            Assert.Equal("a", Assert.Single((await read.WaitAsync(TimeSpan.FromSeconds(5))).Items).Id);
        }
        finally
        {
            release.TrySetResult();
            await blocker;
            // The test owns this external worker and must drain it before the engine disposes its store.
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private sealed class BlockingConverter : IPointConverter
    {
        public string Name => "blocking";
        public string Version => "1";
        public int Dimensions(PointDefinition point) => 1;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async ValueTask<float[]> ConvertAsync(JsonElement value, PointDefinition point, CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return [(float)(value.GetDouble() / point.Scale)];
        }
    }
}
