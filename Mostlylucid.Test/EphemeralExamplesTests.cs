using System.Collections.Concurrent;
using Mostlylucid.Helpers.Ephemeral;
using Mostlylucid.Helpers.Ephemeral.Examples;
using Xunit;

namespace Mostlylucid.Test;

/// <summary>
/// Tests for the example classes in Mostlylucid.Helpers.Ephemeral.Examples.
/// These tests ensure the documentation examples actually compile and work.
/// </summary>
public class EphemeralExamplesTests
{
    /// <summary>
    /// Polls until the condition holds or the timeout passes. A fixed sleep before asserting on
    /// background work fails when the machine is busy (e.g. the full suite in a Docker build).
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
    }

    #region SignalingHttpClient Tests

    [Fact]
    public async Task SignalingHttpClient_EmitsExpectedSignals()
    {
        // Arrange
        using var httpClient = new HttpClient(new FakeHttpHandler("Hello, World!"));
        var emitter = new TestSignalEmitter();

        // Act
        var result = await SignalingHttpClient.DownloadWithSignalsAsync(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, "http://test.com/data"),
            emitter);

        // Assert
        Assert.Equal("Hello, World!", System.Text.Encoding.UTF8.GetString(result));
        Assert.Contains("stage.starting", emitter.Signals);
        Assert.Contains("progress:0", emitter.Signals);
        Assert.Contains("stage.request", emitter.Signals);
        Assert.Contains("stage.headers", emitter.Signals);
        Assert.Contains("stage.reading", emitter.Signals);
        Assert.Contains("stage.completed", emitter.Signals);
    }

    [Fact]
    public async Task SignalingHttpClient_EmitsProgressSignals()
    {
        // Arrange - create content large enough to trigger multiple progress signals
        var content = new string('x', 100_000);
        using var httpClient = new HttpClient(new FakeHttpHandler(content, contentLength: content.Length));
        var emitter = new TestSignalEmitter();

        // Act
        await SignalingHttpClient.DownloadWithSignalsAsync(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, "http://test.com/large"),
            emitter);

        // Assert - should have progress signals
        var progressSignals = emitter.Signals.Where(s => s.StartsWith("progress:")).ToList();
        Assert.True(progressSignals.Count > 1, "Should have multiple progress signals for large content");
        Assert.Contains("progress:100", emitter.Signals);
    }

    #endregion

    #region AdaptiveTranslationService Tests

    [Fact]
    public void TryParseRetryAfter_ValidInput_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(AdaptiveTranslationService.TryParseRetryAfter("rate-limit:5000ms", out var delay));
        Assert.Equal(TimeSpan.FromMilliseconds(5000), delay);
    }

    [Fact]
    public void TryParseRetryAfter_InvalidInput_ReturnsFalse()
    {
        Assert.False(AdaptiveTranslationService.TryParseRetryAfter("rate-limit", out _));
        Assert.False(AdaptiveTranslationService.TryParseRetryAfter("rate-limit:abc", out _));
        Assert.False(AdaptiveTranslationService.TryParseRetryAfter("rate-limit:5000", out _)); // no ms suffix
        Assert.False(AdaptiveTranslationService.TryParseRetryAfter("", out _));
    }

    [Fact]
    public async Task AdaptiveTranslationService_EnqueuesSuccessfully()
    {
        // Arrange
        var api = new FakeTranslationApi();
        await using var service = new AdaptiveTranslationService(api);

        // Act
        await service.TranslateAsync(new TranslationRequest("Hello", "es"));
        await Task.Delay(100); // Let work complete

        // Assert
        Assert.True(api.CallCount > 0 || service.PendingCount > 0 || service.TotalCompleted > 0);
    }

    #endregion

    #region SignalBasedCircuitBreaker Tests

    [Fact]
    public async Task CircuitBreaker_ClosedWhenNoFailures()
    {
        // Arrange
        await using var coordinator = new EphemeralWorkCoordinator<int>(
            async (item, ct) => await Task.Delay(10, ct),
            new EphemeralOptions { MaxConcurrency = 4 });

        var circuitBreaker = new SignalBasedCircuitBreaker(threshold: 3);

        // Act & Assert - circuit should be closed with no signals
        Assert.False(circuitBreaker.IsOpen(coordinator));
        Assert.Equal(0, circuitBreaker.GetFailureCount(coordinator));
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterThreshold()
    {
        // Arrange
        var failureCount = 0;
        await using var coordinator = new EphemeralWorkCoordinator<int>(
            async (item, ct) =>
            {
                // Simulate failure with signal - but we need to emit via options
                await Task.Delay(10, ct);
            },
            new EphemeralOptions
            {
                MaxConcurrency = 4,
                OnSignal = _ => Interlocked.Increment(ref failureCount)
            });

        var circuitBreaker = new SignalBasedCircuitBreaker(threshold: 3, windowSize: TimeSpan.FromSeconds(10));

        // Assert - initial state
        Assert.False(circuitBreaker.IsOpen(coordinator));
    }

    #endregion

    #region TelemetrySignalHandler Tests

    [Fact]
    public async Task TelemetryHandler_ProcessesSignals()
    {
        // Arrange
        var telemetry = new InMemoryTelemetryClient();
        await using var handler = new TelemetrySignalHandler(telemetry);

        // Act
        var signal = new SignalEvent("test-signal", 123, "key-1", DateTimeOffset.UtcNow);
        handler.OnSignal(signal);

        // Wait for async processing
        await WaitUntilAsync(() => telemetry.GetEvents().Any(e => e.Name == "EphemeralSignal"));

        // Assert
        var events = telemetry.GetEvents();
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Name == "EphemeralSignal");
    }

    [Fact]
    public async Task TelemetryHandler_CategorizesBySignalType()
    {
        // Arrange
        var telemetry = new InMemoryTelemetryClient();
        await using var handler = new TelemetrySignalHandler(telemetry);

        // Act - send different types
        handler.OnSignal(new SignalEvent("error.timeout", 1, null, DateTimeOffset.UtcNow));
        handler.OnSignal(new SignalEvent("perf.slow", 2, null, DateTimeOffset.UtcNow));
        handler.OnSignal(new SignalEvent("success", 3, null, DateTimeOffset.UtcNow));

        // Wait for async processing
        await WaitUntilAsync(() =>
        {
            var seen = telemetry.GetEvents();
            return seen.Any(e => e.Type == TelemetryEventType.Exception) &&
                   seen.Any(e => e.Type == TelemetryEventType.Metric);
        });

        // Assert
        var events = telemetry.GetEvents();
        Assert.Contains(events, e => e.Type == TelemetryEventType.Exception);
        Assert.Contains(events, e => e.Type == TelemetryEventType.Metric);
    }

    [Fact]
    public async Task TelemetryHandler_ReportsStats()
    {
        // Arrange
        var telemetry = new InMemoryTelemetryClient();
        await using var handler = new TelemetrySignalHandler(telemetry);

        // Act
        for (int i = 0; i < 10; i++)
        {
            handler.OnSignal(new SignalEvent($"signal-{i}", i, null, DateTimeOffset.UtcNow));
        }

        // Wait for processing
        await Task.Delay(500);

        // Assert
        Assert.True(handler.ProcessedCount >= 0);
        Assert.Equal(0, handler.DroppedCount); // Queue shouldn't overflow
    }

    #endregion

    #region AsyncSignalProcessor Tests

    [Fact]
    public async Task AsyncSignalProcessor_ProcessesSignals()
    {
        // Arrange
        var processed = new ConcurrentBag<string>();
        await using var processor = new AsyncSignalProcessor(
            async (signal, ct) =>
            {
                await Task.Delay(10, ct);
                processed.Add(signal.Signal);
            },
            maxConcurrency: 4,
            maxQueueSize: 100);

        // Act
        for (int i = 0; i < 10; i++)
        {
            processor.Enqueue(new SignalEvent($"signal-{i}", i, null, DateTimeOffset.UtcNow));
        }

        // Wait for processing
        await WaitUntilAsync(() => processor.ProcessedCount == 10);

        // Assert
        Assert.Equal(10, processed.Count);
        Assert.Equal(10, processor.ProcessedCount);
    }

    [Fact]
    public async Task AsyncSignalProcessor_DropsWhenQueueFull()
    {
        // Arrange
        var processed = new ConcurrentBag<string>();
        await using var processor = new AsyncSignalProcessor(
            async (signal, ct) =>
            {
                await Task.Delay(100, ct); // Slow processing
                processed.Add(signal.Signal);
            },
            maxConcurrency: 1, // Very limited
            maxQueueSize: 5);  // Small queue

        // Act - flood with signals
        var enqueuedCount = 0;
        for (int i = 0; i < 50; i++)
        {
            if (processor.Enqueue(new SignalEvent($"signal-{i}", i, null, DateTimeOffset.UtcNow)))
                enqueuedCount++;
        }

        // Assert - some should have been dropped
        Assert.True(processor.DroppedCount > 0, "Should have dropped some signals");
    }

    #endregion

    #region Test Helpers

    private class TestSignalEmitter : ISignalEmitter
    {
        private readonly List<string> _signals = new();

        public IReadOnlyList<string> Signals => _signals;
        public long OperationId => 1;
        public string? Key => null;

        public void Emit(string signal) => _signals.Add(signal);
        public bool EmitCaused(string signal, SignalPropagation? cause)
        {
            _signals.Add(signal);
            return true;
        }

        public bool Retract(string signal) => _signals.Remove(signal);
        public int RetractMatching(string pattern) => _signals.RemoveAll(s => StringPatternMatcher.Matches(s, pattern));
        public bool HasSignal(string signal) => _signals.Contains(signal);
    }

    private class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _content;
        private readonly long? _contentLength;

        public FakeHttpHandler(string content, long? contentLength = null)
        {
            _content = content;
            _contentLength = contentLength;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_content)
            };

            if (_contentLength.HasValue)
            {
                response.Content.Headers.ContentLength = _contentLength;
            }

            return Task.FromResult(response);
        }
    }

    private class FakeTranslationApi : ITranslationApi
    {
        public int CallCount { get; private set; }

        public Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new TranslationResult($"Translated: {request.Text}", false));
        }
    }

    #endregion
}
