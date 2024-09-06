# इकाई जाँच उममी.Net - nyomyyomy डाटा बिना मोजे का उपयोग किया जा रहा है

# परिचय

इस श्रृंखला के पिछले भाग में जहाँ मैंने परखा[ उममी. नैट ट्रैक विधि ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-0420: 30</datetime>
[TOC]

## समस्या

पिछले भाग में मैं मुझे एक देना करने के लिए इस्तेमाल किया `Mock<HttpMessageHandler>` और उस हैंडलर को वापस लें जिसका प्रयोग किया गया है `UmamiClient`, यह एक आम पैटर्न है जब जाँच कोड इस्तेमाल करते हैं `HttpClient`___ इस पोस्ट में मैं नए को जाँचने के लिए आप दिखाएगा `UmamiDataService` के बगैर q.q.q. kgm

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

## मोजे का इस्तेमाल क्यों करें?

मोके एक शक्तिशाली उपहास लाइब्रेरी है जो आपको इंटरफेस तथा वर्गों के लिए उपहास वस्तुओं को बनाने देता है. यह व्यापक रूप से अपनी निर्भरता से जाँच के अंतर्गत कोड को अलग करने के लिए परीक्षण में प्रयोग किया जाता है. लेकिन कुछ ऐसे मामले हैं जहाँ मोफ का इस्तेमाल करना बहुत मुश्‍किल हो सकता है या फिर वह नामुमकिन भी हो सकता है । उदाहरण के लिए, जब जाँच कोड जो स्थिर विधियों का प्रयोग करता है या जब कोड को जाँचता है तो उसकी निर्भरता को अटूट करता है.

उदाहरण जो मैंने ऊपर दिया है परीक्षण में बहुत कुछ बनाता है `UmamiClient` क्लास, लेकिन इसमें कुछ बदलाव भी हैं । यह निश्चित रूप से यूजी कोड है और मैं वास्तव में जरूरत नहीं है सामान का एक टुकड़ा करता है. अतः जब परीक्षा हो तो परिश्रम में लग जाओ, `UmamiDataService` मैं एक अलग तरीके से बात करने का फैसला किया ।

# उममी डाटा सर्विस जांच की जा रही है

वह `UmamiDataService` उममी लाइब्रेरी के साथ एक भविष्य के अतिरिक्त है जो आपको उमरमी से डेटा लाने देगा कि कैसे कई लोगों को एक पृष्ठ था, क्या एक निश्चित प्रकार की घटना हुई है, एक निश्चित प्रकार के बारे में, शहर ओएस, स्क्रीन आकार, इत्यादि. यह एक बहुत शक्तिशाली लेकिन अभी है [सिर्फ जावास्क्रिप्ट के माध्यम से उममीटो काम करता है](https://umami.is/docs/api/website-stats)___ तो मैं इसके लिए C# ग्राहक बनाने की कोशिश में चला गया कि डेटा के साथ खेलने के लिए चाहता था.

वह `UmamiDataService` क्लास में विभाजित किया जाता है विभिन्न वर्गों में (इन तरीकों लंबे समय के लिए लंबे समय से कर रहे हैं) उदाहरण के लिए `PageViews` विधि.

आप देख सकते हैं कि कोड की अत्यधिक मात्रा पृष्ठVERRERERRERERD वर्ग में से क्वैरी मापदंड बनाया जा रहा है (यह करने के लिए अन्य तरीके हैं लेकिन यह, उदाहरण के लिए गुण या समायोजन यहाँ काम करने के लिए.

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
जैसा कि आप इसे वास्तव में देख सकते हैं सिर्फ क्वैरी वाक्यांश बनाता है. कॉल को सत्यापित करता है (देखें) [अंतिम लेख](/blog/unittestinglogginginaspnetcore) इस बारे में कुछ जानकारी के लिए और फिर उममी एपीआई में कॉल करता है। तो फिर हम यह कैसे परख सकते हैं?

## उममी डाटा- सर्विस को जाँच किया जा रहा है

उममीक्रेंट परीक्षण के विपरीत, मैंने कोशिश करने का निर्णय लिया `UmamiDataService` के बगैर q.q.q. kgm इसके बजाय, मैं एक सरल बनाया `DelegatingHandler` क्लास के इस फैसले से मुझे फिर से जवाब देने का मौका मिलता है । यह मोजे का उपयोग करने से एक बहुत आसान तरीका है और मुझे परीक्षण करने की अनुमति देता है `UmamiDataService` और लग़ो नहीं है `HttpClient`.

आप नीचे के कोड में मैं सिर्फ विस्तार देख सकते हैं `DelegatingHandler` और ओवरराइड करें `SendAsync` विधि. यह तरीका मुझे निवेदन का निरीक्षण करने और निवेदन के आधार पर एक प्रतिक्रिया वापस करने की अनुमति देता है ।

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

## सेटअप

नए सेट करने के लिए `UmamiDataService` इस हैंडलर का उपयोग करने के लिए समान रूप से सरल है.

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

आप मैं सिर्फ ऊपर सेट देखेंगे `ServiceCollection`, जोड़ जोड़ें `FakeLogger<T>` (फिर देखते रहो) [इस बारे में ज़्यादा जानकारी के लिए अंतिम लेख](/blog/unittestinglogginginaspnetcore) फिर एक ज़रूरी चीज़ (बारिश) को तक़सीम करती हैं `UmamiData` उपयोक्ता नाम व पासवर्ड के साथ सेवा मैं प्रयोग करना चाहता हूँ (इसलिए मैं विफलता का परीक्षण कर सकता हूँ).

फिर मैं उसे फोन करता हूँ `services.SetupUmamiData(username, password);` जो एक एक्सटेंशन विधि है मैं स्थापित करने के लिए बनाया `UmamiDataService` के साथ `UmamiDataDelegatingHandler` और `AuthService`;

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

आप देख सकते हैं कि यह मैं में हुक जहां है `UmamiDataDelegatingHandler` और `AuthService` करने के लिए `UmamiDataService`___ इस संरचना है कि जिस तरह से है वह है `AuthService` 'उन्स' `HttpClient` और `UmamiDataService` उपयोग `AuthService` उममी एपीआई में कॉल करने के लिए `bearer` टोकन तथा `BaseAddress` पहले से सेट.

## परीक्षण

वास्तव में यह वास्तव में इस वास्तव में सरल परीक्षण बनाता है. यह मैं भी लॉगिंग को जाँचने के लिए चाहता था के रूप में मैं भी चाहता था बस एक सा जुड़ता है. यह सब कर रहा है मेरे माध्यम से प्रेषित कर रहा है `DelegatingHandler` और मैं निवेदन पर आधारित एक प्रतिक्रिया सिमुलेट करता हूँ.

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

### प्रतिक्रिया सिमुलेट किया जा रहा है

इस विधि के लिए प्रतिक्रिया सिमुलेट करने के लिए आप मुझे याद होगा इस लाइन में `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

यह सभी क्वैरी स्ट्रिंग से जानकारी खींच रहा है और एक 'मूलवादी' प्रतिक्रिया बनाता है (जीवन परीक्षा पर आधारित है, फिर से एक बार फिर बहुत छोटी खुराक इस पर. आप शुरू और अंत तारीख के बीच दिनों की संख्या के लिए जाँच करेंगे और फिर एक ही दिन की संख्या के साथ प्रतिक्रिया लौटा देंगे.

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

# ऑन्टियम

तो यह वास्तव में यह एक परीक्षण के लिए बहुत सरल है `HttpClient` qqu का उपयोग किए बिना अनुरोध और मुझे लगता है कि यह अधिक से अधिक सफाई इस तरह है. आप मोजे में से कुछ तो खो देते हैं, लेकिन इस तरह के आसान परीक्षण के लिए, मुझे लगता है कि यह एक अच्छा व्यापार समाप्त है.