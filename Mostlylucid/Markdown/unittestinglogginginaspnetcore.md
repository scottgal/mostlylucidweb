# Unit Testing Umami.Net - Logging in ASP.NET Core

# Introduction
I'm a relative noob using Moq (yes I'm aware of the controversies) and I was trying to test a new service I'm adding to Umami.Net, UmamiData. This is a service this allows me to pull data from my Umami instance to use in stuff like sorting posts by popularity etc...

[TOC]
<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# The Problem
I was trying to add a simple test for the login function I need to use when pulling data. 
As you can see it's a simple service which passes a username and password to the `/api/auth/login` endpoint and gets a result. If the result is successful it stores the token in the `_token` field and sets the `Authorization` header for the `HttpClient` to use in future requests.

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
Now I also wanted to test against the logger to make sure it was logging the correct messages. I'm using the `Microsoft.Extensions.Logging` namespace and I wanted to test that the correct log messages were being written to the logger.

In Moq there's a BUNCH of posts around testing logging they all have this basic form (from https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

HOWEVER due to Moq's recent changes (It.IsAnyType is now obsolete) and ASP.NET Core's changes to FormattedLogValues I was having a hard time getting this to work.

I tried a BUNCH of versions and variants but it always failed. So...I gave up.

# The Solution
So reading a bunch of GitHub messages I came across a post by David Fowler (my former colleague and now the Lord of .NET) which showed a simple way to test logging in ASP.NET Core. 
This uses the *new to me* `Microsoft.Extensions.Diagnostics.Testing` [package](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) which has some really useful extensions for testing logging.

So instead of all the Moq stuff I just added the `Microsoft.Extensions.Diagnostics.Testing` package and added the following to my tests.


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

You'll see that this sets up my ServiceCollection, adds the new `FakeLogger<T>` and then sets up the `UmamiData` service with the username and password I want to use (so I can test failure).

## The Tests Using FakeLogger
Then my tests can become:

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
Where you'll see I simply call the `GetServiceProvider` method to get my service provider, then get the `AuthService` and `ILogger<AuthService>` from the service provider.

Because I have these set up as `FakeLogger<T>` I can then access the `FakeLogCollector` and `FakeLogRecord` to get the logs and check them.

Then I can simply check the logs for the correct messages.

## In Conclusion
So there you have it, a simple way to test log messages in Unit Tests without the Moq nonsense.