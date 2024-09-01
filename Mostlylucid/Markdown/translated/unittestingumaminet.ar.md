# (أ) وحدة اختبار وحدة الاختبار - الشبكة - وحدة الاختبار

# أولاً

الآن لدي [مجموعة مواد لشبكة الأمان](https://www.nuget.org/packages/Umami.Net/) هناك أريد بالطبع أن أضمن أن كل شيء يعمل كما هو متوقع. وللقيام بذلك، فإن أفضل طريقة هي إجراء اختبار شامل إلى حد ما لجميع الأساليب والفصول. هذا هو المكان الذي يأتي اختبار الوحدة في.
ملاحظة: هذه ليست وظيفة من نوع "النهج الأمثل"، إنها الطريقة التي قمت بها حالياً. في الواقع أنا لست في الواقع حقاً بحاجة إلى `IHttpMessageHandler` هنا a يُمْكِنُ أَنْ تُهاجمَ a تفويض Missage Handler إلى a عادي Http Client إلى هذا. أنا فقط أردت أن أظهر كيف يمكنك أن تفعل ذلك مع موك.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01TT17: 22</datetime>

# وحدة الاختبار

ويشير الاختبار الذي تجريه الوحدة إلى عملية اختبار فرادى وحدات الشفرة للتأكد من أنها تعمل على النحو المتوقع. ويتم ذلك عن طريق اختبارات الكتابة التي تدعو الأساليب و الفئات بطريقة متحكم بها ثم التحقق من الناتج كما هو متوقع.

لحزمة مثل Umamamimi.Net هذا صعب جداً حيث أن كلاهما يدعوا عميلاً نائياً `HttpClient` التي لها `IHostedService` وهو يستخدم لجعل إرسال بيانات الأحداث الجديدة سلسة قدر الإمكان.

## شهادة الاختبار: معدل الحمل

الجزء الأكبر من اختبار `HttpClient` المكتبة الأساسية تتجنب نداء 'HttpClient' الفعلي. وهذا ما يتم من خلال إنشاء `HttpClient` التي تستخدم `HttpMessageHandler` التي ترجع استجابة معروفة. وهذا ما يتم من خلال إنشاء `HttpClient` (أ) `HttpMessageHandler` في هذه الحالة أنا فقط أردد ردّ ردّ الإدخال وأتحقق من أنّه لم يتم تهكمه من قبل الـ `UmamiClient`.

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

كما سترون هذه الانشاءات `Mock<HttpMessageHandler>` ثمّ أَدْرَجُ إلى `UmamiClient`.
في هذه الشفرة سأربط هذا في `IServiceCollection` (أ) طريقة التنفيذ. ويضاف إلى ذلك جميع الخدمات التي يتطلبها `UmamiClient` بما في ذلك `HttpMessageHandler` ومن ثم ترجع `IServiceCollection` للاستخدام في الاختبارات.

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

إلى استخدام هذا و حقنه في `UmamiClient` ثم استخدم هذه الخدمات في `UmamiClient` -مُعَدّة. -مُعَدّة.

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

سترى أن لدي مجموعة من البارامترات الاختيارية البديلة هنا تسمح لي بحقن خيارات مختلفة لأنواع مختلفة من الاختبارات.

### الإخت الإختبارات

الآن لدي الآن كل هذا التهيئة في مكان ما يمكنني الآن بدء كتابة الاختبارات لـ `UmamiClient` (أ)

#### دعم

كل ما يعنيه كل هذا هو أن اختباراتنا يمكن في الواقع أن تكون بسيطة جداً

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

هنا ترون قضية الاختبار المبسطة، مجرد التأكد من أن `UmamiClient` يمكن أن يرسل رسالة ويحصل على رد، والأهم من ذلك نحن أيضاً نختبر لحالة استثنائية حيث `type` هو خطأ. وهذا جزء من الاختبار كثيراً ما يُغفل، مما يضمن فشل الشفرة على النحو المتوقع.

#### 

لإختبار طريقة عرض الصفحة يمكننا أن نفعل شيئاً مشابهاً في الشفرة تحت الشفرة أنا أستخدم `EchoHttpHandler` أن تعكس فقط الرد المرسل وتأكد من أنه يعيد ما أتوقعه.

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

### Htttpsussus

هذا استخدامات `HttpContextAccessor` لوضع الطريق إلى `/testpath` ومن ثم التحقق من أن `UmamiClient` إرسـال هـذا حقـيـقـاً

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

هذا مهم بالنسبة لرمز عميلنا في أومامي لأن الكثير من البيانات المرسلة من كل طلب `HttpContext` (أ) الهدف من الهدف. لذا يمكننا أن نرسل لا شيء على الإطلاق في `await umamiClient.TrackPageView();` وسترسل البيانات الصحيحة عن طريق استخراج أورل من `HttpContext`.

كما سنرى لاحقاً من المهم أيضاً إرسال عناصر مثل `UserAgent` وقد عقد مؤتمراً بشأن `IPAddress` كما أنها تستخدم من قبل خادم أمامي لتتبع البيانات و 'تعقّب' وجهات نظر مستخدم بدون استخدام الكوكيز.

لكي يكون لدينا هذا يمكن التنبؤ به نحن نحدد مجموعة من كونستس في `Consts` -مصنفة. -مصنفة. وهكذا يمكننا أن نختبر ضد الاستجابات والطلبات التي يمكن التنبؤ بها.

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

## اختبار ما بعد الاختبار

هذه فقط البداية لاستراتيجية اختبارنا لـ أمامي. Net، ما زال علينا أن نختبر `IHostedService` واختبار البيانات الفعلية التي تولدها أومامي (التي لا يتم توثيقها في أي مكان ولكن تحتوي على رمز JWT مع بعض البيانات المفيدة.)

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

اذاً سوف نريد ان نختبر لذلك، محاكاة الرمز و من الممكن ان نرجع البيانات عن كل زيارة (كما ستتذكرون ان هذا مصنوع من a `uuid(websiteId,ipaddress, useragent)`).

# في الإستنتاج

هذه مجرد بداية لاختبار حزمة إمامي. نت، هناك الكثير لفعله لكن هذه بداية جيدة. سأضيف المزيد من الإختبارات بينما أذهب و بدون شك سأحسن هذه الإختبارات