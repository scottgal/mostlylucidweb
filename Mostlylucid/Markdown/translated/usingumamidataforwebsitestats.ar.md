# استخدام بيانات الأمّطري للمواقع الشبكية

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05-T23: 45</datetime>

# أولاً

أحد مشاريعي منذ بدء هذه المدونات هو رغبة مهووسة تقريباً لتعقب عدد المستخدمين الذين ينظرون إلى موقعي الإلكتروني. لفعل هذا أنا أستخدم "أُوميامي" ولدي [BUNUNTUNTS الوظائف](/blog/category/Umami) حول استخدام وتأسيس أومامي. ولدي أيضاً حزمة من النوغيت تجعل من الممكن تتبع البيانات من موقع ASP.NET الأساسي على شبكة الإنترنت.

الآن قمت بإضافة خدمة جديدة تسمح لك بسحب البيانات مرة أخرى من أومامي إلى تطبيق C#. هذه خدمة بسيطة تستخدم AOMAMI API لسحب البيانات من نموذج أومامي الخاص بك واستخدامها على موقعك/تطبيقك.

كالعادة كلّ رمز المصدر لهذا يمكن العثور عليه [على حسابي](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) لهذا الموقع

[رابعاً -

# عدد أفراد

هذا موجود بالفعل في حزمة Umami. Nuget. Nuget ، تثبيتها باستخدام الأمر التالي:

```bash
dotnet add package Umami.Net
```

ثم تحتاج إلى إنشاء الخدمة في `Program.cs` 

```csharp
    services.SetupUmamiData(config);
```

هذا استخدامات `Analytics' element from your `تَعِينات. Jjson ملفّ:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

(هنا) `UmamiScript` هو النص الذي تستخدمه في تتبع جانب العميل في أمامي (Omamimi) (Amamimi)[انظر هنا](/blog/usingumamiforlocalanalytics) لِكي يَضِعَ ذلك فوق
الـ `WebSiteId` هو هوية الموقع الذي صنعته في موقعك على الإنترنت.
`UmamiPath` هو الطريق إلى مرحلة الإمامة الخاصة بك.

الـ `UserName` وقد عقد مؤتمراً بشأن `Password` (في هذه الحالة أستخدم كلمة السر الإدارية).

# 

الآن لديك `UmamiDataService` في مجموعتك الخدمية يمكنك البدء في استخدامها!

## المحتويات (بالأسبان)

الأساليب كلها من تعريف AOMAmi API يمكنك أن تقرأ عنها هنا:
https://umami.is/docs/api/wpsite-stats

الكل الكل الكل ترجع هو ملفق بوصة a `UmamiResults<T>` الذي لـه `Success` وممتلكات وممتلكات `Result` (أ) أو الممتلكات الخاصة بها. الـ `Result` هو الشيء الذي أُعيد من AMAMI API.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

جميع الطلبات باستثناء `ActiveUsers` لـ a أساس طلب هدف مع إثنان من إجباري خاصّ. أضفت تاريخ الملاءمة إلى عنصر طلب القاعدة لجعله أسهل في تحديد تاريخي البداية والنهاية.

```csharp
public class BaseRequest
{
    [QueryStringParameter("startAt", isRequired: true)]
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    [QueryStringParameter("endAt", isRequired: true)]
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
```

وفيما يلي الطرق التي تتبعها الدائرة:

### المجلدات

هذا فقط يحصل على العدد الإجمالي للمستخدمين النشطين في الموقع

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### 

هذه ترجع مجموعة من الإحصائيات عن الموقع، بما في ذلك عدد المستخدمين، ومشاهدات الصفحات، وما إلى ذلك.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

يمكنك تعيين عدد من البارامترات لتنقية البيانات المسترجعة من API. على سبيل المثال استخدام `url` سيعيد الاحصائيات لـ a محدّد URL.

<details>
<summary>StatsRequest object</summary>
```csharp
public class StatsRequest : BaseRequest
{
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
وفيما يلي بيان بعودة جسم JSON Omamami.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

هذا ملفوف داخل `StatsResponseModel` (أ) الهدف من الهدف.

```csharp
namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class StatsResponseModels
{
    public Pageviews pageviews { get; set; }
    public Visitors visitors { get; set; }
    public Visits visits { get; set; }
    public Bounces bounces { get; set; }
    public Totaltime totaltime { get; set; }


    public class Pageviews
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visitors
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visits
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Bounces
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Totaltime
    {
        public int value { get; set; }
        public int prev { get; set; }
    }
}
```

### 

تقدم لكم القياسات في أمامي عدد المشاهدات لأنواع معينة من الخصائص.

#### أحداث أحداث

ومن الأمثلة على هذه الأحداث ما يلي:

"الاختراعات" في أومامي هي عناصر محددة يمكنك تتبعها في موقع ما. عند تتبع الأحداث باستخدام إمامي. Net يمكنك تعيين عدد من الخصائص التي يتم تتبعها مع اسم الحدث. على سبيل المثال هنا أتتبع `Search` مع URL و البحث مصطلح.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

إلى جلب بيانات عن هذا الحدث سوف تستخدم `Metrics` طريقة:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

كما هو الحال بالنسبة للأساليب الأخرى التي تقبل `MetricsRequest` (مع: `BaseRequest` (أ) عدد من الخواص الاختيارية لفرز البيانات.

<details>
<summary>MetricsRequest object</summary>
```csharp
public class MetricsRequest : BaseRequest
{
    [QueryStringParameter("type", isRequired: true)]
    public MetricType Type { get; set; } // Metrics type

    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
    
    [QueryStringParameter("language")]
    public string? Language { get; set; } // Name of language
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("limit")]
    public int? Limit { get; set; } = 500; // Number of events returned (default: 500)
}
```

</details>
هنا يمكنك أن ترى أنه يمكنك تحديد عدد من الخواص في عنصر الطلب إلى تحديد ما هي القياسات التي تريد أن ترجعها.

يمكنك أيضاً تعيين a `Limit` (ب) الحد من عدد النتائج التي أُعيدت.

وعلى سبيل المثال، للحصول على الحدث خلال اليوم الماضي الذي ذكرته أعلاه، سوف تستخدمون الطلب التالي:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

الجسم الذي أُعيد إلى جسم Json من API هو كما يلي:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

ومرة أخرى أَلْفُ هذا في `MetricsResponseModels` (أ) الهدف من الهدف.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

حيث x هو اسم الحدث و y هو عدد المرات التي تم تحريكها.

#### المحتويات

ومن أكثر المقاييس فائدة عدد مرات الاطلاع على الصفحات. هذا هو عدد المرات التي تم فيها مشاهدة صفحة في الموقع. تحت الاختبار الذي أستخدمه للحصول على عدد الصفحات المشاهدة على مدى الـ30 يوما الماضية. ستلاحظين `Type` مُسند `MetricType.url` وهذا ايضاً قيمة افتراضية لذا لا تحتاج الى وضعها.

```csharp
  [Fact]
    public async Task Metrics_StartEnd()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        
        var metrics = await websiteDataService.GetMetrics(new MetricsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-30),
            EndAtDate = DateTime.Now,
            Type = MetricType.url,
            Limit = 500
        });
        Assert.NotNull(metrics);
        Assert.Equal( HttpStatusCode.OK, metrics.Status);

    }
