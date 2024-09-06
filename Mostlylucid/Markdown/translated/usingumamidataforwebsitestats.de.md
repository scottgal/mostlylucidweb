# Verwendung von Umami-Daten für Website-Statistiken

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Einleitung

Eines meiner Projekte seit dem Start dieses Blogs ist ein fast obsessive Wunsch zu verfolgen, wie viele Benutzer auf meiner Website schauen. Um dies zu tun, benutze ich Umami und habe eine [BUNCH der Stellen](/blog/category/Umami) um Umami zu benutzen und einzurichten. Ich habe auch ein Nuget-Paket, das es ermöglicht, Daten von einer ASP.NET Core-Website zu verfolgen.

Jetzt habe ich einen neuen Service hinzugefügt, mit dem Sie Daten von Umami zu einer C#-Anwendung zurückziehen können. Dies ist ein einfacher Dienst, der die Umami API verwendet, um Daten aus Ihrer Umami-Instanz zu ziehen und auf Ihrer Website / App zu verwenden.

Wie üblich kann der Quellcode dazu gefunden werden [auf meinem GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) für diese Seite.

[TOC]

# Installation

Dies ist bereits im Umami.Net Nuget Paket, installieren Sie es mit dem folgenden Befehl:

```bash
dotnet add package Umami.Net
```

Sie müssen dann die Einrichtung des Dienstes in Ihrem `Program.cs` Datei:

```csharp
    services.SetupUmamiData(config);
```

Dabei wird die `Analytics' element from your `appsettings.json` Datei:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Hier die `UmamiScript` ist das Skript, das Sie für das Client-Seiten-Tracking in Umami verwenden ([siehe hier](/blog/usingumamiforlocalanalytics) für wie man das aufstellt ).
Das `WebSiteId` ist die ID für die Website, die Sie in Ihrer Umami-Instanz erstellt haben.
`UmamiPath` ist der Weg zu Ihrer Umami-Instanz.

Das `UserName` und `Password` sind die Anmeldeinformationen für die Umami-Instanz (in diesem Fall verwende ich das Admin-Passwort).

# Verwendung

Jetzt haben Sie die `UmamiDataService` in Ihrer Service-Sammlung können Sie damit beginnen!

## Methoden

Die Methoden stammen alle aus der Umami API Definition, die Sie hier lesen können:
https://umami.is/docs/api/website-stats

Alle Retouren sind in einem `UmamiResults<T>` Gegenstand, der eine `Success` Eigentum und a `Result` Eigentum. Das `Result` property ist das von der Umami API zurückgegebene Objekt.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Alle Anfragen mit Ausnahme von `ActiveUsers` haben ein Basis-Request-Objekt mit zwei obligatorischen Eigenschaften. Ich fügte hinzu, Komfort DateTimes zum Basis-Request-Objekt, um es einfacher zu machen, die Start- und Enddaten zu setzen.

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

Der Service hat folgende Methoden:

### Aktive Benutzer

Dies bekommt nur die Gesamtzahl der LURRENTEN aktiven Benutzer auf der Website

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Statistik

Dies liefert eine Reihe von Statistiken über die Website, einschließlich der Anzahl der Benutzer, Seitenaufrufe, etc.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Sie können eine Reihe von Parametern festlegen, um die von der API zurückgegebenen Daten zu filtern. Zum Beispiel mit `url` wird die Statistiken für eine bestimmte URL zurückgeben.

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
Das JSON-Objekt, das Umami zurückgibt, ist wie folgt.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Das ist in meinem `StatsResponseModel` Gegenstand.

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

### Metrik

Metrics in Umami bieten Ihnen die Anzahl der Ansichten für bestimmte Arten von Eigenschaften.

#### Veranstaltungen

Ein Beispiel dafür ist Events`:

