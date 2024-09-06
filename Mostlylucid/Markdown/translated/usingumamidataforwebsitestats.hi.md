# वेबसाइट स्टेट्स के लिए उममी डाटा उपयोग किया जा रहा है

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 09-5T23: 45</datetime>

# परिचय

इस ब्लॉग को शुरू करने के बाद से मेरी एक परियोजना यह है कि कितने उपयोक्ता मेरी वेबसाइट पर देखना चाहते हैं। यह करने के लिए मैं उममी का उपयोग करें और एक है [पोस्ट- स्क्रिप्ट](/blog/category/Umami) चारों ओर का उपयोग और उममी स्थापित. मैं भी एक Nuget पैकेज है जो एक uget पैकेज से डेटा को ट्रैक करने के लिए संभव बनाता है. Nuenet कोर वेबसाइट.

अब मैंने एक नई सेवा जोड़ी है जो आपको उममी से डाटा लेने की अनुमति देता है C# अनुप्रयोग में। यह एक सरल सेवा है जो उममी उदाहरण से डेटा खींचने के लिए काम करती है और इसे अपनी वेबसाइट / app.

इस के लिए हमेशा के लिए स्रोत कोड पाया जा सकता है के रूप में [मेरे Githब पर](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) इस साइट के लिए.

[विषय

# संस्थापन

यह पहले से ही उमला है.Net Nuget पैकेज में है, इसे निम्न कमांड के उपयोग से संस्थापित करें:

```bash
dotnet add package Umami.Net
```

तो आप में सेवा सेट करने की जरूरत है `Program.cs` फ़ाइल:

```csharp
    services.SetupUmamiData(config);
```

यह उपयोग करता है `Analytics' element from your `एएसआईएसिस फ़ाइल:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

यहाँ `UmamiScript` स्क्रिप्ट है जो आप उममी में क्लाएंट साइड ट्रैक के लिए प्रयोग करते हैं[यहाँ देखें](/blog/usingumamiforlocalanalytics) कि स्थापित करने के लिए कैसे.
वह `WebSiteId` वेबसाइट के लिए आईडी है जो आप अपने उममी उदाहरण में बनाया है.
`UmamiPath` अपने उममी उदाहरण के लिए पथ है.

वह `UserName` और `Password` उममी उदाहरण के लिए महत्व हैं (इस मामले में मैं प्रशासक पासवर्ड इस्तेमाल करता हूँ).

# उपयोग

अब तुम्हारे पास है `UmamiDataService` अपनी सेवा संग्रह में आप इसका इस्तेमाल शुरू कर सकते हैं!

## विधि

सभी तरीके उममी एपीआई परिभाषा से हैं आप उनके बारे में यहाँ पढ़ सकते हैं:
https://mema.s/lop/ webss

सभी लौटाए गए हैं `UmamiResults<T>` वस्तु जिसमें एक है `Success` गुण और एक `Result` गुण. वह `Result` संपत्ति उममी एपीआई से वापस आ गया है.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

सभी निवेदन इससे अलग हैं `ActiveUsers` दो अनिवार्य गुणों के साथ बेस निवेदन वस्तु है. मैंने बेस निवेदन वस्तु को सुविधाओं के समय जोड़ दिए ताकि शुरू और अंत की तारीख तय करना आसान हो जाए ।

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

सेवा में निम्नलिखित तरीके हैं:

### सक्रिय उपयोक्ता

यह सिर्फ साइट पर CURENVE उपयोक्ताओं की कुल संख्या प्राप्त करता है

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### स्थिति

यह साइट के बारे में बहुत सारे आंकड़े बताता है, जिसमें उपयोक्ताओं की संख्या, पृष्ठ दृश्य इत्यादि सम्मिलित हैं.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

आप कई पैरामीटर्स निर्धारित कर सकते हैं जो डाटा को एपीआई से वापस फ़िल्टर करने के लिए सेट कर सकते हैं. उदाहरण के लिए `url` विशिष्ट यूआरएल के लिए स्टेट्स वापस आएगा.

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
JSON ऑब्जेक्ट उममी का परिणाम होगा जैसा कि चल रहा है.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

यह मेरे अंदर लिपटे है `StatsResponseModel` वस्तु.

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

### मेट्रिक्स

उममी में तूफान आप विशिष्ट प्रकार के गुणों के लिए दृश्यों की संख्या प्रदान करते हैं.

#### घटनाएँ

इनमें से एक है घटना:

उममी में'घटना' विशिष्ट वस्तुएँ हैं जिन्हें आप किसी साइट पर ट्रैक कर सकते हैं. उममी का उपयोग करते वक्‍त आप कई गुण निर्धारित कर सकते हैं जो घटना नाम के साथ ट्रैक किया जाता है. उदाहरण के लिए यहाँ मैं ट्रैक के लिए `Search` यूआरएल तथा खोज वाक्यांश के साथ निवेदन.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

इस घटना के बारे में आँकड़ा लाने के लिए जिसे आप प्रयोग करते हैं `Metrics` विधि:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

अन्य तरीक़ों से यह स्वीकार करता है `MetricsRequest` वस्तु (इस आवश्यक के साथ) `BaseRequest` गुण और एक वैकल्पिक गुण का आंकड़ा को फिल्टर करने के लिए.

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
यहाँ आप देख सकते हैं कि आप निवेदन तत्व में कितने गुण निर्दिष्ट कर सकते हैं यह निर्धारित करने के लिए कि आप क्या शिकायत करना चाहते हैं

आप भी एक सेट कर सकते हैं `Limit` परिणाम की संख्या सीमित करने के लिए गुण लौटा.

उदाहरण के लिए, पिछले दिन हुई घटना को लेकर मैंने ऊपर ज़िक्र किया था कि आप इस गुज़ारिश का इस्तेमाल करेंगे:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

JSON ऑब्जेक्ट एपीआई से वापस आ गया है के रूप में:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

और फिर मैं इसे अपने में लपेट `MetricsResponseModels` वस्तु.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

x घटना नाम और y कहाँ है यह उत्पन्न किया गया है की संख्या है.

#### पृष्ठ दृश्य

एक बहुत ही उपयोगी वायरसों में से एक पृष्ठ दृष्टिकोण की संख्या है. यह साइट पर कई बार एक पृष्ठ को देखा गया है । नीचे परीक्षा है कि मैं पिछले 30 दिनों में पृष्ठ दृश्य की संख्या प्राप्त करने के लिए प्रयोग करता हूँ. आप नोट करेंगे `Type` पैरामीटर को बतौर सेट किया गया है `MetricType.url` हालांकि यह भी डिफ़ॉल्ट मान है तो आपको इसे सेट करने की आवश्यकता नहीं है.

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

यह एक लौटाता है `MetricsResponse` वस्तु जो निम्न JSON स्ट्रक्चर है:

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

कहाँ `x` यूआरएल है और `y` यह संख्या कई बार देखी गयी है ।

### पृष्ठ- व्यू

यह विशिष्ट यूआरएल के लिए पृष्ठ दृश्यों की संख्या बताता है.

फिर यहाँ एक जांच है मैं इस विधि के लिए उपयोग में:

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

यह एक लौटाता है `PageViewsResponse` वस्तु जो निम्न JSON स्ट्रक्चर है:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

कहाँ `date` तारीख़ और समय है `value` पृष्ठ दृश्य की संख्या है, यह निर्धारित सीमा (या घंटे, महीने, आदि) में प्रत्येक दिन के लिए दोहराया जाता है. फिर पर निर्भर करें `Unit` गुण.

अन्य तरीक़ों से यह स्वीकार करता है `PageViewsRequest` वस्तु (इस आवश्यक के साथ) `BaseRequest` गुण और एक वैकल्पिक गुण का आंकड़ा को फिल्टर करने के लिए.

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
अन्य तरीकों के साथ आप डाटा को एपीआई से फ़िल्टर करने के लिए कई गुणों को सेट कर सकते हैं, उदाहरण के लिए आप नियत कर सकते हैं
`Country` किसी विशिष्ट देश से पृष्ठ दृश्यों की संख्या प्राप्त करने के लिए संपत्ति.

# सेवा का इस्तेमाल

इस साइट में मेरे पास कुछ कोड हैं जो मुझे इस सेवा का उपयोग करते हैं प्रत्येक ब्लॉग पृष्ठ की संख्या प्राप्त करने के लिए। नीचे दिए गए कोड में मैं प्रारंभ और अंत तारीख और एक उपसर्ग ले जाता है (जो है) `/blog` मेरे मामले में और ब्लॉग में प्रत्येक पृष्ठ के लिए विचारों की संख्या प्राप्त करें.

तो मैं एक घंटे के लिए इस डेटा को कैश तो मुझे उममी एपीआई को मारने की जरूरत नहीं है.

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

# ऑन्टियम

यह एक सरल सेवा है जो आपको उममी से डाटा खींचने और इसे अपने अनुप्रयोग में इस्तेमाल करने देता है. मैं हर ब्लॉग पृष्ठ के दृश्यों की संख्या प्राप्त करने के लिए इसका इस्तेमाल करता हूँ और इसे पृष्ठ पर प्रदर्शित करता हूँ. लेकिन यह सिर्फ अपने साइट का उपयोग करने के बारे में डेटा के बारे में डेटा के लिए बहुत उपयोगी है जो के लिए और कैसे वे इसका उपयोग करते हैं.