```

هذا الدالة `MetricsResponse` جسم له هيكل JOSON التالي:

```json
[
  {
    "x": "/",
    "y": 1
  },
  {
    "x": "/blog",
    "y": 1
  },
  {
    "x": "/blog/usingumamidataforwebsitestats",
    "y": 1
  }
]
```

المكان `x` هو URL و `y` هو عدد المرات التي تم النظر فيها.

### الصفحة

هذا يعود عدد مَزْرَجات الصفحة لـ a URL.

هنا عبارة عن اختبار استخدمه لهذه الطريقة:

```csharp
    [Fact]
    public async Task PageViews_StartEnd_Day_Url()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
    
        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.day,
            Url = "/blog"
        });
        Assert.NotNull(pageViews);
        Assert.Equal( HttpStatusCode.OK, pageViews.Status);

    }
```

هذا الدالة `PageViewsResponse` جسم له هيكل JOSON التالي:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

المكان `date` هو التاريخ و `value` (أ) عدد مرات الاطلاع على الصفحات، ويعاد هذا العدد لكل يوم في المدى المحدد (أو الساعة، أو الشهر، وما إلى ذلك). الاعتماد على `Unit` (ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه‍) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه) و(ه( و(ه) و(ه) و(ه( و(ه( و(ه() و(ه((

كما هو الحال بالنسبة للأساليب الأخرى التي تقبل `PageViewsRequest` (مع: `BaseRequest` (أ) عدد من الخواص الاختيارية لفرز البيانات.

<details>
<summary>PageViewsRequest object</summary>
```csharp
public class PageViewsRequest : BaseRequest
{
    // Required properties

