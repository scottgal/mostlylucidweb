# Ummami.Net - UmmiBack Back FroundSender 测试单位测试 Ummi.Net - UmmiBackFroundSender

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在前一条款中,我们讨论了如何检验 `UmamiClient` 使用 x Unit 和 Moq 。 在本条中,我们将讨论如何检验 `UmamiBackgroundSender` 类。 缩略 `UmamiBackgroundSender` 与 `UmamiClient` 使用时 `IHostedService` 保持在背景中运行并发送请求 `UmamiClient` 完全脱离主执行线( 以免阻断执行) 。

和往常一样,你可以在我的GitHub上看到所有源代码 [在这里](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-009-003-09:00</datetime>

## `UmamiBackgroundSender`

实际结构的实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、实际结构、 `UmamiBackgroundSender` 很简单。 这是一个主机服务,一旦发现新的请求,即向Umami服务器发送请求。 基本结构 `UmamiBackgroundSender` 类别显示如下:

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

正如你可以看到的,这只是一个经典 `IHostedService` 使用ASP.NET的 `services.AddHostedService<UmamiBackgroundSender>()` 方法。 这踢开 `StartAsync` 当应用程序启动时使用的方法 。
内心的目光 `SendRequest` 方法就是魔法发生的地方。 这是我们从频道读到请求并发送到 Umami 服务器的地方。

这不包括发送请求的实际方法(见下文)。

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

所有这些真正真正做的就是 将请求包装到 `SendBackgroundPayload` 记录并发送至频道。

我们的巢穴接收回路 `SendRequest` 将不断从频道读取,直到它关闭。 这就是我们将集中进行试验努力的地方。

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

背景服务有一些语义, 使得它能够一到就发出讯息。
然而,这却引起了一个问题;如果我们没有从 `Send` 我们如何测试这实际上正在做什么?

## 测试测试 `UmamiBackgroundSender`

那么问题是,我们如何测试这个服务 5n? 实际测试没有反应?

答案是注射 `HttpMessageHandler` 我们发送到我们的 UmmiClient 。 这样我们就能拦截请求 检查内容

### EchoMockHttp 邮件Handler

你会记得上篇文章里 我们设置了一个模拟HttpMessageHandler 这活在 `EchoMockHandler` 静态类 :

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

你可以看到这里我们用莫克来设置 `SendAsync` 返回根据请求回复响应的方法(在 HttppClient 中,所有 Async 请求都通过 `SendAsync`).

你看,我们首先设置了莫克

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

然后,我们用魔术-- `Protected` 设置 `SendAsync` 方法。 这是因为 `SendAsync` 通常无法在公众的API `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

然后我们用全套的 `ItExpr.IsAny` 以匹配任何请求并回复来自 `responseFunc` 我们通过。

## 测试方法。

内心深处 `UmamiBackgroundSender_Tests` 类中,我们有一个共同的方法来定义所有测试方法。

### 设置设置设置设置设置设置设置

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

一旦我们界定了这一点,我们需要管理我们 `IHostedService` 在试验方法中:

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

你可以看到我们通过 处理器到我们的 `GetServices` 设置方法 :

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

在这里,我们通过我们的处理器 我们的服务,把它挂在 `UmamiClient` 设置 。

然后,我们加上: `UmamiBackgroundSender` 服务收藏和获取 `IHostedService` 由服务提供商提供。 然后把这个还给测试类 允许它使用

#### 终身服务

现在我们有了所有这些设置,我们可以简单地 `StartAsync` 主机服务,使用它,然后等待它停止:

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

这将启动主机服务, 发送请求, 等待回复, 然后停止服务 。

### 信件处理器

我们首先从建立 `EchoMockHandler` 和 `TaskCompletionSource` 它将发出信号 测试是完整的。 这一点很重要,可以将上下文返回到主测试线,以便我们能够正确捕捉失败和超时。

缩略 ` async (message, token) => {}` 是我们将上述功能传递给我们的模拟操作员。 我们可以在这里检查请求并回复回复(在这种情况下,我们真的不做任何事情)。

我们的 `EchoMockHandler.ResponseHandler` 是一种帮助者方法, 将请求体返回到我们的方法, 这样让我们可以确认信息正在传递到 `UmamiClient` 会 议 日 和 排 `HttpClient` 正确 。

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

然后我们抓住这个反应 并把它分解成 `EchoedRequest` 对象。 这是一个简单的对象, 代表我们发送到服务器的请求 。

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

你看,这个包封 `Type` 和 `Payload` 请求的日期。 这是我们在测试中要检查的。

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

关键是我们如何处理失败测试 因为我们不是这里需要使用的主要线条环境 `TaskCompletionSource` 将测试失败的信号反馈到主线。

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

这将设置例外 `TaskCompletionSource` 然后将500个错误返回到测试中。

# 在结论结论中

所以这是我第一个更详细的文章, `IHostedService` 这要求它这样做,因为它是一个相当复杂的测试 当它像这里一样 它不返回一个价值 呼叫者。