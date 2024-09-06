# 利用Umami数据进行网站统计

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-009-05T23:45</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

自博客开始以来, 我的一个计划几乎是渴望追踪有多少用户看我的网站。 为了做到这一点,我用木美 并有一个 [员额的BUNCH](/blog/category/Umami) 使用和设置Umami。 我还有一套“Niget”软件包, 能够追踪ASP.NET核心网站的数据。

现在我添加了一个新的服务, 允许您将数据从 Umami 调回到 C # 应用程序 。 这是一个简单的服务, 使用 Umami API 从您的 Umami 实例中提取数据, 并在您的网站/ 应用程序中使用 。

和往常一样,所有源代码都可以找到 [我的吉他露露,我的吉他露露,我的吉他露露,我的吉他露露,我的吉卜露,我的吉卜露,我的吉卜露,我的吉卜露,我的吉卜露,](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) 用于此站点 。

[技选委

# 安装安装

已经在 Ummami.Net Nuget 软件包中了, 使用以下命令安装 :

```bash
dotnet add package Umami.Net
```

然后,你需要设置服务 在您的 `Program.cs` 文件 :

```csharp
    services.SetupUmamiData(config);
```

此处使用 `Analytics' element from your `appings.json'file:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

在这里 `UmamiScript` 是在 Umami (Umami) 客户端跟踪时使用的脚本[此处见](/blog/usingumamiforlocalanalytics) 如何设置 。 )
缩略 `WebSiteId` 是您在 Umami 实例中创建的网站的 ID 。
`UmamiPath` 您的 Umami 实例路径 。

缩略 `UserName` 和 `Password` 是Umami案的证书(在这种情况下,我使用行政密码)。

# 用法

现在你有了 `UmamiDataService` 在您的服务收藏中,您可以开始使用它!

## 方法方法

方法都来自Umami API定义,
http://umami.is/docs/api/website-stats http://umami.is/docs/api/website-stats https://umami.is/docs/api/website-stats

将所有返回都包裹在 `UmamiResults<T>` 对象具有 `Success` 财产和财产财产及财产财产(a) `Result` 财产。 缩略 `Result` 属性是从 Umami API 返回的对象。

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

除下列所有请求外的所有请求 `ActiveUsers` 具有两个强制特性的基本请求对象。 我将“方便日期时间”添加到基本请求对象,以便更容易确定开始日期和结束日期。

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

本服务有以下方法:

### 活动用户

这正好使网站的当前用户总数达到此数

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### 状态

这将返回有关网站的一堆统计数据, 包括用户数、页面浏览量等 。

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

您可以设置若干参数来过滤从 API 返回的数据。 例如,使用 `url` 返回特定 URL 的统计 。

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
JSON反对Umami的返回如下:

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

这个包裹在我的体内 `StatsResponseModel` 对象。

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

### 计量数

Umami的计量为您提供特定类型属性的视图数量 。

#### 事件事件事件事件

其中一个例子是:

在Umami的“晚上”是特定的东西 你可以在现场追踪到 当使用 Umami. Net 跟踪事件时, 您可以设置一些属性, 这些属性会与事件名称一起被跟踪 。 比如这里,我追踪 `Search` 与 URL 和 搜索 条目一起请求 。

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

要获取此事件的数据, 您将会使用此 `Metrics` 方法 :

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

与本接受的其他方法一样,本 `MetricsRequest` 对象( 强制 `BaseRequest` 属性)和一些用于筛选数据的可选属性。

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
您可以在此看到, 您可以在请求元素中指定一些属性, 以指定要返回的度量 。

您也可以设置 `Limit` 属性以限制返回结果的数量。

例如,为了得到上述我提到的过去一天的事件,你将提出以下请求:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

从API返回的JSON物体如下:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

我再次将这个包在 `MetricsResponseModels` 对象。

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

x 是事件名称, y 是它被触发的次数 。

#### 页面意见和意见

最有用的衡量标准之一是页面浏览量。 这是页面在网站上浏览的次数 。 以下是我用来测试过去30天页面浏览量的测试。 你会注意到 `Type` 参数设置为 `MetricType.url` 然而,这也是默认值, 所以您不需要设置它 。

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

此返回返回 a `MetricsResponse` 具有下列 JSON 结构的物体:

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

何处处 `x` URL 是 URL 和 `y` 是它被查看的次数。

### 页面意见和意见

这将返回特定 URL 页面浏览数 。

我用这个方法来测试:

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

此返回返回 a `PageViewsResponse` 具有下列 JSON 结构的物体:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

何处处 `date` 日期和 `value` 是页面浏览次数,按指定范围(或小时、月等)每天重复使用。 取决于 `Unit` 财产))

与本接受的其他方法一样,本 `PageViewsRequest` 对象( 强制 `BaseRequest` 属性)和一些用于筛选数据的可选属性。

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
与其它方法一样,您可以设置一些属性来过滤从 API 返回的数据,例如,您可以设置
`Country` 属性从特定国家获取页面浏览次数。

# 利用该处

使用此服务获取每个博客页面的浏览量。 在下面的代码中,我需要一个开始和结束日期和一个前缀(即: `/blog` 并获得博客上每一页的浏览量。

然后把数据藏起来一个小时 这样我就不用一直打UmamiAPI了

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

# 在结论结论中

这是一个简单的服务, 允许您从 Umami 提取数据, 并将其用于应用程序 。 我用这个来获得每个博客页面的浏览次数, 但它非常有用 仅仅获得一个BUNCH数据 有关谁使用你的网站 以及他们如何使用它。