# Umami.Net - 不使用摩克测试Umami数据

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在这个系列的上一部分,我测试了[ Umami.Net跟踪方法 ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-004T20:30</datetime>
[技选委

## 问题

在前一部分我用莫克给我一个 `Mock<HttpMessageHandler>` 中所使用的处理器,然后返回 `UmamiClient`,这是一个常见模式,当测试代码使用 `HttpClient`.. 在这个职位上,我将教你如何测试新的 `UmamiDataService` 没有使用 Moq 。

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

## 为什么用莫克?

Moq 是一个强大的模拟图书馆, 允许您为界面和课程创建模拟对象 。 它广泛用于单位测试,将测试中的代码与其依赖性隔离开来。 然而,在有些情况下,使用Moq可能是繁琐的,甚至是不可能的。 例如,当测试代码使用静态方法时,或当测试中的代码与其依赖性紧密结合时。

以上我所举的例子在测试 `UmamiClient` 类,但也有一些缺点。 这是UGLY密码 做很多我并不需要的东西 所以当测试时 `UmamiDataService` 我决定尝试另一种方法

# 测试 UmmiDataServices

缩略 `UmamiDataService` 这是Umami. Net 库的未来补充, 这将使您能够从 Umami 库中获取数据, 例如查看网页有多少浏览, 某类事件发生多少, 由数以吨计的参数覆盖国家、 城市、 OS、 屏幕大小等过滤 。 这是一个非常强大的,但现在 [Umami API 仅通过 JavaScript 有效](https://umami.is/docs/api/website-stats).. 所以想利用这些数据 我努力为它创建了一个 C # 客户端。

缩略 `UmamiDataService` 类被划分为模块部分类(方法为SUPER长),例如,这里是 `PageViews` 方法。

您可以看到,该代码的 MUCH 正在从 PagePeviewResources Services 类( 还有其他方法可以做到这一点, 但此方法, 例如在这里使用属性或反射工作 ) 中构建QueyString 。

<details>
<summary>GetPageViews</summary>
```csharp
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(PageViewsRequest pageViewsRequest)
    {
        if (await authService.LoginAsync() == false)
            return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
        // Start building the query string
        var queryParams = new List<string>
        {
            $"startAt={pageViewsRequest.StartAt}",
            $"endAt={pageViewsRequest.EndAt}",
            $"unit={pageViewsRequest.Unit.ToLowerString()}"
        };

        // Add optional parameters if they are not null
        if (!string.IsNullOrEmpty(pageViewsRequest.Timezone)) queryParams.Add($"timezone={pageViewsRequest.Timezone}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Url)) queryParams.Add($"url={pageViewsRequest.Url}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Referrer)) queryParams.Add($"referrer={pageViewsRequest.Referrer}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Title)) queryParams.Add($"title={pageViewsRequest.Title}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Host)) queryParams.Add($"host={pageViewsRequest.Host}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Os)) queryParams.Add($"os={pageViewsRequest.Os}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Browser)) queryParams.Add($"browser={pageViewsRequest.Browser}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Device)) queryParams.Add($"device={pageViewsRequest.Device}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Country)) queryParams.Add($"country={pageViewsRequest.Country}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Region)) queryParams.Add($"region={pageViewsRequest.Region}");
        if (!string.IsNullOrEmpty(pageViewsRequest.City)) queryParams.Add($"city={pageViewsRequest.City}");

        // Combine the query parameters into a query string
        var queryString = string.Join("&", queryParams);

        // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews?{queryString}");

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully got page views");
            var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
            return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new PageViewsResponseModel());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.LoginAsync();
            return await GetPageViews(pageViewsRequest);
        }

        logger.LogError("Failed to get page views");
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode,
            response.ReasonPhrase ?? "Failed to get page views", null);
    }
```

</details>
正如你可以看到的,这实际上只是构建了一个查询字符串。 认证调用电话(见 [最后一条](/blog/unittestinglogginginaspnetcore) 以了解这方面的一些细节),然后向Umami API发出呼吁。 那么,我们如何测试这个呢?

## 测试 UmmiData Services 数据服务

与UmamiClient的测试相反, 我决定测试 `UmamiDataService` 没有使用 Moq 。 相反,我创造了一个简单的 `DelegatingHandler` 允许我询问请求,然后回覆答复。 这比使用Moq简单得多, `UmamiDataService` 无需嘲笑 `HttpClient`.

在下面的代码中,你可以看到,我只要延长 `DelegatingHandler` 并覆盖 `SendAsync` 方法。 这种方法使我能够检查请求,并根据请求回信答复。

```csharp
public class UmamiDataDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/api/auth/login":
                var authContent = await request.Content.ReadFromJsonAsync<AuthRequest>(cancellationToken);
                if (authContent?.username == "username" && authContent?.password == "password")
                    return ReturnAuthenticatedMessage();
                else if (authContent?.username == "bad")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
            default:

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/metrics"))
                {
                    var metricsRequest = GetParams<MetricsRequest>(request);
                    return ReturnMetrics(metricsRequest);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
 }
```

## 设置设置设置设置设置设置设置

设置新的 `UmamiDataService` 使用此处理器同样简单。

```csharp
    public IServiceProvider GetServiceProvider (string username="username", string password="password")
    {
        var services = new ServiceCollection();
        var mockLogger = new FakeLogger<UmamiDataService>();
        var authLogger = new FakeLogger<AuthService>();
        services.AddScoped<ILogger<UmamiDataService>>(_ => mockLogger);
        services.AddScoped<ILogger<AuthService>>(_ => authLogger);
        services.SetupUmamiData(username, password);
        return  services.BuildServiceProvider();
        
    }
```

你会看到我刚刚设计了 `ServiceCollection`,加上 `FakeLogger<T>` (再次见 [有关细节的最后一篇文章](/blog/unittestinglogginginaspnetcore) 然后,然后, `UmamiData` 使用用户名和密码的服务( 这样我就可以测试失败) 。

然后我呼唤你们, `services.SetupUmamiData(username, password);` 这是我为建立 `UmamiDataService` 和和 `UmamiDataDelegatingHandler` 和 `AuthService`;

```csharp
    public static void SetupUmamiData(this IServiceCollection services, string username="username", string password="password")
    {
        var umamiSettings = new UmamiDataSettings()
        {
            UmamiPath = Consts.UmamiPath,
            Username = username,
            Password = password,
            WebsiteId = Consts.WebSiteId
        };
        services.AddSingleton(umamiSettings);
        services.AddHttpClient<AuthService>((provider,client) =>
        {
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            

        }).AddHttpMessageHandler<UmamiDataDelegatingHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));  //Set lifetime to five minutes

        services.AddScoped<UmamiDataDelegatingHandler>();
        services.AddScoped<UmamiDataService>();
    }
```

你可以看到,这就是 在这里,我勾入 `UmamiDataDelegatingHandler` 和 `AuthService` 会 议 日 和 排 `UmamiDataService`.. 目前的结构方式是 `AuthService` # 自己 # # 自己 # # 自己 # # 自己 # # `HttpClient` 和 `UmamiDataService` 使用 `AuthService` 致电Umami API 与 `bearer` 符号和符号 `BaseAddress` 已经设置 。

## 测试

这真的让测试过程变得非常简单。 这只是一点动词 因为我也想测试 伐木。 它所做的就是通过我的 `DelegatingHandler` 我根据请求模拟回应。

```csharp
public class UmamiData_PageViewsRequest_Test : UmamiDataBase
{
    private readonly DateTime StartDate = DateTime.ParseExact("2021-10-01", "yyyy-MM-dd", null);
    private readonly DateTime EndDate = DateTime.ParseExact("2021-10-07", "yyyy-MM-dd", null);
    
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var umamiDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var umamiDataLogger = serviceProvider.GetRequiredService<ILogger<UmamiDataService>>();
        var result = await umamiDataService.GetPageViews(StartDate, EndDate);
        var fakeAuthLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeAuthLogger.Collector; 
        IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
        Assert.Contains("Login successful", logs.Select(x => x.Message));
        
        var fakeUmamiDataLogger = (FakeLogger<UmamiDataService>)umamiDataLogger;
        FakeLogCollector umamiDataCollector = fakeUmamiDataLogger.Collector;
        IReadOnlyList<FakeLogRecord> umamiDataLogs = umamiDataCollector.GetSnapshot();
        Assert.Contains("Successfully got page views", umamiDataLogs.Select(x => x.Message));
        
        Assert.NotNull(result);
    }
}
```

### 模拟响应

模拟此方法的响应, 您会记得, 我有这条线在 `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

所有这一切都是从查询字符串中提取信息, 并构建一个“现实主义”回应(基于我编译的现场测试, 你会看到我测试从开始到结束日期之间的天数, 然后用同样的天数返回回复。

```csharp
    private static HttpResponseMessage ReturnPageViewsMessage(PageViewsRequest request)
    {
        var startAt = request.StartAt;
        var endAt = request.EndAt;
        var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startAt).DateTime;
        var endDate = DateTimeOffset.FromUnixTimeMilliseconds(endAt).DateTime;
        var days = (endDate - startDate).Days;

        var pageViewsList = new List<PageViewsResponseModel.Pageviews>();
        var sessionsList = new List<PageViewsResponseModel.Sessions>();
        for(int i=0; i<days; i++)
        {
            
            pageViewsList.Add(new PageViewsResponseModel.Pageviews()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*4
            });
            sessionsList.Add(new PageViewsResponseModel.Sessions()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*8
            });
        }
        var pageViewResponse = new PageViewsResponseModel()
        {
            pageviews = pageViewsList.ToArray(),
            sessions = sessionsList.ToArray()
        };
        var json = JsonSerializer.Serialize(pageViewResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
```

# 在结论结论中

所以测试一个 `HttpClient` 请求不需要使用Moq, 我觉得这样更干净。 在莫克州,你确实失去了一些 先进的技术 但对于这样的简单测试, 我认为这是一个很好的权衡。