'Events' in Umami sind bestimmte Elemente, die Sie auf einer Website verfolgen können. Wenn Sie Ereignisse mit Umami.Net verfolgen, können Sie eine Reihe von Eigenschaften festlegen, die mit dem Ereignisnamen verfolgt werden. Zum Beispiel hier tracke ich `Search` Anfragen mit der URL und dem Suchbegriff.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Um Daten über dieses Ereignis zu erhalten, würden Sie die `Metrics` Methode:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Wie bei den anderen Methoden akzeptiert dies die `MetricsRequest` Gegenstand (mit der obligatorischen `BaseRequest` Eigenschaften) und eine Reihe von optionalen Eigenschaften, um die Daten zu filtern.

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
Hier sehen Sie, dass Sie im Request-Element eine Reihe von Eigenschaften angeben können, um festzulegen, welche Metriken Sie zurückgeben möchten.

Sie können auch eine `Limit` Eigenschaft, um die Anzahl der zurückgegebenen Ergebnisse zu begrenzen.

Zum Beispiel, um die Veranstaltung über den letzten Tag, den ich oben erwähnt, würden Sie die folgende Anfrage verwenden:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

Das JSON-Objekt, das von der API zurückgegeben wird, ist wie folgt:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Und wieder wickeln ich dies in meine `MetricsResponseModels` Gegenstand.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Wo x der Ereignisname ist und y die Anzahl der Male, die er ausgelöst wurde.

#### Seitenansichten

Eine der nützlichsten Metriken ist die Anzahl der Seitenaufrufe. Dies ist die Anzahl der Male, die eine Seite auf der Website angesehen wurde. Unten ist der Test, den ich benutze, um die Anzahl der Seitenaufrufe in den letzten 30 Tagen zu erhalten. Sie werden feststellen, dass `Type` Parameter wird als `MetricType.url` Dies ist jedoch auch der Standardwert, damit Sie ihn nicht einstellen müssen.

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

Dies ergibt eine `MetricsResponse` Objekt, das die folgende JSON-Struktur hat:

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

Dabei ist `x` ist die URL und `y` ist die Anzahl der Male, die sie betrachtet wurde.

### Seitenansichten

Dies gibt die Anzahl der Seitenaufrufe für eine bestimmte URL zurück.

Auch hier ist ein Test, den ich für diese Methode:

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

Dies ergibt eine `PageViewsResponse` Objekt, das die folgende JSON-Struktur hat:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Dabei ist `date` ist das Datum und `value` ist die Anzahl der Seitenaufrufe, die für jeden Tag im angegebenen Bereich (oder Stunde, Monat usw.) wiederholt werden. je nach `Unit` Grundbesitz).

Wie bei den anderen Methoden akzeptiert dies die `PageViewsRequest` Gegenstand (mit der obligatorischen `BaseRequest` Eigenschaften) und eine Reihe von optionalen Eigenschaften, um die Daten zu filtern.

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
Wie bei den anderen Methoden können Sie eine Reihe von Eigenschaften festlegen, um die von der API zurückgegebenen Daten zu filtern, z.B. können Sie die
`Country` Eigentum, um die Anzahl der Seitenaufrufe aus einem bestimmten Land zu erhalten.

# Benutzung des Dienstes

In dieser Website habe ich einige Code, der mich diesen Service verwenden lässt, um die Anzahl der Ansichten zu erhalten, die jede Blog-Seite hat. Im Code unten nehme ich ein Start- und Enddatum und ein Präfix (das ist `/blog` in meinem Fall) und erhalten Sie die Anzahl der Ansichten für jede Seite im Blog.

Ich speichere diese Daten dann eine Stunde lang, damit ich die Umami API nicht weiter schlagen muss.

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

# Schlussfolgerung

Dies ist ein einfacher Service, mit dem Sie Daten aus Umami ziehen und in Ihrer Anwendung verwenden können. Ich benutze dies, um die Anzahl der Ansichten für jede Blog-Seite zu erhalten und sie auf der Seite anzuzeigen. Aber es ist sehr nützlich, um nur eine BUNCH von Daten darüber, wer Ihre Website verwendet und wie sie es verwenden.