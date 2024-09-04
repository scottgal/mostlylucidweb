# Umami.Net - ASP.NET核心的登录

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

我是使用Moq(是的,我知道有争议)的亲戚, 我想测试我给Ummami.Net, UmmiData 添加的新服务。 这个服务可以让我从我的Umami实例中提取数据,

[技选委

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-009-04T13:22</datetime>

# 问题

我试图添加一个简单的测试, 测试在调用数据时我需要使用的登录函数 。
正如您可以看到的,这是一个简单的服务, 传递用户名和密码到 `/api/auth/login` 终点和结果。 如果结果成功,它会将标语存储在 `_token` 字段设置 `Authorization` 和 `HttpClient` 用于今后的请求。

```csharp
public class AuthService(HttpClient httpClient, UmamiDataSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token = string.Empty;
    public HttpClient HttpClient => httpClient;

    public async Task<bool> LoginAsync()
    {
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                logger.LogError("Login failed");
                return false;
            }

            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
}
```

现在,我还想对登录器进行测试,以确保它记录正确的信息。 我用的是 `Microsoft.Extensions.Logging` 命名空间和我想测试 正确的日志信息 正在写入到日志中 。

在Moq,有一套BUNCH的标签 环绕着测试伐木 他们都有这种基本的形式(来自https://adamstorr.co.uk/blog/mocking-ilogger- with-moq/)

```csharp
public static Mock<ILogger<T>> VerifyDebugWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
{
    Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;
    
    logger.Verify(
        x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => state(v, t)),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

    return logger;
}
```

以及ASP.NET Core对格式化LogValues的修改,

我试过BUNCH的版本和变体 但总是失败了 所以... 我放弃了

# 解决方案

读到一连串GitHub留言, 我遇见David Fowler(我前同事,现为.NET之王)的一篇文章,
此处使用 *对我而言全新的* `Microsoft.Extensions.Diagnostics.Testing` [软件包包包](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) 测试日志时有一些非常有用的扩展 。

而不是所有莫克人的东西,我刚刚加了 `Microsoft.Extensions.Diagnostics.Testing` 软件包,并在我的测试中添加以下内容。

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

你会看到,这设置了 我的服务集合, 增加新的 `FakeLogger<T>` 然后,你应当设置, `UmamiData` 使用用户名和密码的服务( 这样我就可以测试失败) 。

## 使用假冒错误的测试

然后,我的测试可以变成:

```csharp
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var authService = serviceProvider.GetRequiredService<AuthService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var result = await authService.LoginAsync();
        var fakeLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeLogger.Collector; // Collector allows you to access the captured logs
         IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
         Assert.Contains("Login successful", logs.Select(x => x.Message));
        Assert.True(result);
    }
```

在那里你会看到,我只要打电话 `GetServiceProvider` 获得我的服务供应商的方法,然后获得 `AuthService` 和 `ILogger<AuthService>` 由服务提供商提供。

因为我有这些设置 `FakeLogger<T>` 然后我就可以进入 `FakeLogCollector` 和 `FakeLogRecord` 得到日志并检查它们。

然后,我可以简单地检查日志以获取正确信息。

# 在结论结论中

所以你有了它,一个简单的方法 测试Unit Tests的日志信息 而不是莫克的胡说八道。