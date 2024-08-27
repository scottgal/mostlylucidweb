using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.Models;

namespace Umami.Net;

public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory,  UmamiClientSettings settings, ILogger<UmamiBackgroundSender> logger) : IHostedService
{

    private  Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

    private Task _sendTask = Task.CompletedTask;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope(); 
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var sendPayload = payloadService.PopulateFromPayload(settings.WebsiteId, payload, eventData);
        sendPayload.Url = url;
        sendPayload.Title = title;
        await _channel.Writer.WriteAsync(new SendBackgroundPayload("event", sendPayload));
        logger.LogInformation("Umami pageview event sent");
    }
    
    public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
    {
        await using var scope = scopeFactory.CreateAsyncScope(); 
        var payloadService = scope.ServiceProvider.GetRequiredService<PayloadService>();
        var sendPayload = payloadService.PopulateFromPayload(settings.WebsiteId, payload, eventData);
        await _channel.Writer.WriteAsync(new SendBackgroundPayload(eventType, sendPayload));
        logger.LogInformation("Umami background event sent");
    }

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
    

    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload, type:payload.EventType);

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
        }
    }

    public record SendBackgroundPayload(string EventType, UmamiPayload Payload);
}