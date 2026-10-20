using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umami.Net.Models;

namespace Umami.Net;

public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory, ILogger<UmamiBackgroundSender> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Channel<SendBackgroundPayload> _channel = Channel.CreateBounded<SendBackgroundPayload>(new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    private Task _sendTask = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sendTask = SendRequest(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("UmamiBackgroundSender is stopping.");

        _channel.Writer.TryComplete();
        try { await _sendTask.WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _cancellationTokenSource.CancelAsync();
            logger.LogWarning("Umami background delivery did not drain before the shutdown deadline");
        }
    }


    public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        payload ??= new UmamiPayload();
        payload.UseDefaultUserAgent |= useDefaultUserAgent;
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        sendPayload.Url = url;
        sendPayload.Title = title;
        await _channel.Writer.WriteAsync(new SendBackgroundPayload("event", sendPayload));
        logger.LogInformation("Umami pageview event sent");
    }

    public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null,
        bool useDefaultUserAgent = false, string? distinctId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        eventData ??= new UmamiEventData();
        if (!string.IsNullOrEmpty(email))
            eventData.TryAdd("email", email);
        if (!string.IsNullOrEmpty(username))
            eventData.TryAdd("username", username);
        if (!string.IsNullOrEmpty(userId))
            eventData.TryAdd("userId", userId);


        var thisPayload = new UmamiPayload
        {
            Data = eventData,
            SessionId = sessionId,
            DistinctId = distinctId,
            UseDefaultUserAgent = useDefaultUserAgent
        };
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var payload = payloadService.PopulateFromPayload(thisPayload, eventData);
        await Send(payload, eventType: "identify");
    }

    public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null,
        bool useDefaultUserAgent = false, string? distinctId = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var thisPayload = new UmamiPayload
        {
            SessionId = sessionId,
            DistinctId = distinctId,
            Data = eventData ?? new UmamiEventData(),
            UseDefaultUserAgent = useDefaultUserAgent
        };
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var payload = payloadService.PopulateFromPayload(thisPayload, eventData);
        await Send(payload, eventType: "identify");
    }

    public async Task Track(string eventName, UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var payload = payloadService.PopulateFromPayload(null, data: eventData);
       payload.Name = eventName;
       payload.UseDefaultUserAgent = useDefaultUserAgent;
        await Send(payload);
        
    }

    public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event", bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        payload ??= new UmamiPayload();
        payload.UseDefaultUserAgent |= useDefaultUserAgent;
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        await _channel.Writer.WriteAsync(new SendBackgroundPayload(eventType, sendPayload));
        logger.LogInformation("Umami background event sent");
    }


    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await WaitForWorkAsync(token))
        while (_channel.Reader.TryRead(out var payload))
            try
            {
                using var scope = scopeFactory.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                // Send the event via the client
                using var response = await client.SendAsync(payload.Payload, type: payload.EventType, cancellationToken: token);

                if (response.IsSuccessStatusCode) logger.LogDebug("Umami background event delivered: {EventType}", payload.EventType);
                else logger.LogWarning("Umami background delivery failed: {StatusCode}", response.StatusCode);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Umami background delivery canceled.");
                return; // Exit the loop on cancellation
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending Umami background event.");
            }
    }

    private async Task<bool> WaitForWorkAsync(CancellationToken token)
    {
        try { return await _channel.Reader.WaitToReadAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
    }

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
}