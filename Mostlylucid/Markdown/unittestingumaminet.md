# Unit Testing Umami.Net - Testing UmamiClient

# Introduction
Now I have the [Umami.Net package](https://www.nuget.org/packages/Umami.Net/) out there I of course want to ensure it all works as expected. To do this the best way is to somewhat comprehensively test all the methods and classes. This is where unit testing comes in.
Note: This isn't a 'perfect approach' type post, it's just how I've currently done it. In reality I don't REALLY need to Mock the `IHttpMessageHandler` here a you can attack a DelegatingMessageHandler to a normal HttpClient to do this. I just wanted to show how you can do it with a Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Unit Testing
Unit testing refers to the process of testing individual units of code to ensure they work as expected. This is done by writing tests that call the methods and classes in a controlled way and then checking the output is as expected.

For a package like Umami.Net this is soewhat tricky as it both calls a remote client over `HttpClient` and has an `IHostedService` it uses to make the sending of new event data as seamless as possible.

## Testing UmamiClient
The major part of testing an `HttpClient` based library is avoiding the actual 'HttpClient' call. This is done by creating a `HttpClient` that uses a `HttpMessageHandler` that returns a known response. This is done by creating a `HttpClient` with a `HttpMessageHandler` that returns a known response; in this case I just echo back the input response and check that's not been mangled by the `UmamiClient`.

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

As you'll see this sets up a `Mock<HttpMessageHandler>` I then pass into the `UmamiClient`. 
In this code I hook this into our `IServiceCollection` setup method. This adds all the services required by the `UmamiClient` including our new `HttpMessageHandler` and then returns the `IServiceCollection` for use in the tests.

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
To use this and inject it into the `UmamiClient` I then use these services in the `UmamiClient` setup.

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

You'll see I have a bunch of alternative optional parameters here allowing me to inject different options for different test types. 

### The Tests
So now I have all this setup in place I can now start writing tests for the `UmamiClient` methods.
#### Send
What all this setup means is that our tests can actually be pretty simple

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

Here you see the simplest test case, just ensuring that the `UmamiClient` can send a message and get a response; importantly we also test for an exception case where the `type` is wrong. This is an often overlooked part of testing, ensuring that the code fails as expected.

#### Page View
To test our pageview method we can do something similar. In the code below I use my `EchoHttpHandler` to just reflect back the sent response and ensure that it sends back what I expect. 

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
### HttpContextAccessor
This uses the `HttpContextAccessor` to set the path to `/testpath` and then checks that the `UmamiClient` sends this correctly.

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
This is important for our Umami client code as much of the data sent from each request is actually dynamically generated from the `HttpContext` object. So we can send nothing at all in a `await umamiClient.TrackPageView();` call and it will still send the correct data by extracting the Url from the `HttpContext`.

As we'll see later it's also important the awe send items like the `UserAgent` and `IPAddress` as these are used by the Umami server to track the data and 'track' user views without using cookies.

In order to have this predictable we define a bunch of Consts in the `Consts` class. So we can test against predictable responses and requests. 

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

## Further testing
This is just the start of our testing strategy for Umami.Net, we still have to test the `IHostedService` and test against the actual data Umami generates (which isn't documented anywhere but contains a JWT token with some useful data.)

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
So we'll want to test for that, simulate the token and possibly return the data on each visit (as you'll recall this is made from a `uuid(websiteId,ipaddress, useragent)`).

# In Conclusion
This is just the start of testing the Umami.Net package, there's a lot more to do but this is a good start. I'll be adding more tests as I go and no doubt improving these ones. 