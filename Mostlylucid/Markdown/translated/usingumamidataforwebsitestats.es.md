# Uso de datos de Umami para estadísticas del sitio web

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-05T23:45</datetime>

# Introducción

Uno de mis proyectos desde el inicio de este blog es un deseo casi obsesivo de rastrear cuántos usuarios miran a mi sitio web. Para hacer esto uso Umami y tengo un [BUNCH de puestos](/blog/category/Umami) alrededor de usar y configurar Umami. También tengo un paquete Nuget que hace posible el seguimiento de los datos de un sitio web ASP.NET Core.

Ahora he añadido un nuevo servicio que le permite recuperar los datos de Umami a una aplicación C#. Este es un servicio sencillo que utiliza la API de Umami para extraer datos de tu instancia de Umami y usarlos en tu sitio web / aplicación.

Como de costumbre, todo el código fuente para esto se puede encontrar [en mi GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) para este sitio.

[TOC]

# Instalación

Esto ya está en el paquete Umami.Net Nuget, instálelo usando el siguiente comando:

```bash
dotnet add package Umami.Net
```

Usted entonces necesita para configurar el servicio en su `Program.cs` archivo:

```csharp
    services.SetupUmamiData(config);
```

Esto utiliza la `Analytics' element from your `archivo appsettings.json`:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Aquí la `UmamiScript` es el script que utilizas para el seguimiento del lado del cliente en Umami ([Mira aquí.](/blog/usingumamiforlocalanalytics) para cómo configurar eso ).
Los `WebSiteId` es el ID del sitio web que creó en su instancia de Umami.
`UmamiPath` es el camino a tu instancia Umami.

Los `UserName` y `Password` son las credenciales de la instancia Umami (en este caso uso la contraseña de administrador).

# Uso

Ahora tienes el `UmamiDataService` en su colección de servicio puede empezar a utilizarlo!

## Métodos

Los métodos son todos de la definición de la API de Umami que puede leer sobre ellos aquí:
https://umami.is/docs/api/website-stats

Todos los retornos están envueltos en un `UmamiResults<T>` objeto que tiene un `Success` propiedad y a `Result` propiedad. Los `Result` propiedad es el objeto devuelto desde la API de Umami.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Todas las solicitudes aparte de `ActiveUsers` tener un objeto de solicitud de base con dos propiedades obligatorias. Añadí comodidad DateTimes al objeto de solicitud base para que sea más fácil establecer las fechas de inicio y fin.

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

El servicio tiene los siguientes métodos:

### Usuarios activos

Esto acaba de obtener el número total de usuarios activos en el sitio

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Estadísticas

Esto devuelve un montón de estadísticas sobre el sitio, incluyendo el número de usuarios, páginas vistas, etc.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Puede establecer una serie de parámetros para filtrar los datos devueltos desde la API. Por ejemplo, usando `url` devuelve las estadísticas para una URL específica.

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
El objeto JSON Umami devuelve es el siguiente.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Esto está envuelto dentro de mi `StatsResponseModel` objeto.

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

### Métricas

Métricas en Umami le proporcionan el número de vistas para tipos específicos de propiedades.

#### Acontecimientos

Un ejemplo de esto es Acontecimientos`:

"Eventos" en Umami son elementos específicos que se pueden rastrear en un sitio. Al rastrear eventos usando Umami.Net puede establecer una serie de propiedades que son rastreadas con el nombre del evento. Por ejemplo, aquí hago un seguimiento. `Search` peticiones con la URL y el término de búsqueda.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Para obtener datos sobre este evento usted usaría el `Metrics` método:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Al igual que con los otros métodos esto acepta la `MetricsRequest` objeto (con la obligación de `BaseRequest` propiedades) y una serie de propiedades opcionales para filtrar los datos.

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
Aquí puede ver que puede especificar un número de propiedades en el elemento request para especificar qué métricas desea devolver.

También puede establecer un `Limit` propiedad para limitar el número de resultados devueltos.

Por ejemplo, para obtener el evento en el último día que mencioné anteriormente se usaría la siguiente petición:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

El objeto JSON devuelto desde la API es el siguiente:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

Y de nuevo envuelvo esto en mi `MetricsResponseModels` objeto.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Donde x es el nombre del evento y y es el número de veces que se ha activado.

#### Vistas de página

Una de las métricas más útiles es el número de páginas vistas. Este es el número de veces que una página ha sido vista en el sitio. A continuación se muestra la prueba que utilizo para obtener el número de páginas vistas en los últimos 30 días. Tomarás nota de la `Type` parámetro se establece como `MetricType.url` Sin embargo, este es también el valor predeterminado para que no necesite configurarlo.

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

Esto devuelve un `MetricsResponse` objeto que tiene la siguiente estructura JSON:

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

Dónde `x` es el URL y `y` es el número de veces que se ha visto.

### PageViews

Esto devuelve el número de vistas de página para una URL específica.

Una vez más aquí es una prueba que utilizo para este método:

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

Esto devuelve un `PageViewsResponse` objeto que tiene la siguiente estructura JSON:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Dónde `date` es la fecha y `value` es el número de páginas vistas, esto se repite para cada día en el rango especificado (o hora, mes, etc. dependiendo de la `Unit` propiedad).

Al igual que con los otros métodos esto acepta la `PageViewsRequest` objeto (con la obligación de `BaseRequest` propiedades) y una serie de propiedades opcionales para filtrar los datos.

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
Al igual que con los otros métodos, puede establecer una serie de propiedades para filtrar los datos devueltos desde la API, por ejemplo, podría establecer la
`Country` propiedad para obtener el número de páginas vistas de un país específico.

# Uso del servicio

En este sitio tengo un código que me permite utilizar este servicio para obtener el número de vistas que tiene cada página de blog. En el siguiente código tomo una fecha de inicio y fin y un prefijo (que es `/blog` en mi caso) y obtener el número de vistas para cada página en el blog.

Entonces cacheo estos datos durante una hora para no tener que seguir golpeando la API de Umami.

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

# Conclusión

Este es un servicio sencillo que le permite extraer datos de Umami y usarlos en su aplicación. Utilizo esto para obtener el número de vistas para cada página del blog y mostrarlo en la página. Pero es muy útil para obtener un montón de datos sobre quién utiliza su sitio y cómo lo utilizan.