# इकाई जांच उममी. net - टेस्ट उममीम

# परिचय

अब मेरे पास है [उममी पैकेज](https://www.nuget.org/packages/Umami.Net/) वहाँ बाहर मैं निश्चित रूप से यह सभी की उम्मीद के रूप में काम करता है सुनिश्चित करने के लिए चाहते हैं. ऐसा करने का सबसे अच्छा तरीका है, सभी तरीक़ों और वर्गों को सीमित रूप से जाँच करना । यह इकाई जाँच में आता है जहां है.
नोट: यह एक 'सिद्ध प्रवेश तरह के पोस्ट नहीं है, यह है कि कैसे मैं वर्तमान में यह किया है. वास्तव में मैं वास्तव में नकली करने की जरूरत नहीं है `IHttpMessageHandler` यहाँ एक आप एक विनाशकारी संदेशर पर हमला कर सकते हैं यह करने के लिए एक सामान्य Diald करने के लिए। मैं सिर्फ आप इसे एक नकली के साथ कैसे कर सकते हैं दिखाना चाहता था.

[विषय

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024- 09- 0117: 22</datetime>

# इकाई जाँच

इकाई जाँच कोड के व्यक्ति को जाँचने की प्रक्रिया का उल्लेख करता है यह सुनिश्चित करने के लिए कि वे वांछित रूप में काम करते हैं. इसे लिखने के द्वारा किया जाता है जो उन तरीक़ों और वर्गों को नियंत्रित करते हैं और फिर आउटपुट की अपेक्षा की जाती है ।

उममी की तरह एक पैकेज के लिए यह बहुत ही मुश्किल है क्योंकि यह दोनों एक रिमोट ग्राहक को कॉल करते हैं `HttpClient` और ये कि उसी पर (कयामत में) दोबारा उठाना लाज़िम है `IHostedService` यह नए कार्यक्रम डाटा को जितना संभव हो उतना भेजने के लिए प्रयोग करता है ।

## उममी कुल जांच की जा रही है

परीक्षा का मुख्य भाग `HttpClient` आधारित लाइब्रेरी वास्तविक'कहीं' कॉल से बचने के लिए है. इसे बनाने के द्वारा बनाया जाता है `HttpClient` जो एक उपयोग करता है `HttpMessageHandler` यह एक ज्ञात प्रतिक्रिया बताता है. इसे बनाने के द्वारा बनाया जाता है `HttpClient` एक के साथ `HttpMessageHandler` कि एक ज्ञात प्रतिक्रिया बताता है, इस मामले में मैं सिर्फ इनपुट प्रतिक्रिया वापस देता हूँ और जांच करता हूँ कि नहीं किया गया है `UmamiClient`.

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

आप इस सेट एक देख जाएगा के रूप में `Mock<HttpMessageHandler>` फिर मैं अंदर जाता हूँ `UmamiClient`.
इस कोड में मैं इसे हमारे में हुक करें `IServiceCollection` विधि सेटअप करें. यह सभी आवश्यकताओं को बढ़ाता है `UmamiClient` हमारे नए को भी शामिल करें `HttpMessageHandler` फिर (उसका) एक अन्दाज़ा मुक़र्रर किया `IServiceCollection` परीक्षण में उपयोग के लिए.

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

इसे प्रयोग करने के लिए और उसमें शामिल करने के लिए `UmamiClient` मैं तो इन सेवाओं का उपयोग इन सेवाओं में `UmamiClient` सेटअप.

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

आप देख सकते हैं कि मैं वैकल्पिक पैरामीटर्स यहाँ मुझे विभिन्न परीक्षा प्रकार के लिए विभिन्न विकल्पों को बाहर करने की अनुमति देता हूँ।

### परीक्षण

तो अब मैं जगह में इस सभी सेटअप है...... मैं अब के लिए लिख सकते हैं जाँच शुरू कर सकते हैं `UmamiClient` तरीके ।

#### भेजें

क्या इस सेटअप का मतलब है कि हमारी परीक्षा वास्तव में बहुत सरल हो सकती है

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

यहाँ आप सबसे आसान जांच मामले को देखते हैं, बस सुनिश्चित करते हैं कि `UmamiClient` एक संदेश भेज सकते हैं और प्रतिक्रिया प्राप्त कर सकते हैं; महत्वपूर्ण रूप से हम किसी अपवाद के लिए भी परीक्षण कर सकते हैं जहाँ कोई अपवाद हो `type` गलत है. यह अकसर जाँच के एक भाग को नज़रअंदाज़ करता है, और यह निश्‍चित करता है कि कोड अपेक्षाकृत रूप से असफल हो जाता है ।

#### पृष्ठ दृश्य

अपने पेज दृश्य विधि की जाँच करने के लिए हम भी ऐसा कर सकते हैं । नीचे दिए गए कोड में मैं अपना उपयोग करें `EchoHttpHandler` बस वापस भेजने की प्रतिक्रिया को प्रतिबिम्बित करने के लिए और यह सुनिश्चित करने के लिए कि यह मैं क्या उम्मीद करता हूँ.

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

### 2880 संदर्भकर्ता

यह उपयोग करता है `HttpContextAccessor` पथ को सेट करने के लिए `/testpath` फिर जाँच करता है अलग-अलग, `UmamiClient` यह उचित रूप से भेज दिया गया.

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

यह हमारे उममी क्लाएंट कोड के लिए महत्वपूर्ण है कि प्रत्येक निवेदन से प्राप्त डाटा के अधिकांश के रूप में वास्तव में प्रभावशाली रूप से तैयार किया गया है `HttpContext` वस्तु. तो हम एक में कुछ भी नहीं भेज सकते हैं `await umamiClient.TrackPageView();` कॉल और यह अभी भी यूआरएल को बाहर निकालने के द्वारा सही डाटा भेज देगा `HttpContext`.

जैसे हम बाद में देखेंगे यह भी महत्वपूर्ण है डर की वस्तुओं को ऐसे भेजना `UserAgent` और `IPAddress` जब ये उममी सर्वर द्वारा प्रयोग में लिए जाते हैं तो डेटा को ट्रैक करने और 'ट्रैक' उपयोक्ता दृश्यों को बिना कुकीज़ का उपयोग किए रखता है.

इस भविष्यवाणी करने योग्य होने के लिए...... हम में कनेक्शन के एक गुच्छा की परिभाषा दें `Consts` वर्ग. तो हम अटकलें लगाने और अनुरोध करने के खिलाफ जाँच कर सकते हैं.

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

## आगे की जाँच

यह सिर्फ उममी के लिए हमारे परीक्षण रणनीति की शुरुआत है, हम अभी भी परीक्षण करने के लिए है `IHostedService` वास्तविक डाटा उममी के खिलाफ परीक्षा (जो प्रकाशित नहीं है) पर किसी भी उपयोगी डाटा के साथ एक जे डब्ल्यूआईटी टोकन है.)

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

तो हम उस के लिए परीक्षण करना चाहते हैं, संकेत का सिमुलेट करें और संभवतः प्रत्येक भेंट पर डेटा लौटा दें (जैसे आप इसे याद करेंगे) `uuid(websiteId,ipaddress, useragent)`).

# ऑन्टियम

यह सिर्फ उममी पैकेज परीक्षण की शुरुआत है, लेकिन यह एक अच्छी शुरुआत है. मैं इन लोगों को सुधार करने के रूप में और अधिक परीक्षण जोड़ने के लिए किया जाएगा.