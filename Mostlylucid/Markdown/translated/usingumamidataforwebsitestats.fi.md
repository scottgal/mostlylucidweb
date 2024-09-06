# Umami-tietojen käyttö verkkosivujen tilastoihin

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024–09–05T23/45</datetime>

# Johdanto

Yksi projekteistani tämän blogin alusta lähtien on lähes pakkomielteinen halu seurata, kuinka moni käyttäjä katsoo nettisivuilleni. Tätä varten käytän Umamia ja minulla on [PYSYVÄT TEHTÄVÄT](/blog/category/Umami) Umamin käytön ja perustamisen ympärillä. Minulla on myös Nuget-paketti, joka mahdollistaa tietojen seuraamisen ASP.NET Core -sivustolta.

Nyt olen lisännyt uuden palvelun, jonka avulla voit vetää dataa takaisin Umamista C#-sovellukseen. Tämä on yksinkertainen palvelu, joka käyttää Umami-rajapintaa vetääkseen dataa Umami-instanssistasi ja käyttääkseen sitä verkkosivuillasi / sovelluksessasi.

Tavanomaiseen tapaan kaikki tämän lähdekoodit löytyvät [GitHubillani](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) tälle sivustolle.

[TÄYTÄNTÖÖNPANO

# Asennus

Tämä on jo Umami.Net Nuget -paketissa, asenna se seuraavalla komennolla:

```bash
dotnet add package Umami.Net
```

Sinun täytyy sitten perustaa palvelu omassa `Program.cs` tiedosto:

```csharp
    services.SetupUmamiData(config);
```

Tässä käytetään `Analytics' element from your `appsetings.json-tiedosto:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Tässä. `UmamiScript` on käsikirjoitus, jota käytät asiakaspuolen jäljittämiseen Umamissa ([katso täältä](/blog/usingumamiforlocalanalytics) siitä, miten perustaa se ).
Erytropoietiini `WebSiteId` on Umami-installaatiossasi luomasi sivuston henkilöllisyystodistus.
`UmamiPath` on polku Umami-installaatioosi.

Erytropoietiini `UserName` sekä `Password` Ovatko valtakirjat Umami-instanssin (tässä tapauksessa käytän Admin-salasanaa).

# Käyttö

Nyt sinulla on `UmamiDataService` Palvelukokoelmassasi voit alkaa käyttää sitä!

## Menetelmät

Menetelmät ovat kaikki Umamin API-määritelmästä, josta voit lukea ne täältä:
https://umami.is/docs/api/website-stats

Kaikki tuotot on kääritty `UmamiResults<T>` esine, jolla on `Success` omaisuus ja a `Result` kiinteistöt. Erytropoietiini `Result` Omaisuus on esine, joka on palautettu Umamin rajapinnasta.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Kaikki pyynnöt lukuun ottamatta `ActiveUsers` joilla on kahden pakollisen ominaisuuden peruspyyntökappale. Lisäsin perustietopyyntöön mukavuuspäivät, jotta aloitus- ja lopetuspäivien asettaminen helpottuisi.

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

Palvelussa on seuraavat menetelmät:

### Aktiiviset käyttäjät

Tämä vain saa yhteensä CURRENT aktiivisia käyttäjiä sivustolla

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Tilastot

Tämä palauttaa joukon tilastoja sivustosta, mukaan lukien käyttäjämäärät, sivunäkymät jne.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Voit asettaa useita parametreja API:stä palautettujen tietojen suodattamiseksi. Esimerkiksi käyttämällä `url` palauttaa tilastot tietylle URL-osoitteelle.

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
JSON-objekti Umami palaa seuraavasti.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Tämä on minun sisälläni. `StatsResponseModel` Esine.

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

### Metriikka

Umamin Metrics tarjoaa sinulle useita näkökulmia tietyntyyppisiin ominaisuuksiin.

#### Tapahtumat

Yksi esimerkki näistä on Events`:

"Tapahtumat" Umamissa ovat erityisiä kohteita, joita voi seurata sivustolla. Kun seuraat tapahtumia Umami.Netin avulla, voit asettaa useita ominaisuuksia, joita seuraa tapahtumanimi. Esimerkiksi tässä minä seuraan `Search` pyyntöjä URL-osoitteella ja hakutermillä.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Voit hakea tietoja tästä tapahtumasta käyttämällä `Metrics` menetelmä:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Kuten muillakin menetelmillä, tämä hyväksyy `MetricsRequest` esine (pakollisella `BaseRequest` ominaisuudet) ja useita valinnaisia ominaisuuksia tietojen suodattamiseksi.

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
Tässä näet, että voit määrittää useita ominaisuuksia pyynnöissä määrittääksesi, mitä metrejä haluat palauttaa.

Voit myös asettaa `Limit` omaisuus, joka rajoittaa palautettujen tulosten määrää.

Esimerkiksi saadaksesi tapahtuman kuluneena päivänä, jonka mainitsin edellä, käyttäisit seuraavaa pyyntöä:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

API:stä palautettu JSON-objekti on seuraava:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Ja taas käärin tämän omaan `MetricsResponseModels` Esine.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Missä x on tapahtuman nimi ja y on sen laukaistujen kertamäärien määrä.

#### Sivunäkymät

Yksi hyödyllisimmistä mittareista on sivunäkymän määrä. Näin monta kertaa sivua on katsottu sivustolla. Alla on testi, jolla saan sivun katselumäärän viimeisen 30 päivän aikana. Huomaat, että `Type` parametri asetetaan seuraavasti: `MetricType.url` Tämä on kuitenkin myös oletusarvo, joten sitä ei tarvitse asettaa.

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

Tämä palauttaa a `MetricsResponse` Esine, jolla on seuraava JSON-rakenne:

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

Jossa `x` on URL-osoite ja `y` on se, kuinka monta kertaa sitä on katsottu.

### PageViews

Tämä palauttaa tietyn URL-osoitteen sivun katselumäärän.

Tässäkin on testi, jota käytän tässä menetelmässä:

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

Tämä palauttaa a `PageViewsResponse` Esine, jolla on seuraava JSON-rakenne:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Jossa `date` on päiväys ja `value` on sivun katselumäärä, tämä toistetaan jokaiselle päivälle määritetyllä vaihteluvälillä (tai tunti, kuukausi jne.). riippuen `Unit` kiinteistöt).

Kuten muillakin menetelmillä, tämä hyväksyy `PageViewsRequest` esine (pakollisella `BaseRequest` ominaisuudet) ja useita valinnaisia ominaisuuksia tietojen suodattamiseksi.

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
Kuten muillakin menetelmillä voit asettaa useita ominaisuuksia API:stä palautettujen tietojen suodattamiseksi, voit esimerkiksi
`Country` kiinteistöä, jolla saa sivun katselumäärän tietystä maasta.

# Palveluksen käyttö

Tällä sivustolla on jokin koodi, jonka avulla voin käyttää tätä palvelua saadakseni jokaisen blogisivun katsojamäärät. Alla olevassa koodissa otan aloitus- ja lopetuspäivän sekä etuliitteen (joka on `/blog` minun kohdallani) ja saat blogiin jokaisen sivun katsojamäärän.

Tämän jälkeen piilotan tiedot tunniksi, jotta minun ei tarvitse jatkaa Umamin rajapintaa.

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

# Johtopäätöksenä

Tämä on yksinkertainen palvelu, jonka avulla voit hakea dataa Umamista ja käyttää sitä sovelluksessasi. Käytän tätä saadakseni jokaisen blogisivun katsojamäärät ja näyttääkseni ne sivulla. Mutta se on erittäin hyödyllinen vain saada BUNCH tietoja siitä, kuka käyttää sivustoasi ja miten he käyttävät sitä.