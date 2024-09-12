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
}