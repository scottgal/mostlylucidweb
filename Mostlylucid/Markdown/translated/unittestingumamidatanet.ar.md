# (أ) وحدة اختبار وحدة اختبار أمّامي.

# أولاً

في الجزء السابق من هذه السلسلة حيث قمت باختبار[ طرق التتبع ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04-TT20:30</datetime>
[TOC]

## المشكلة

في الجزء السابق استخدمت (موك) لإعطائي `Mock<HttpMessageHandler>` وارجع العجل المستخدم في `UmamiClient`هذا نمط شائع عند اختبار شفر `HttpClient`/ / / / في هذا المنصب سأريكم كيف تختبرون الجديد `UmamiDataService` بدون استخدام معقّد.

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

## لماذا تستخدم "مُق" ؟

Moq a مكتبة إلى إ_ نشئ لـ و. وهو يستخدم على نطاق واسع في اختبار الوحدة لعزل الرمز قيد الاختبار من المعتمدات. ومع ذلك، هناك بعض الحالات التي يمكن أن يكون فيها استخدام العقار مرهقاً أو حتى مستحيلاً. فعلى سبيل المثال، عند اختبار الشفرة التي تستخدم طرقاً ساكنة أو عندما تكون الشفرة قيد الاختبار مرتبطة ارتباطاً وثيقاً باعتماداتها.

المثال الذي أعطيته أعلاه يعطي الكثير من المرونة في اختبار `UmamiClient` لكن أيضاً لديه بعض المساوئ إنها شفرة قبيحة وتقوم بالكثير من الأشياء التي لا أحتاجها حقاً اذاً متى اختبار `UmamiDataService` قررت أن أجرب نهجاً مختلفاً

# شهادة الاختبار

الـ `UmamiDataService` هو إضافة مستقبلية إلى مكتبة أومامي. Net التي ستسمح لك بجلب البيانات من أومامي لأشياء مثل رؤية عدد المشاهدات التي حصلت على الصفحة، ما هي الأحداث التي حدثت من نوع معين، هذه قوة قوية جداً لكن الآن [AMOmi API يعمل فقط من خلال جافScrubt](https://umami.is/docs/api/website-stats)/ / / / لذا أريد أن ألعب مع تلك البيانات التي مررت بها من خلال جهد إنشاء عميل C# لذلك.

الـ `UmamiDataService` (الطرائق طويلة) على سبيل المثال هنا على سبيل المثال هنا `PageViews` من الناحية العملية.

يمكنك أن ترى أن الكثير من الكود هو بناء عملية الفرز من تمرير في فئة Page Pethern Pechnology Prequest (هناك طرق أخرى للقيام بذلك ولكن هذا، على سبيل المثال، باستخدام الخصائص أو الانعكاس يعمل هنا).

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
كما يمكنك أن ترى هذا حقاً فقط نشئ سلسلة إقتراح. يوثق هذا النداء (انظر: [المادة 4 من المادة 4](/blog/unittestinglogginginaspnetcore) للحصول على بعض التفاصيل عن هذا) وبعد ذلك إجراء مكالمة إلى Amamami API. إذاً كيف نختبر هذا؟

## اختبار خدمة بيانات الأمومة

على النقيض من اختبار imamimic conliminent، قررت أن أختبر `UmamiDataService` بدون استخدام معقّد. بدلاً من ذلك، أنا خلقت بسيطاً `DelegatingHandler` الصف الذي يسمح لي باستجواب الطلب ثم إعادة الرد. هذا نهج أبسط بكثير من استخدام (موق) و يسمح لي بإختبار `UmamiDataService` « بغير » لا « للظ بما لا » لا لا لا لا لا لا لا `HttpClient`.

في الشفرة تحت الشفرة يمكنك أن ترى أنني ببساطة امتداد `DelegatingHandler` و التجاوز الـ `SendAsync` من الناحية العملية. وهذه الطريقة تسمح لي بفحص الطلب وإعادة الرد بناء على الطلب.

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

## إنشاء

من أجل إقامة نظام جديد جديد `UmamiDataService` ان استخدام هذا المعالج هو امر بسيط ايضا.

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

سترين أنّي وضعت للتوّ `ServiceCollection`يضاف ما يلي: `FakeLogger<T>` (أرى مرة أخرى [وللاطلاع على تفاصيل هذا الأمر](/blog/unittestinglogginginaspnetcore) ثم يُقَرَّر ثم يُقَرَّر `UmamiData` الخدمة مع اسم المستخدم و كلمة السر التي أريد استخدامها (حتى أتمكن من اختبار الفشل).

ثمّ أَدْفُ إلى `services.SetupUmamiData(username, password);` الذي هو طريق مُمَد خلقتُه لإنشاء `UmamiDataService` مع أن `UmamiDataDelegatingHandler` وقد عقد مؤتمراً `AuthService`;

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

يمكنك أن ترى أن هذا هو المكان الذي أعلق فيه في `UmamiDataDelegatingHandler` وقد عقد مؤتمراً `AuthService` - - - - - - - - - - `UmamiDataService`/ / / / الطريقة التي يُنظّم بها هذا هو أن `AuthService` 'الممتلكات` `HttpClient` وقد عقد مؤتمراً `UmamiDataService` (أ) `AuthService` لإجراء المكالمات إلى AMAMAI API مع `bearer` (ب) وتكيـر و `BaseAddress` تم تعيينها مسبقاً.

## الإخت الإختبارات

هذا يجعل في الواقع اختبار هذا بسيطاً جداً. هو فقط a قليلاً فضّال كما أردتُ أيضاً إلى إختبار قطع الأشجار أيضاً. كل ما يفعله هو أن يُنشر من خلال `DelegatingHandler` و أنا أقوم بمحاكاة الرد بناءً على الطلب

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

### & مُموح الإيجاب

لمحاكاة الرد لهذه الطريقة، ستتذكرون أن لدي هذا الخط في `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

كل ما يفعله هذا هو سحب المعلومات من الاستعلام وبناء استجابة "حقيقية" (استناداً إلى الاختبارات الحية التي جمعتها، مرة أخرى القليل جداً جداً من الدساتير على هذا). سترى أني سأختبر عدد الأيام بين تاريخ البدء والنهاية ثم أعيد الرد بنفس عدد الأيام

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

# في الإستنتاج

اذاً انه في الواقع انه من السهل جداً ان تختبر `HttpClient` طلب بدون استخدام (موق) وأعتقد أن المكان أكثر نظافة بهذه الطريقة أنت تخسر بعضاً من التطور الذي أمكن تحقيقه في (موك) لكن لإختبارات بسيطة كهذه، أعتقد أنها مقايضة جيدة.