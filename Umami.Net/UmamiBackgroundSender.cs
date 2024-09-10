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

    private readonly Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

    private Task _sendTask = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sendTask = SendRequest(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("UmamiBackgroundSender is stopping.");

        // Signal cancellation and complete the channel
        await _cancellationTokenSource.CancelAsync();
        _channel.Writer.Complete();
        try
        {
            // Wait for the background task to complete processing any remaining items
            await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("StopAsync operation was canceled.");
        }
    }

    public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        sendPayload.UseDefaultUserAgent = useDefaultUserAgent;
        sendPayload.Url = url;
        sendPayload.Title = title;
        await _channel.Writer.WriteAsync(new SendBackgroundPayload("event", sendPayload));
        logger.LogInformation("Umami pageview event sent");
    }

    public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null,
        bool useDefaultUserAgent = false)
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
            UseDefaultUserAgent = useDefaultUserAgent
        };
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var payload = payloadService.PopulateFromPayload(thisPayload, eventData);
        await Send(payload, eventType: "identify");
    }

    public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null,
        bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var thisPayload = new UmamiPayload
        {
            SessionId = sessionId,
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
        var thisPayload = new UmamiPayload
        {
            Name = eventName,
            Data = eventData ?? new UmamiEventData(),
            UseDefaultUserAgent = useDefaultUserAgent
        };
        var payload = payloadService.PopulateFromPayload(thisPayload, eventData);
        await Send(payload);
        ;
    }

    public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event", bool useDefaultUserAgent = false)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        sendPayload.UseDefaultUserAgent = useDefaultUserAgent;
        await _channel.Writer.WriteAsync(new SendBackgroundPayload(eventType, sendPayload));
        logger.LogInformation("Umami background event sent");
    }


    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        while (_channel.Reader.TryRead(out var payload))
            try
            {
                using var scope = scopeFactory.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                // Send the event via the client
                await client.Send(payload.Payload, type: payload.EventType);

                logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
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

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
}