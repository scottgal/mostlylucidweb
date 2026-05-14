using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umami.Net.Models;
using Umami.Net.Test.Extensions;
using Umami.Net.Test.MessageHandlers;

namespace Umami.Net.Test;

public class UmamiBackgroundSender_Tests
{
    private (UmamiBackgroundSender, IHostedService) GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        services.AddScoped<UmamiBackgroundSender>();
        services.AddScoped<IHostedService, UmamiBackgroundSender>(provider =>
            provider.GetRequiredService<UmamiBackgroundSender>());
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var backgroundSender = serviceProvider.GetRequiredService<UmamiBackgroundSender>();
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        return (backgroundSender, hostedService);
    }

    [Fact]
    public async Task Track_Page_View()
    {
        var page = "https://background.com";
        var title = "Background Example Page";
        var tcs = new TaskCompletionSource<bool>();
        // Arrange
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Result.Content.ReadFromJsonAsync<EchoedRequest>(token);
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(page, jsonContent.Payload.Url);
                Assert.Equal(title, jsonContent.Payload.Title);
                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Track_Event()
    {
        // Arrange
        var eventName = "Background Event";
        var key = "My Test Key";
        var value = "My Test Value";

        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(eventName, jsonContent.Payload.Name);
                var data = jsonContent.Payload.Data.First();
                Assert.Equal(key, data.Key);
                Assert.Equal(value, data.Value.ToString());

                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.Track(eventName, new UmamiEventData { { key, value } });

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;

        await backgroundSender.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Send_Event()
    {
        // Arrange
        var eventName = "Background Event";
        var key = "My Test Key";
        var value = "My Test Value";

        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);
                // Assertions
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(eventName, jsonContent.Payload.Name);
                var data = jsonContent.Payload.Data.First();
                Assert.Equal(key, data.Key);
                Assert.Equal(value, data.Value.ToString());

                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.Send(new UmamiPayload
            { Name = eventName, Data = new UmamiEventData { { key, value } } });

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }
    
    
    [Fact]
    public async Task Track_PageView_DefaultUserAgent()
    {
        // Arrange
        var pageName = "RSS";
        var pageTitle = "RSS Feed";
        var tcs = new TaskCompletionSource<bool>();
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                responseContent.Headers.TryGetValues("User-Agent", out var userAgent);
                
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);
                // Assertions
                Assert.Equal(PayloadService.DefaultUserAgent, userAgent?.First());
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.NotNull(jsonContent.Payload);
                Assert.Equal(pageName, jsonContent.Payload.Url);
                Assert.Equal(pageTitle, jsonContent.Payload.Title);
                Assert.NotNull(jsonContent.Payload.Data); 
                var originalUserAgent = jsonContent.Payload.Data["OriginalUserAgent"];
                Assert.Equal(Consts.UserAgent, originalUserAgent.ToString());

                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(pageName,title:pageTitle, useDefaultUserAgent: true);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }
    
        [Fact]
    public async Task Track_Event_DefaultUserAgent()
    {
        // Arrange
        var eventName = "RSS";
        var tcs = new TaskCompletionSource<bool>();
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                responseContent.Headers.TryGetValues("User-Agent", out var userAgent);
                
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);
                // Assertions
                Assert.Equal(PayloadService.DefaultUserAgent, userAgent?.First());
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.NotNull(jsonContent.Payload);
                Assert.Equal(eventName, jsonContent.Payload.Name);
                Assert.NotNull(jsonContent.Payload.Data); 
                var originalUserAgent = jsonContent.Payload.Data["OriginalUserAgent"];
                Assert.Equal(Consts.UserAgent, originalUserAgent.ToString());

                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.Track(eventName,useDefaultUserAgent: true);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }

    /// <summary>
    /// Verifies that Identify correctly sends user identification data to Umami.
    /// Tests email, username, userId, and sessionId parameters.
    /// </summary>
    [Fact]
    public async Task Identify_User()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var userId = "user123";
        var sessionId = "session123";
        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);

                // Assertions
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.NotNull(jsonContent.Payload.Data);
                Assert.Equal(email, jsonContent.Payload.Data["email"].ToString());
                Assert.Equal(username, jsonContent.Payload.Data["username"].ToString());
                Assert.Equal(userId, jsonContent.Payload.Data["userId"].ToString());
                Assert.Equal(sessionId, jsonContent.Payload.SessionId);
                Assert.Equal("identify", jsonContent.Type);

                tcs.SetResult(true);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);

        await backgroundSender.Identify(email, username, sessionId, userId);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }

    /// <summary>
    /// Verifies that Identify merges custom event data with user identification data.
    /// Ensures additional metadata can be attached to user identification events.
    /// </summary>
    [Fact]
    public async Task Identify_User_WithEventData()
    {
        // Arrange
        var email = "test@example.com";
        var customKey = "custom_field";
        var customValue = "custom_value";
        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);

                // Assertions
                Assert.NotNull(jsonContent);
                Assert.NotNull(jsonContent.Payload.Data);
                Assert.Equal(email, jsonContent.Payload.Data["email"].ToString());
                Assert.Equal(customValue, jsonContent.Payload.Data[customKey].ToString());

                tcs.SetResult(true);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);

        await backgroundSender.Identify(email, eventData: new UmamiEventData { { customKey, customValue } });

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }

    /// <summary>
    /// Verifies that IdentifySession correctly identifies a session with custom data.
    /// Tests that session ID and event data are properly sent as "identify" event type.
    /// </summary>
    [Fact]
    public async Task IdentifySession()
    {
        // Arrange
        var sessionId = "session123";
        var key = "session_data";
        var value = "session_value";
        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = await EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Content.ReadFromJsonAsync<EchoedRequest>(token);

                // Assertions
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(sessionId, jsonContent.Payload.SessionId);
                Assert.NotNull(jsonContent.Payload.Data);
                Assert.Equal(value, jsonContent.Payload.Data[key].ToString());
                Assert.Equal("identify", jsonContent.Type);

                tcs.SetResult(true);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);

        await backgroundSender.IdentifySession(sessionId, eventData: new UmamiEventData { { key, value } });

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
    }

    /// <summary>
    /// Verifies that the background sender recovers gracefully from exceptions.
    /// Ensures one failed event doesn't crash the worker loop - subsequent events should still process.
    /// This validates the "fail loudly but recoverably" philosophy.
    /// </summary>
    [Fact]
    public async Task BackgroundSender_HandlesException_DoesNotCrash()
    {
        // Arrange
        var eventName = "Test Event";
        var sendAttempts = 0;
        var tcs = new TaskCompletionSource<bool>();

        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            sendAttempts++;

            if (sendAttempts == 1)
            {
                // First attempt throws exception
                throw new HttpRequestException("Simulated network error");
            }

            // Second attempt succeeds
            var responseContent = await EchoMockHandler.ResponseHandler(message, token);
            tcs.SetResult(true);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);

        // Send two events - first should fail, second should succeed
        await backgroundSender.Track(eventName);
        await Task.Delay(100); // Give time for first to fail
        await backgroundSender.Track(eventName);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000, cancellationToken));
        if (completedTask != tcs.Task) throw new TimeoutException("The background task did not complete in time.");

        await tcs.Task;
        Assert.Equal(2, sendAttempts);
    }

    /// <summary>
    /// Verifies that StopAsync performs graceful shutdown by processing all queued events.
    /// Ensures no events are lost when the application shuts down.
    /// Tests the sidecar service's clean shutdown behavior.
    /// </summary>
    [Fact]
    public async Task StopAsync_DrainsQueue_GracefulShutdown()
    {
        // Arrange
        var eventsProcessed = 0;
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            Interlocked.Increment(ref eventsProcessed);
            var responseContent = await EchoMockHandler.ResponseHandler(message, token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent.Content };
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        await hostedService.StartAsync(CancellationToken.None);

        // Queue multiple events
        await backgroundSender.Track("event1");
        await backgroundSender.Track("event2");
        await backgroundSender.Track("event3");

        // Stop and allow processing
        await backgroundSender.StopAsync(CancellationToken.None);

        // Verify all events were processed before shutdown
        Assert.Equal(3, eventsProcessed);
    }
}