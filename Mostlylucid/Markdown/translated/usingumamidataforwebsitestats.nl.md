# Umami-gegevens gebruiken voor webstatistieken

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Inleiding

Een van mijn projecten sinds het starten van deze blog is een bijna obsessieve wens om bij te houden hoeveel gebruikers kijken op mijn website. Om dit te doen gebruik ik Umami en heb ik een [BUNCH van posten](/blog/category/Umami) rond het gebruik en het opzetten van Umami. Ik heb ook een Nuget pakket dat het mogelijk maakt om gegevens van een ASP.NET Core website te volgen.

Nu heb ik een nieuwe service toegevoegd waarmee je data terug kunt halen van Umami naar een C# applicatie. Dit is een eenvoudige service die gebruik maakt van de Umami API om gegevens uit uw Umami instantie te halen en deze te gebruiken op uw website / app.

Zoals gewoonlijk kan alle broncode hiervoor gevonden worden [op mijn GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) voor deze site.

[TOC]

# Installatie

Dit zit al in het Umami.Net Nuget pakket, installeer het met het volgende commando:

```bash
dotnet add package Umami.Net
```

U moet dan de dienst in uw `Program.cs` bestand:

```csharp
    services.SetupUmamiData(config);
```

Dit maakt gebruik van de `Analytics' element from your `appsettings.json

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Hier de `UmamiScript` is het script dat u gebruikt voor de client side tracking in Umami ([Zie hier](/blog/usingumamiforlocalanalytics) voor hoe dat op te zetten ).
De `WebSiteId` is het ID voor de website die u hebt gemaakt in uw Umami instantie.
`UmamiPath` is het pad naar uw Umami instantie.

De `UserName` en `Password` zijn de referenties voor de Umami instantie (in dit geval gebruik ik het Admin wachtwoord).

# Gebruik

Nu heb je de `UmamiDataService` in uw service collectie kunt u beginnen met het te gebruiken!

## Methoden

De methoden zijn allemaal van de Umami API definitie kunt u hier lezen over hen:
https://umami.is/docs/api/website-stats

Alle retourzendingen zijn verpakt in een `UmamiResults<T>` object dat een `Success` eigendom en a `Result` eigendom. De `Result` eigendom is het object dat van de Umami API is teruggegeven.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Alle aanvragen met uitzondering van `ActiveUsers` hebben een basis aanvraag object met twee verplichte eigenschappen. Ik heb gemak toegevoegd DateTimes aan het basisverzoek object om het makkelijker te maken om de start- en einddatums in te stellen.

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

De service heeft de volgende methoden:

### Actieve gebruikers

Dit krijgt gewoon het totale aantal LOPENDE actieve gebruikers op de site

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Statistieken

Dit geeft een heleboel statistieken terug over de site, inclusief het aantal gebruikers, paginaweergaven, enz.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

U kunt een aantal parameters instellen om de gegevens terug te filteren van de API. Bijvoorbeeld het gebruik van `url` geeft de statistieken voor een specifieke URL terug.

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
Het JSON object dat Umami teruggeeft is als volgt.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Dit is verpakt in mijn `StatsResponseModel` object.

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

### Metrics

Metrics in Umami bieden u het aantal views voor specifieke soorten eigenschappen.

#### Gebeurtenissen

Een voorbeeld hiervan is Evenementen...

'Events' in Umami zijn specifieke items die je kunt volgen op een site. Bij het volgen van gebeurtenissen met Umami.Net kunt u een aantal eigenschappen instellen die worden gevolgd met de evenementnaam. Bijvoorbeeld hier traceer ik `Search` verzoeken met de URL en de zoekterm.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Om gegevens over deze gebeurtenis op te halen zou u de `Metrics` methode:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Zoals bij de andere methoden aanvaardt dit de `MetricsRequest` object (met de verplichte `BaseRequest` eigenschappen) en een aantal optionele eigenschappen om de gegevens te filteren.

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
Hier kunt u zien dat u een aantal eigenschappen kunt opgeven in het verzoekelement om aan te geven welke metriek u wilt retourneren.

U kunt ook een `Limit` eigenschap om het aantal teruggekeerde resultaten te beperken.

Bijvoorbeeld om het evenement te krijgen over de afgelopen dag die ik hierboven vermeld zou u gebruik maken van de volgende aanvraag:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

Het van de API geretourneerde JSON object is als volgt:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

En weer wikkel ik dit in mijn `MetricsResponseModels` object.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Waar x de gebeurtenisnaam is en y het aantal keren dat het is geactiveerd.

#### Paginaweergaven

Een van de meest nuttige metrics is het aantal paginaweergaven. Dit is het aantal keren dat een pagina is bekeken op de site. Hieronder is de test die ik gebruik om het aantal pagina's in de afgelopen 30 dagen te krijgen. U zult merken dat de `Type` parameter is ingesteld als `MetricType.url` Maar dit is ook de standaard waarde, dus je hoeft het niet in te stellen.

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

Dit geeft een `MetricsResponse` object met de volgende JSON structuur:

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

waarbij `x` is de URL en `y` is het aantal keren dat het is bekeken.

### Paginaweergaven

Dit geeft het aantal paginaweergaven voor een specifieke URL terug.

Nogmaals hier is een test die ik gebruik voor deze methode:

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

Dit geeft een `PageViewsResponse` object met de volgende JSON structuur:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

waarbij `date` de datum is en `value` is het aantal paginaweergaven, dit wordt herhaald voor elke dag in het opgegeven bereik (of uur, maand, enz. afhankelijk van de `Unit` eigenschap).

Zoals bij de andere methoden aanvaardt dit de `PageViewsRequest` object (met de verplichte `BaseRequest` eigenschappen) en een aantal optionele eigenschappen om de gegevens te filteren.

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
Zoals met de andere methoden kunt u een aantal eigenschappen instellen om de gegevens terug te filteren van de API, bijvoorbeeld kunt u de
`Country` eigendom om het aantal paginaweergaven uit een bepaald land te krijgen.

# Het gebruik van de dienst

Op deze site heb ik een aantal code waarmee ik gebruik maken van deze dienst om het aantal views elke blog pagina heeft te krijgen. In de onderstaande code neem ik een begin- en einddatum en een voorvoegsel (dat is `/blog` in mijn geval) en krijg het aantal weergaven voor elke pagina in de blog.

Ik cache deze gegevens voor een uur, zodat ik niet hoeft te blijven slaan op de Umami API.

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

# Conclusie

Dit is een eenvoudige service waarmee u gegevens uit Umami kunt halen en gebruiken in uw toepassing. Ik gebruik dit om het aantal weergaven voor elke blogpagina te krijgen en deze weer te geven op de pagina. Maar het is erg handig voor het krijgen van een BUNCH van gegevens over wie uw site gebruikt en hoe ze het gebruiken.