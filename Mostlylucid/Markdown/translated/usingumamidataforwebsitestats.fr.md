# Utilisation des données Umami pour les statistiques du site Web

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Présentation

Un de mes projets depuis le lancement de ce blog est un désir presque obsessionnel de suivre combien d'utilisateurs regardent mon site Web. Pour ce faire, j'utilise Umami et j'ai un [BUNCH des postes](/blog/category/Umami) autour de l'utilisation et de la mise en place d'Umami. J'ai également un paquet Nuget qui permet de suivre les données d'un site ASP.NET Core.

Maintenant, j'ai ajouté un nouveau service qui vous permet de récupérer les données d'Umami dans une application C#. Il s'agit d'un service simple qui utilise l'API Umami pour extraire les données de votre instance Umami et l'utiliser sur votre site / application.

Comme d'habitude, tout le code source pour cela peut être trouvé [sur mon GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) pour ce site.

[TOC]

# Installation

Ceci est déjà dans le paquet Umami.Net Nuget, l'installer en utilisant la commande suivante:

```bash
dotnet add package Umami.Net
```

Vous devez alors mettre en place le service dans votre `Program.cs` fichier & #160;:

```csharp
    services.SetupUmamiData(config);
```

Il s'agit de `Analytics' element from your `appsettings.json` fichier:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Ici, le `UmamiScript` est le script que vous utilisez pour le suivi côté client dans Umami ([Voir ici](/blog/usingumamiforlocalanalytics) pour la façon de mettre cela en place ).
Les `WebSiteId` est l'ID du site Web que vous avez créé dans votre instance Umami.
`UmamiPath` est le chemin vers votre instance Umami.

Les `UserName` et `Password` sont les identifiants pour l'instance Umami (dans ce cas, j'utilise le mot de passe Admin).

# Utilisation

Maintenant, vous avez le `UmamiDataService` dans votre collection de services, vous pouvez commencer à l'utiliser!

## Méthodes

Les méthodes sont toutes de la définition de l'API Umami que vous pouvez lire à leur sujet ici:
https://umami.is/docs/api/website-stats

Tous les retours sont emballés dans un `UmamiResults<T>` objet qui a un `Success` biens immobiliers et `Result` propriété. Les `Result` propriété est l'objet retourné de l'API Umami.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Toutes les demandes, à l'exception `ActiveUsers` ont un objet de requête de base avec deux propriétés obligatoires. J'ai ajouté la commodité DateTimes à l'objet de requête de base pour faciliter la mise en place des dates de début et de fin.

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

Le service a les méthodes suivantes:

### Utilisateurs actifs

Il suffit d'obtenir le nombre total d'utilisateurs actifs CURRENT sur le site

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Statistiques

Cela renvoie un tas de statistiques sur le site, y compris le nombre d'utilisateurs, de pages vues, etc.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Vous pouvez définir un certain nombre de paramètres pour filtrer les données retournées depuis l'API. Par exemple en utilisant `url` retournera les statistiques pour une URL spécifique.

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
Le retour de l'objet JSON Umami est le suivant.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

C'est enveloppé à l'intérieur de mon `StatsResponseModel` objet.

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

### métriques

Les mesures dans Umami vous fournissent le nombre de vues pour des types spécifiques de propriétés.

#### Événements

L'un de ces exemples est Events`:

Les « événements » en Umami sont des éléments spécifiques que vous pouvez suivre sur un site. Lorsque vous suivez les événements en utilisant Umami.Net, vous pouvez définir un certain nombre de propriétés qui sont suivies avec le nom de l'événement. Par exemple, ici, je traque `Search` demande avec l'URL et le terme de recherche.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Pour récupérer des données sur cet événement, vous utiliseriez le `Metrics` méthode:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Comme pour les autres méthodes, cette `MetricsRequest` objet (avec l'obligation `BaseRequest` propriétés) et un certain nombre de propriétés optionnelles pour filtrer les données.

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
Ici, vous pouvez voir que vous pouvez spécifier un certain nombre de propriétés dans l'élément de requête pour spécifier quelles métriques vous souhaitez retourner.

Vous pouvez également définir un `Limit` propriété pour limiter le nombre de résultats retournés.

Par exemple, pour obtenir l'événement au cours du dernier jour que j'ai mentionné ci-dessus, vous utiliseriez la demande suivante:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

L'objet JSON renvoyé de l'API est le suivant :

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Et encore une fois je l'enroule dans mon `MetricsResponseModels` objet.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Où x est le nom de l'événement et y est le nombre de fois qu'il a été déclenché.

#### Vues des pages

L'une des mesures les plus utiles est le nombre de pages vues. C'est le nombre de fois qu'une page a été vue sur le site. Voici le test que j'utilise pour obtenir le nombre de pages vues au cours des 30 derniers jours. Vous noterez que `Type` paramètre est défini comme `MetricType.url` Cependant, c'est aussi la valeur par défaut afin que vous n'ayez pas besoin de la définir.

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

Cela revient à un `MetricsResponse` objet dont la structure JSON est la suivante:

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

où `x` est l'URL et `y` est le nombre de fois qu'il a été visionné.

### Affichages de pages

Cela retourne le nombre de pages vues pour une URL spécifique.

Encore une fois, voici un test que j'utilise pour cette méthode:

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

Cela revient à un `PageViewsResponse` objet dont la structure JSON est la suivante:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

où `date` est la date et `value` est le nombre de pages vues, cela est répété pour chaque jour dans la plage spécifiée (ou heure, mois, etc.). en fonction de la `Unit` la propriété).

Comme pour les autres méthodes, cette `PageViewsRequest` objet (avec l'obligation `BaseRequest` propriétés) et un certain nombre de propriétés optionnelles pour filtrer les données.

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
Comme avec les autres méthodes, vous pouvez définir un certain nombre de propriétés pour filtrer les données renvoyées de l'API, par exemple, vous pouvez définir le
`Country` propriété pour obtenir le nombre de pages vues d'un pays spécifique.

# Utilisation du service

Dans ce site, j'ai un certain code qui me permet d'utiliser ce service pour obtenir le nombre de vues que chaque page de blog a. Dans le code ci-dessous, je prends une date de début et de fin et un préfixe (qui est `/blog` dans mon cas) et obtenir le nombre de vues pour chaque page du blog.

Je cache ces données pendant une heure, donc je n'ai pas à continuer à frapper l'API Umami.

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

# En conclusion

Il s'agit d'un service simple qui vous permet de tirer des données d'Umami et de les utiliser dans votre application. J'utilise ceci pour obtenir le nombre de vues pour chaque page de blog et l'afficher sur la page. Mais il est très utile pour obtenir un BUNCH de données sur qui utilise votre site et comment ils l'utilisent.