using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.Config;
using Umami.Net.Helpers;

namespace Umami.Net.Test;

public class BackgroundDeliveryTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private static ServiceProvider Services(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor();
        services.AddSingleton(new UmamiClientSettings { WebsiteId = Guid.NewGuid().ToString() });
        services.AddSingleton<JwtDecoder>(); services.AddScoped<PayloadService>();
        services.AddSingleton<UmamiBackgroundSender>();
        services.AddHttpClient<UmamiClient>(http => http.BaseAddress = new("https://example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(send));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ShutdownDrainsAlreadyQueuedEvents()
    {
        var received = 0;
        using var services = Services(async (_, token) =>
        {
            await Task.Delay(10, token); Interlocked.Increment(ref received);
            return new(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var sender = services.GetRequiredService<UmamiBackgroundSender>();
        await sender.Track("first"); await sender.Track("second"); await sender.Track("third");
        await sender.StartAsync(CancellationToken.None);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await sender.StopAsync(deadline.Token);
        Assert.Equal(3, received);
    }

    [Fact]
    public async Task ShutdownDeadlineCancelsAnInFlightSend()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var services = Services(async (_, token) =>
        {
            started.SetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            finally { stopped.SetResult(); }
            return new(HttpStatusCode.OK);
        });
        var sender = services.GetRequiredService<UmamiBackgroundSender>();
        await sender.StartAsync(CancellationToken.None); await sender.Track("waiting");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await sender.StopAsync(deadline.Token);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
