# Unit Testing Umami.Net - Testing UmamiBackgroundSender

# Introduction
In the previous article, we discussed how to test the `UmamiClient` using xUnit and Moq. In this article, we will discuss how to test the `UmamiBackgroundSender` class. The `UmamiBackgroundSender` is a bit different to `UmamiClient` as it uses `IHostedService` to stay running in the background and send requests through `UmamiClient` completely out of the main executing thread (so it doesn't block execution).

As usual you can see all the source code for this on my GitHub [here](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).


[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

The actual structure of `UmamiBackgroundSender` is quite simple. It is a hosted service that sends requests to the Umami server as soon as it detects a new request. The basic structure `UmamiBackgroundSender` class is shown below:

```csharp
public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory, ILogger<UmamiBackgroundSender> logger) : IHostedService
{

    private  Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

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

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
    
    }

```
As you can see this is just a classic `IHostedService` it's added to our service collection in ASP.NET using the `services.AddHostedService<UmamiBackgroundSender>()` method. This kicks off the `StartAsync` method when the application starts. 
The look inside the `SendRequest` method is where the magic happens. This is where we read from the channel and send the request to the Umami server.

This excludes the actual methods to send the requests (shown below). 

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

All these really do is package the request up into the `SendBackgroundPayload` record and send it to the channel. 

Our nested receive loop in `SendRequest` will keep reading from the channel until it is closed. This is where we will focus our testing efforts.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```
The background service has some semantics which allow it to just fire off the message as soon as it arrives. 
However this raises a problem; if we don't get a returned value from the `Send` how do we test this is actually doing anything?


## Testing `UmamiBackgroundSender`
So then the question is how DO we test this service fiven there's no response to actually test against?

The answer is to inject an `HttpMessageHandler` to the mocked HttpClient we send into our UmamiClient. This will allow us to intercept the request and check it's contents.

### EchoMockHttpMessageHandler
You'll recall from the previous article we set up a mock HttpMessageHandler. This lives inside the `EchoMockHandler` static class:

```csharp
public static class EchoMockHandler
{
    public static HttpMessageHandler Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFunc)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                responseFunc(request, cancellationToken).Result);

        return mockHandler.Object;
    }
```

You can see here we use Mock to set up a `SendAsync` method which will return a response based on the request (in HttpClient all async requests are done through `SendAsync`).

You see we first setup the Mock
```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```
We then use the magic of `Protected` to set up the `SendAsync` method. This is because `SendAsync` isn't normally accessible in the public API of `HttpMessageHandler`. 

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```    

We then just use the catch-all `ItExpr.IsAny` to match any request and return the response from the `responseFunc` we pass in.

## Test methods.
Inside the `UmamiBackgroundSender_Tests` class we have a common way to define all the test methods.

### Setup
```csharp
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
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

Once we have this defined we need to manage our `IHostedService` lifetime in the test method:

```csharp
       var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```
You can see we pass in the handler to our `GetServices` setup method:

```csharp
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
```
Here we pass in our handler to our services to hook it in the the `UmamiClient` setup. 

We then add the `UmamiBackgroundSender` to the service collection and get the `IHostedService` from the service provider. Then return this to the test class to allow it's use.

#### Hosted Service Lifetime
Now that we have all these set up we can simply `StartAsync` the Hosted Service, use it then wait until it stops:

```csharp
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
```

This will start the hosted service, send the request, wait for the response then stop the service.

### Message Handler
We first start by setting up the `EchoMockHandler` and the `TaskCompletionSource` which will signal the test is complete. This is important to return the context to the main test thread so we can correctly capture failures and timeouts.

The ``` async (message, token) =>
        {}``` is the function we pass into our mock handler we mentioned above. In here we can check the request and return a response (which in this case we really don't do anything with).

Our `EchoMockHandler.ResponseHandler` is a helper method that will return the request body back to our method, this lets us verify the message is passing through the `UmamiClient` to the `HttpClient` correctly.

```csharp
    public static async Task<HttpResponseMessage> ResponseHandler(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Read the request content
        var requestBody = request.Content?.ReadAsStringAsync(cancellationToken).Result;
        // Create a response that echoes the request body
        var responseContent = requestBody ?? "No request body";
        // Return the response
        return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
```

We then grab this response and deserialize it into a `EchoedRequest` object. This is a simple object that represents the request we sent to the server.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```
You see this encapsulates the `Type` and `Payload` of the request. This is what we will check against in our test.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

What's critical here is how we handle failing tests, as we're not in the main thread context here we need to use `TaskCompletionSource` to signal back to the main thread that the test has failed. 

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

This will set the exception on the `TaskCompletionSource` and return a 500 error to the test.

# In Conclusion
So that's the first of my rather more detailed posts, `IHostedService` warrants this as it's rather complex to test when like here it doesn't return a value to the caller. 