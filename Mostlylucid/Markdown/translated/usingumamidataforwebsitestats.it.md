# Uso dei dati Umami per le statistiche del sito web

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Introduzione

Uno dei miei progetti dall'inizio di questo blog è un desiderio quasi ossessivo di monitorare quanti utenti guardare il mio sito web. Per fare questo uso Umami e avere un [BUNCH di posti](/blog/category/Umami) in giro per l'uso e l'installazione di Umami. Ho anche un pacchetto Nuget che permette di tracciare i dati da un sito web ASP.NET Core.

Ora ho aggiunto un nuovo servizio che ti permette di riportare i dati da Umami a un'applicazione C#. Questo è un servizio semplice che utilizza l'API Umami per estrarre i dati dalla tua istanza Umami e utilizzarlo sul tuo sito web / app.

Come al solito tutto il codice sorgente per questo può essere trovato [sul mio GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) per questo sito.

[TOC]

# Installazione

Questo è già nel pacchetto Umami.Net Nuget, installarlo utilizzando il seguente comando:

```bash
dotnet add package Umami.Net
```

È quindi necessario impostare il servizio nel vostro `Program.cs` file:

```csharp
    services.SetupUmamiData(config);
```

In questo modo si utilizza il `Analytics' element from your `file appsettings.json [56]:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Ecco... `UmamiScript` è lo script che si usa per il client side tracking in Umami ([vedi qui](/blog/usingumamiforlocalanalytics) per come impostare questo).
La `WebSiteId` è l'ID per il sito web che hai creato nella tua istanza Umami.
`UmamiPath` è il percorso per la vostra istanza Umami.

La `UserName` e `Password` sono le credenziali per l'istanza Umami (in questo caso uso la password di Admin).

# Uso

Ora hai la `UmamiDataService` nella tua collezione di servizi puoi iniziare ad usarla!

## Metodi

I metodi sono tutti dalla definizione di Umami API si può leggere su di loro qui:
https://umami.is/docs/api/website-stats

Tutti i rendimenti sono avvolti in un `UmamiResults<T>` oggetto che ha un `Success` proprietà e a `Result` proprieta'. La `Result` proprietà è l'oggetto restituito dall'API Umami.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Tutte le richieste a parte `ActiveUsers` avere un oggetto di richiesta base con due proprietà obbligatorie. Ho aggiunto comodità DateTime per l'oggetto di richiesta base per rendere più facile impostare le date di inizio e fine.

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

Il servizio ha i seguenti metodi:

### Utenti attivi

Questo ottiene solo il numero totale di utenti attivi attuali sul sito

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Statistiche

Questo restituisce una serie di statistiche sul sito, compreso il numero di utenti, pagine viste, ecc.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

È possibile impostare un certo numero di parametri per filtrare i dati restituiti dall'API. Per esempio usando `url` restituirà le statistiche per un URL specifico.

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
L'oggetto JSON Umami ritorna è il seguente.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Questo è avvolto nel mio `StatsResponseModel` Oggetto.

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

### MetricsCity name (optional, probably does not need a translation)

Metrics in Umami fornisce il numero di visualizzazioni per tipi specifici di proprietà.

#### Manifestazioni

Un esempio di questi è Eventi [49]:

'Eventi' in Umami sono elementi specifici che è possibile tracciare su un sito. Durante il monitoraggio degli eventi utilizzando Umami.Net è possibile impostare una serie di proprietà che sono tracciate con il nome dell'evento. Per esempio qui traccio `Search` richieste con l'URL e il termine di ricerca.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Per recuperare i dati di questo evento si userebbe il `Metrics` metodo:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Come per gli altri metodi, ciò accetta il `MetricsRequest` oggetto (con l'obbligo `BaseRequest` proprietà) e una serie di proprietà opzionali per filtrare i dati.

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
Qui puoi vedere che puoi specificare una serie di proprietà nell'elemento richiesta per specificare quali metriche vuoi restituire.

È anche possibile impostare un `Limit` proprietà per limitare il numero di risultati restituiti.

Ad esempio, per ottenere l'evento nel corso dell'ultimo giorno di cui sopra si dovrebbe utilizzare la seguente richiesta:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

L'oggetto JSON restituito dall'API è il seguente:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

E di nuovo avvolgo questo nel mio `MetricsResponseModels` Oggetto.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Dove x è il nome dell'evento e y è il numero di volte che è stato attivato.

#### Visualizzazioni pagine

Una delle metriche più utili è il numero di visualizzazioni delle pagine. Questo è il numero di volte che una pagina è stata visualizzata sul sito. Di seguito è riportato il test che uso per ottenere il numero di pagine viste negli ultimi 30 giorni. Prendi nota della `Type` parametro impostato come `MetricType.url` Tuttavia questo è anche il valore predefinito quindi non è necessario impostarlo.

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

Questo restituisce un `MetricsResponse` oggetto che ha la seguente struttura JSON:

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

Dove `x` è l'URL e `y` è il numero di volte che è stato visualizzato.

### PageViewsCity name (optional, probably does not need a translation)

Questo restituisce il numero di visualizzazioni delle pagine per un URL specifico.

Ancora una volta ecco un test che uso per questo metodo:

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

Questo restituisce un `PageViewsResponse` oggetto che ha la seguente struttura JSON:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Dove `date` è la data e `value` è il numero di visualizzazioni delle pagine, questo viene ripetuto per ogni giorno nell'intervallo specificato (o ora, mese, ecc.). a seconda della `Unit` Proprieta').

Come per gli altri metodi, ciò accetta il `PageViewsRequest` oggetto (con l'obbligo `BaseRequest` proprietà) e una serie di proprietà opzionali per filtrare i dati.

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
Come per gli altri metodi è possibile impostare una serie di proprietà per filtrare i dati restituiti dall'API, ad esempio è possibile impostare il
`Country` proprietà per ottenere il numero di pagine viste da un paese specifico.

# Utilizzo del Servizio

In questo sito ho un codice che mi permette di utilizzare questo servizio per ottenere il numero di visualizzazioni di ogni pagina del blog ha. Nel codice sottostante prendo una data di inizio e fine e un prefisso (che è `/blog` nel mio caso) e ottenere il numero di visualizzazioni per ogni pagina nel blog.

Poi cacherò questi dati per un'ora in modo da non dover continuare a colpire l'API Umami.

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

# In conclusione

Questo è un servizio semplice che consente di estrarre i dati da Umami e utilizzarli nella tua applicazione. Uso questo per ottenere il numero di visualizzazioni per ogni pagina del blog e visualizzarlo sulla pagina. Ma è molto utile per ottenere solo un BUNCH di dati su chi utilizza il tuo sito e come lo utilizzano.