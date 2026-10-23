using System.Threading.Channels;

namespace Mostlylucid.Relevance.Demo;

public sealed record DemoLog(DateTimeOffset At, string Source, string Message, int Status, double LatencyMs,
    double Retries, double QueueDepth, bool Synthetic = false, bool ResetWindow = false, bool Measure = true);
public sealed record SystemMetrics(double LatencyMs, double ErrorFraction, double Retries, double QueueDepth);
public sealed record OperationsSnapshot(string State, long Observations, SystemMetrics Metrics,
    RecommendationResult? Suggestions, DestinationPrediction? Prediction, IReadOnlyList<DemoLog> Logs);

public sealed class OperationsMonitor(DemoEngines engines, ILogger<OperationsMonitor> logger) : BackgroundService
{
    private sealed record Envelope(DemoLog Log, TaskCompletionSource? Completion);
    private readonly Channel<Envelope> channel = Channel.CreateBounded<Envelope>(new BoundedChannelOptions(256) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly Queue<DemoLog> window = new();
    private readonly Queue<DemoLog> history = new();
    private readonly object sync = new();
    private OperationsSnapshot snapshot = new("healthy", 0, new(0, 0, 0, 0), null, null, []);
    public OperationsSnapshot Snapshot { get { lock (sync) return snapshot; } }

    public async ValueTask PublishAsync(DemoLog entry, bool waitForProcessing = false, CancellationToken cancellationToken = default)
    {
        var completion = waitForProcessing ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) : null;
        await channel.Writer.WriteAsync(new Envelope(entry, completion), cancellationToken);
        if (completion is not null) await completion.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in channel.Reader.ReadAllAsync())
        {
            try { await ObserveAsync(envelope.Log); envelope.Completion?.TrySetResult(); }
            catch (Exception ex) { logger.LogError(ex, "Diagnostic log update failed"); envelope.Completion?.TrySetException(ex); }
        }
    }

    private async Task ObserveAsync(DemoLog entry)
    {
        history.Enqueue(entry);
        while (history.Count > 50) history.Dequeue();
        var previous = Snapshot;
        if (!entry.Measure) { lock (sync) snapshot = previous with { Logs = history.Reverse().ToArray() }; return; }
        if (entry.ResetWindow) window.Clear();
        window.Enqueue(entry);
        while (window.Count > 30) window.Dequeue();
        var metrics = new SystemMetrics(window.Average(l => l.LatencyMs), window.Count(l => l.Status >= 500 || l.Status == 429) / (double)window.Count,
            window.Average(l => l.Retries), window.Average(l => l.QueueDepth));
        var state = metrics.ErrorFraction >= 0.6 ? "outage"
            : metrics.LatencyMs > 300 || metrics.QueueDepth > 100 || metrics.Retries > 1 ? "degraded"
            : previous.State is "degraded" or "outage" or "recovered" ? "recovered" : "healthy";
        await engines.Operations.SaveAsync("logs", new { id = Guid.NewGuid().ToString(), subject = "demo-server", type = "measurement", state,
            latency_ms = metrics.LatencyMs, error_fraction = metrics.ErrorFraction, retries = metrics.Retries, queue_depth = metrics.QueueDepth });
        var suggestions = state is "degraded" or "outage" ? await engines.Operations.RecommendAsync("demo-server", "diagnostics", count: 3) : null;
        var prediction = await engines.Operations.PredictAsync("demo-server");
        lock (sync) snapshot = new(state, previous.Observations + 1, metrics, suggestions, prediction, history.Reverse().ToArray());
    }

    public async Task InjectAsync(string scenario, CancellationToken cancellationToken)
    {
        if (scenario is not ("traffic" or "database" or "queue" or "recovery")) throw new ArgumentException("Unknown sample incident.");
        for (var i = 0; i < 12; i++)
        {
            var (latency, retries, queue) = scenario switch
            {
                "traffic" => (450.0, 4.0, 220.0), "database" => (900.0, 0.3, 20.0),
                "queue" => (350.0, 1.0, 600.0), _ => (20.0, 0.0, 3.0)
            };
            var message = scenario switch
            {
                "traffic" => "Retry-heavy request burst; upstream queue growing",
                "database" => "Query duration elevated; connection wait observed",
                "queue" => "Consumer backlog increasing; check processing capacity",
                _ => "Recovery trace: normal latency and queue pressure"
            };
            await PublishAsync(new DemoLog(DateTimeOffset.UtcNow, scenario, message, scenario != "recovery" && i % 4 == 3 ? 503 : 200,
                latency, retries, queue, Synthetic: true, ResetWindow: i == 0), true, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        channel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
    }
}

public sealed record Intervention(string Id, DateTimeOffset At, string Action, SystemMetrics Before, string RecommendationId, string Message);

public sealed class DemoPolicy(OperationsMonitor monitor)
{
    private readonly object sync = new();
    private readonly Queue<DateTimeOffset> requests = new();
    private readonly List<Intervention> interventions = [];
    public int Limit { get; private set; } = 240;
    public long Rejected { get; private set; }
    public object View { get { lock (sync) return new { limit = Limit, rejected = Rejected, interventions = interventions.TakeLast(20).Reverse().ToArray() }; } }

    public bool Acquire()
    {
        lock (sync)
        {
            var now = DateTimeOffset.UtcNow;
            while (requests.TryPeek(out var time) && time < now.AddMinutes(-1)) requests.Dequeue();
            if (requests.Count >= Limit) { Rejected++; return false; }
            requests.Enqueue(now);
            return true;
        }
    }

    public async Task<object> ApplyAsync(string action, CancellationToken cancellationToken)
    {
        var observed = monitor.Snapshot;
        if (observed.Suggestions?.Items.All(i => i.Id != action) != false) throw new ArgumentException("Action is not in the current eligible recommendation set.");
        string message;
        lock (sync)
        {
            message = action switch
            {
                "tighten-rate-limit" => "Local shop request limit changed from 240 to 60 per minute. Operations controls remain available.",
                "inspect-database" => $"Inspection snapshot: mean latency {observed.Metrics.LatencyMs:F0}ms; compare this with database wait metrics.",
                "inspect-queue" => $"Inspection snapshot: reported queue {observed.Metrics.QueueDepth:F0}; verify consumer throughput before changing capacity.",
                _ => throw new ArgumentException("No registered handler for this action.")
            };
            if (action == "tighten-rate-limit") Limit = 60;
            interventions.Add(new Intervention(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, action, observed.Metrics, observed.Suggestions!.Id, message));
            if (interventions.Count > 20) interventions.RemoveAt(0);
        }
        await monitor.PublishAsync(new DemoLog(DateTimeOffset.UtcNow, "intervention", message, 200, 0, 0, 0, Measure: false), true, cancellationToken);
        return new { message, policy = View };
    }

    public void Reset() { lock (sync) { Limit = 240; requests.Clear(); } }
}
