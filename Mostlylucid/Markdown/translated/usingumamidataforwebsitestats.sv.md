# Använda Umami data för webbplats statistik

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">Förbehåll IIIA-PT-38</datetime>

# Inledning

Ett av mina projekt sedan jag startade den här bloggen är en nästan besatt önskan att spåra hur många användare som tittar på min hemsida. För att göra detta använder jag Umami och har en [BUNCH AV TJÄNSTER](/blog/category/Umami) kring att använda och sätta upp Umami. Jag har också ett Nuget-paket som gör det möjligt att spåra data från en ASP.NET Core-webbplats.

Nu har jag lagt till en ny tjänst som gör att du kan dra data tillbaka från Umami till en C# ansökan. Detta är en enkel tjänst som använder Umami API för att dra data från din Umami instans och använda den på din webbplats / app.

Som vanligt kan all källkod för detta hittas. [på min GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) för denna webbplats.

[TOC]

# Anläggning

Detta är redan i Ummami.Net Nuget-paketet, installera det med följande kommando:

```bash
dotnet add package Umami.Net
```

Du måste sedan ställa in tjänsten i din `Program.cs` fil:

```csharp
    services.SetupUmamiData(config);
```

Detta använder sig av `Analytics' element from your `appsettings.json" fil:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Här... `UmamiScript` är skriptet du använder för klientsidan spårning i Umami ([Se här](/blog/usingumamiforlocalanalytics) för hur man ställer in det ).
I detta sammanhang är det viktigt att se till att `WebSiteId` är ID för webbplatsen du skapade i din Umami instans.
`UmamiPath` är vägen till din Umami instans.

I detta sammanhang är det viktigt att se till att `UserName` och `Password` är referenser för Umami instans (i detta fall använder jag Admin lösenordet).

# Användning

Nu har du rätt. `UmamiDataService` i din tjänstesamling kan du börja använda den!

## Metoder

Metoderna är alla från Umami API definition kan du läsa om dem här:
Kommissionens genomförandeförordning (EU) nr 668/2014 av den 13 juni 2014 om tillämpningsföreskrifter för Europaparlamentets och rådets förordning (EU) nr 1151/2012 om kvalitetsordningar för jordbruksprodukter och livsmedel (EUT L 179, 19.6.2014, s. 1).

Alla returer är insvepta i en `UmamiResults<T>` objekt som har en `Success` egendom och a `Result` egendom. I detta sammanhang är det viktigt att se till att `Result` egenskap är objektet returneras från Umami API.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Alla förfrågningar förutom `ActiveUsers` ha ett grundförfrågningsobjekt med två obligatoriska egenskaper. Jag lade bekvämlighet DateTimes till basförfrågan objekt för att göra det lättare att ställa in start-och slutdatum.

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

Tjänsten har följande metoder:

### Aktiva användare

Detta får bara det totala antalet CURRENT aktiva användare på webbplatsen

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Statistik

Detta returnerar en massa statistik om webbplatsen, inklusive antalet användare, sidvyer, etc.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Du kan ställa in ett antal parametrar för att filtrera de data som returneras från API:et. Till exempel att använda `url` returnerar statistiken för en specifik URL.

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
JSON-objektet Umami återvänder enligt följande.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Den här är inlindad i min `StatsResponseModel` motsätter sig detta.

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

### Tröskelvärden

Metrics i Umami ger dig antalet vyer för specifika typer av egenskaper.

#### Evenemang

Ett exempel på detta är Evenemang."

'Evenemang' i Umami är specifika objekt som du kan spåra på en webbplats. När du följer händelser med Umami.Net kan du ställa in ett antal egenskaper som spåras med händelsenamnet. Till exempel här jag spårar `Search` förfrågningar med webbadressen och sökordet.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

För att hämta data om denna händelse skulle du använda `Metrics` Metod:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Som med de andra metoderna accepterar detta `MetricsRequest` Föremål (med det obligatoriska `BaseRequest` egenskaper) och ett antal valfria egenskaper för att filtrera data.

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
Här kan du se att du kan ange ett antal egenskaper i begäran element för att ange vilka mått du vill returnera.

Du kan också ställa in en `Limit` egendom för att begränsa antalet returnerade resultat.

Till exempel för att få händelsen under den senaste dagen som jag nämnde ovan skulle du använda följande begäran:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

JSON-objektet som returneras från API:et är följande:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Och igen jag lindar in detta i min `MetricsResponseModels` motsätter sig detta.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Där x är händelsens namn och y är antalet gånger den har utlösts.

#### Sidvyer

En av de mest användbara måtten är antalet sidvyer. Detta är antalet gånger en sida har setts på webbplatsen. Nedan är testet jag använder för att få antalet sidvyer under de senaste 30 dagarna. Du kommer att notera `Type` Parametern är inställd som `MetricType.url` men detta är också standardvärdet så att du inte behöver ställa in det.

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

Detta returnerar en `MetricsResponse` objekt som har följande JSON struktur:

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

där `x` är webbadressen och `y` är det antal gånger man har sett det.

### Sidvyer

Detta returnerar antalet sidvyer för en specifik URL.

Återigen här är ett test jag använder för denna metod:

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

Detta returnerar en `PageViewsResponse` objekt som har följande JSON struktur:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

där `date` är datum och `value` är antalet sidvyer, detta upprepas för varje dag i det angivna intervallet (eller timme, månad, etc.). beroende på `Unit` egendom).

Som med de andra metoderna accepterar detta `PageViewsRequest` Föremål (med det obligatoriska `BaseRequest` egenskaper) och ett antal valfria egenskaper för att filtrera data.

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
Som med de andra metoderna kan du ställa in ett antal egenskaper för att filtrera data som returneras från API, till exempel kan du ställa in
`Country` egendom för att få antalet sidvyer från ett visst land.

# Att använda tjänsten

På denna webbplats har jag några koder som låter mig använda denna tjänst för att få antalet vyer varje blogg sida har. I koden nedan tar jag ett start- och slutdatum och ett prefix (som är `/blog` i mitt fall) och få antalet vyer för varje sida i bloggen.

Jag cache denna data för en timme så jag inte behöver fortsätta att slå Umami API.

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

# Slutsatser

Detta är en enkel tjänst som gör att du kan dra data från Umami och använda den i din ansökan. Jag använder detta för att få antalet vyer för varje bloggsida och visa den på sidan. Men det är mycket användbart för att bara få en BUNCH av data om vem som använder din webbplats och hur de använder den.