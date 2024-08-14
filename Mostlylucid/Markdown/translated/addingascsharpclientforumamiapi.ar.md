# إضافة حرف C# عميل لـ Amami AP

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14TT01: 27</datetime>

## أولاً- مقد مقدماً

في هذا المقال، سأريكم كيفية إنشاء عميل C# لـ Amamami يبلغ عن API. وهذا مثال بسيط يوضح كيفية التحقق من صحة هذا المؤشر واسترجاع البيانات منه.

يمكنك العثور على كل شفرة المصدر لهذا [على بلدي GitHub repo](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[رابعاً -

## النفقات قبل الاحتياجات

-تثبيت أُمّيّة. يمكنك العثور على أمر تثبيت [هنا هنا](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) هذه تفاصيل كيف أقوم بتركيب واستخدام أومامي لتوفير التحليل لهذا الموقع.

وهذا، مرة أخرى، تنفيذ بسيط لعدد قليل من النقاط النهائية لمؤشرات قياس الأثر في موقع أومامي على شبكة الإنترنت. يمكنك العثور على وثائق الـ مستند [هنا هنا](https://umami.is/docs/api/website-stats).

وقد اخترت في هذا الصدد تنفيذ النقاط التالية:

- `GET /api/websites/:websiteId/pageviews` - كما يقترح الاسم، هذه نقطة النهاية ترجع الصفحات و 'الجلسات' لموقع إلكتروني معين على مدى فترة زمنية.

```json
{
  "pageviews": [
    { "x": "2020-04-20 01:00:00", "y": 3 },
    { "x": "2020-04-20 02:00:00", "y": 7 }
  ],
  "sessions": [
    { "x": "2020-04-20 01:00:00", "y": 2 },
    { "x": "2020-04-20 02:00:00", "y": 4 }
  ]
}
```

- `GET /api/websites/:websiteId/stats` - هذا يُرجعُ الإحصاءات الأساسية لa موقع إلكتروني مُعَيَّن.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - هذا يرجع القياسات لـ a موقع إلكتروني مُعْطَل bu URL...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

كما ترون من الدساتير، هذه كلها تقبل عدداً من البارامترات (وقد مثلت هذه كمعاملات استعلام في الرمز أدناه).

## في الاختبار

دائماً أبدأ بإختبار API في عميل رايدر الموجود في HTTP. هذا يسمح لي بإختبار الـ API بسرعة ورؤية الإستجابة.

```http
### Login Request and Store Token
POST https://{{umamiurl}}/api/auth/login
Content-Type: application/json

{
  "username": "{{username}}",

  "password": "{{password}}"
}
> {% client.global.set("auth_token", response.body.token);
    client.global.set("endAt", Math.round(new Date().getTime()).toString() );
    client.global.set("startAt", Math.round(new Date().getTime() - 7 * 24 * 60 * 60 * 1000).toString());
%}


### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/stats?endAt={{endAt}}&startAt={{startAt}}
Authorization: Bearer {{auth_token}}

### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/pageviews?endAt={{endAt}}&startAt={{startAt}}&unit=day
Authorization: Bearer {{auth_token}}


###
GET https://{{umamiurl}}}}/api/websites/{{websiteid}}/metrics?endAt={{endAt}}&startAt={{startAt}}&type=url
Authorization: Bearer {{auth_token}}
```

من الممارسات الجيدة إبقاء الأسماء المتغيرة هنا في `{{}}` a ملفّ إلى أسفل.

```json
{
  "local": {
    "umamiurl":"umamilocal.mostlylucid.net",
    "username": "admin",
    "password": "<password{>",
    "websiteid" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  }
}
```

## إنشاء

أولاً نحتاج إلى ضبط HttpClient والخدمات التي سنستخدمها لتقديم الطلبات.

```csharp

public static class UmamiSetup
{
    public static void SetupUmamiServices(this IServiceCollection services, IConfiguration config)
    {
        var umamiSettings = services.ConfigurePOCO<UmamiSettings>(config.GetSection(UmamiSettings.Section));
        services.AddHttpClient<AuthService>(options =>
        {
            options.BaseAddress = new Uri(umamiSettings.BaseUrl);
            
        }) .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy());;
        services.AddScoped<UmamiService>();
        services.AddScoped<AuthService>();

    }
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

}
```

نحن هنا `UmamiSettings` ويضاف ما يلي: `AuthService` وقد عقد مؤتمراً بشأن `UmamiService` (بدولارات الولايات المتحدة) ونضيف أيضاً سياسة إعادة صياغة لـ HttpClient للتعامل مع الأخطاء العابرة.

يجب علينا أن ننشئ `UmamiService` وقد عقد مؤتمراً بشأن `AuthService` -الفصول الدراسية.

الـ `AuthService` هو ببساطة مسؤول عن الحصول على رمز JWT من API.

```csharp
public class AuthService(HttpClient httpClient, UmamiSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token;
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

لدينا هنا طريقة بسيطة `LoginAsync` أن يرسل طلباً إلى `/api/auth/login` نقطة النهاية مع اسم المستخدم و كلمة السر. إذا كان الطلب ناجحاً، نخزن رمز JWT في `_token` فـي حقـوق `Authorization` رأساً على الـ HttpClient.

الـ `UmamiService` يكون مسؤولاً عن تقديم الطلبات إلى البرنامج.
لكل من الطرق الرئيسية التي قمت بتعريفها لأشياء الطلب التي تقبل جميع البارامترات لكل نقطة نهاية. وهذا يجعل من الأسهل اختبار وصيانة الشفرة.

جميعهم يتبعون نمطاً متزامناً، لذا سأريكم واحداً منهم هنا.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStatsAsync(StatsRequest statsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={statsRequest.StartAt}",
        $"end={statsRequest.EndAt}"
    };

    // Add optional parameters if they are not null
    if (!string.IsNullOrEmpty(statsRequest.Url)) queryParams.Add($"url={statsRequest.Url}");
    if (!string.IsNullOrEmpty(statsRequest.Referrer)) queryParams.Add($"referrer={statsRequest.Referrer}");
    if (!string.IsNullOrEmpty(statsRequest.Title)) queryParams.Add($"title={statsRequest.Title}");
    if (!string.IsNullOrEmpty(statsRequest.Query)) queryParams.Add($"query={statsRequest.Query}");
    if (!string.IsNullOrEmpty(statsRequest.Event)) queryParams.Add($"event={statsRequest.Event}");
    if (!string.IsNullOrEmpty(statsRequest.Host)) queryParams.Add($"host={statsRequest.Host}");
    if (!string.IsNullOrEmpty(statsRequest.Os)) queryParams.Add($"os={statsRequest.Os}");
    if (!string.IsNullOrEmpty(statsRequest.Browser)) queryParams.Add($"browser={statsRequest.Browser}");
    if (!string.IsNullOrEmpty(statsRequest.Device)) queryParams.Add($"device={statsRequest.Device}");
    if (!string.IsNullOrEmpty(statsRequest.Country)) queryParams.Add($"country={statsRequest.Country}");
    if (!string.IsNullOrEmpty(statsRequest.Region)) queryParams.Add($"region={statsRequest.Region}");
    if (!string.IsNullOrEmpty(statsRequest.City)) queryParams.Add($"city={statsRequest.City}");

    // Combine the query parameters into a query string
    var queryString = string.Join("&", queryParams);

    // Make the HTTP request
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/stats?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<StatsResponseModels>();
        return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new StatsResponseModels());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetStatsAsync(statsRequest);
    }

    logger.LogError("Failed to get stats");
    return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get stats", null);
}

```

يمكنك هنا أن ترى أني آخذ كائن الطلب

```csharp
public class BaseRequest
{
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
public class StatsRequest : BaseRequest
{
    // Optional properties
    public string? Url { get; set; } // Name of URL
    public string? Referrer { get; set; } // Name of referrer
    public string? Title { get; set; } // Name of page title
    public string? Query { get; set; } // Name of query
    public string? Event { get; set; } // Name of event
    public string? Host { get; set; } // Name of hostname
    public string? Os { get; set; } // Name of operating system
    public string? Browser { get; set; } // Name of browser
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    public string? Country { get; set; } // Name of country
    public string? Region { get; set; } // Name of region/state/province
    public string? City { get; set; } // Name of city
}
```

وابني سلسلة الاهمية من هذه المعاملات. إذا كان الطلب ناجحاً، نرجع المحتوى كـ `UmamiResult` (أ) الهدف من الهدف. إذا فشل الطلب برمز حالة 401 سنتصل بـ `LoginAsync` ويعاد تقديم الطلب من جديد. هذا يضمن أننا "بكلّ تأكيد" نتعامل مع النهاية الرمزية.

## ثالثاً - استنتاج

هذا مثال بسيط لكيفية إنشاء عميل C# لـ Amamami API. يمكنك استخدام هذا كنقطة بداية لبناء عملاء أكثر تعقيداً أو دمج API في تطبيقاتك الخاصة.