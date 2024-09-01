# Umami.Net-UmamiClient测试

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

现在我有了 [Umami.Net软件包](https://www.nuget.org/packages/Umami.Net/) 我当然想确保一切如预期的顺利进行。 要做到这一点,最好的办法是对所有方法和班级进行某种程度的全面测试。 单位测试就在这里进行
注意:这不是一个“完美处理方式”类型的文章, 这只是我目前所做的。 在现实中,我并不真的需要 嘲笑 `IHttpMessageHandler` 在这里您可以攻击一位女主人汉德勒 到正常的 HttpClient 这样做。 我只是想展示你如何用莫克做它。

[技选委

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-009-001T17:22</datetime>

# 单位测试

单位测试是指单个编码单位的测试过程,以确保它们如预期的那样工作。 这项工作是通过写作测试完成的,这种测试以控制的方式将方法和类称为方法和类,然后按预期检查输出结果。

Umami. Net 这样的软件包很难解决,因为它都叫一个远程客户 `HttpClient` 并且有一个 `IHostedService` 它用来使新事件数据的发送尽可能无缝。

## UmamiClient测试

试验的主要部分a `HttpClient` 基础库正在避免实际的“ HttpClient” 呼叫 。 这是通过创建 `HttpClient` 使用 a `HttpMessageHandler` 返回已知响应。 这是通过创建 `HttpClient` 与 a 具有 `HttpMessageHandler` 返回已知的响应; 在此情况下, 我只是回回回输入响应, 检查输入响应没有被 `UmamiClient`.

```csharp
    public static HttpMessageHandler Create()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("api/send")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                // Read the request content
                var requestBody = request.Content != null
                    ? request.Content.ReadAsStringAsync(cancellationToken).Result
                    : null;

                // Create a response that echoes the request body
                var responseContent = requestBody != null
                    ? requestBody
                    : "No request body";


                // Return the response
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        return mockHandler.Object;
    }
```

你会看到这个设置 `Mock<HttpMessageHandler>` 然后,我进入 `UmamiClient`.
在这个代码中,我把这个插进我们的 `IServiceCollection` 设置方法 。 这增加了缔约国需要的所有服务。 `UmamiClient` 包括我们新的 `HttpMessageHandler` 返回时返回 `IServiceCollection` 用于试验。

```csharp
    public static IServiceCollection SetupServiceCollection(string webSiteId = Consts.WebSiteId,
        string umamiPath = Consts.UmamiPath, HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        var umamiClientSettings = new UmamiClientSettings
        {
            WebsiteId = webSiteId,
            UmamiPath = umamiPath
        };
        services.AddSingleton(umamiClientSettings);
        services.AddScoped<PayloadService>();
        services.AddLogging(x => x.AddConsole());
        // Mocking HttpMessageHandler with Moq
        var mockHandler = handler ?? EchoMockHandler.Create();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
        {
            var umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).ConfigurePrimaryHttpMessageHandler(() => mockHandler);
        return services;
    }
```

将它注入到 `UmamiClient` 然后,我使用这些服务 在 `UmamiClient` 设置 。

```csharp
    public static UmamiClient GetUmamiClient(IServiceCollection? serviceCollection = null,
        HttpContextAccessor? contextAccessor = null)
    {
        serviceCollection ??= SetupServiceCollection();
        SetupUmamiClient(serviceCollection, contextAccessor);
        if (serviceCollection == null) throw new NullReferenceException(nameof(serviceCollection));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<UmamiClient>();
    }
```

你会看到我这里有一堆备选参数 允许我给不同的测试类型 注入不同的选择

### 测试

所以现在我把所有这些设置都安排好了 我现在可以开始写测试了 `UmamiClient` 方法。

#### 发送发送

这些设置意味着 我们的测试其实很简单

```csharp
public class UmamiClient_SendTests
{
    [Fact]
    public async Task Send_Wrong_Type()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        await Assert.ThrowsAsync<ArgumentException>(async () => await umamiClient.Send(type: "boop"));
    }

    [Fact]
    public async Task Send_Empty_Success()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Send();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

这里您可以看到最简单的测试案例, 只是确保 `UmamiClient` 能够发出信息并获得回应;重要的是,我们还测试一个例外案例,即: `type` 是错的。 这是测试中经常被忽略的部分,确保代码如预期的那样失效。

#### 页面视图视图页面视图

为了测试我们的页面浏览方法 我们可以做类似的事情 在下面的代码里,我用我的 `EchoHttpHandler` 只要回过头来回想所发送的回复 并确保它回回我所期望的

```csharp
    [Fact]
    public async Task TrackPageView_WithNoUrl()
    {
        var defaultUrl = "/testpath";
        var contextAccessor = SetupExtensions.SetupHttpContextAccessor(path: "/testpath");
        var umamiClient = SetupExtensions.GetUmamiClient(contextAccessor: contextAccessor);
        var response = await umamiClient.TrackPageView();

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.Equal(content.Payload.Url, defaultUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
```

### HtpContFtext 辅助工具

此处使用 `HttpContextAccessor` 将路径设置为 `/testpath` 然后检查 `UmamiClient` 正确发送此信息 。

```csharp
    public static HttpContextAccessor SetupHttpContextAccessor(string host = Consts.Host,
        string path = Consts.Path, string ip = Consts.Ip, string userAgent = Consts.UserAgent,
        string referer = Consts.Referer)
    {
        HttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = new PathString(path);
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.Request.Headers.UserAgent = userAgent;
        httpContext.Request.Headers.Referer = referer;

        var context = new HttpContextAccessor { HttpContext = httpContext };
        return context;
    }

```

这对于我们的Umami客户代码很重要,因为从每项请求中发送的数据中,大部分实际上是动态生成的。 `HttpContext` 对象。 因此,我们完全不能发送任何信息 `await umamiClient.TrackPageView();` 并且它仍然会通过从电源中提取 Url 发送正确的数据 `HttpContext`.

我们稍后会看到,同样重要的是, 敬畏的发送 类似的东西, `UserAgent` 和 `IPAddress` Umami 服务器使用这些数据和“跟踪”用户视图来跟踪这些数据和“跟踪”用户视图,而不使用 cookie 。

为了有这个可以预测的,我们定义 一群Consts在 `Consts` 类。 因此,我们可以对照可预见的回应和要求进行测试。

```csharp
public class Consts
{
    public const string UmamiPath = "https://example.com";
    public const string WebSiteId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
    public const string Host = "example.com";
    public const string Path = "/example";
    public const string Ip = "127.0.0.1";
    public const string UserAgent = "Test User Agent";
    public const string Referer = "Test Referer";
    public const string DefaultUrl = "/testpath";
    public const string DefaultTitle = "Example Page";
    public const string DefaultName = "RSS";
    public const string DefaultType = "event";

    public const string Email = "test@test.com";

    public const string UserId = "11224456";
    
    public const string UserName = "Test User";
    
    public const string SessionId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
}
```

## 进一步测试

我们的Umami网络测试策略刚刚开始, `IHostedService` 并对照Umami产生的实际数据进行测试(该数据在任何地方都没有记录,但含有带有一些有用数据的JWT标记。 )

```json
{
  "alg": "HS256",
  "typ": "JWT"
}{
  "id": "b9836672-feee-55c5-985a-a5a23d4a23ad",
  "websiteId": "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
  "hostname": "example.com",
  "browser": "chrome",
  "os": "Windows 10",
  "device": "desktop",
  "screen": "1920x1080",
  "language": "en-US",
  "country": "GB",
  "subdivision1": null,
  "subdivision2": null,
  "city": null,
  "createdAt": "2024-09-01T09:26:14.418Z",
  "visitId": "e7a6542f-671a-5573-ab32-45244474da47",
  "iat": 1725182817
}2|Y*: �(N%-ޘ^1>@V
```

所以我们要测试这个, 模拟符号, 并可能返回每次访问的数据(因为您会记得, `uuid(websiteId,ipaddress, useragent)`).

# 在结论结论中

这只是测试 Ummi. Net 软件包的开始, 还有很多工作要做, 但这是一个良好的开始。 我会增加更多的测试 无疑会改进这些测试