    [QueryStringParameter("unit", isRequired: true)]
    public Unit Unit { get; set; } = Unit.day; // Time unit (year | month | hour | day)
    
    [QueryStringParameter("timezone")]
    [TimeZoneValidator]
    public string Timezone { get; set; }

    // Optional properties
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
كما في الطرائق الأخرى، يمكنك تعيين عدد من خادوم إلى مرشح البيانات المسترجعة من الـ AFI، على سبيل المثال يمكنك تعيين
`Country` للحصول على عدد زيارات الصفحات من بلد معين.

# مستعملة الخدمة

في هذا الموقع لدي بعض الرموز التي تسمح لي باستخدام هذه الخدمة للحصول على عدد المشاهدات لكل صفحة مدونة. في الكود الأسفل أدناه أنا آخذ تاريخ بداية ونهاية و تاريخ بداية و نهاية و a سابقة (الذي هو `/blog` في حالتي) والحصول على عدد المشاهدين لكل صفحة في المدونة.

ثم أخفي هذه البيانات لمدة ساعة حتى لا أضطر إلى الاستمرار في ضرب AMAMI API.

```csharp
public class UmamiDataSortService(
    UmamiDataService dataService,
    IMemoryCache cache)
{
    public async Task<List<MetricsResponseModels>?> GetMetrics(DateTime startAt, DateTime endAt, string prefix="" )
    {
        using var activity = Log.Logger.StartActivity("GetMetricsWithPrefix");
        try
        {
            var cacheKey = $"Metrics_{startAt}_{endAt}_{prefix}";
            if (cache.TryGetValue(cacheKey, out List<MetricsResponseModels>? metrics))
            {
                activity?.AddProperty("CacheHit", true);
                return metrics;
            }
            activity?.AddProperty("CacheHit", false);
            var metricsRequest = new MetricsRequest()
            {
                StartAtDate = startAt,
                EndAtDate = endAt,
                Type = MetricType.url,
                Limit = 500
            };
            var metricRequest = await dataService.GetMetrics(metricsRequest);

            if(metricRequest.Status != HttpStatusCode.OK)
            {
                return null;
            }
            var filteredMetrics = metricRequest.Data.Where(x => x.x.StartsWith(prefix)).ToList();
            cache.Set(cacheKey, filteredMetrics, TimeSpan.FromHours(1));
            activity?.AddProperty("MetricsCount", filteredMetrics?.Count()?? 0);
            activity?.Complete();
            return filteredMetrics;
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
         
            return null;
        }
    }

```

# في الإستنتاج

هذه خدمة بسيطة تسمح لك بسحب البيانات من أومامي واستخدامها في طلبك. أنا أستخدم هذا للحصول على عدد المشاهدين لكل صفحة مدونة وعرضها على الصفحة. لكنه مفيد جدا للحصول على مجموعة من البيانات عن من يستخدم موقعك وكيف يستخدمونه.