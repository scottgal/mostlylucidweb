# Seq for ASP.NET Logging - Búsqueda con SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Introducción

En la parte anterior te mostré cómo configurar [auto hosting para Seq usando ASP.NET Core ](/blog/selfhostingseq). Ahora que lo tenemos configurado es el momento de usar más de sus características para permitir un registro y rastreo más completo usando nuestra nueva instancia Seq.

[TOC]

# Rastreo

La localización es como el registro++ que le da una capa adicional de información sobre lo que está sucediendo en su aplicación. Es especialmente útil cuando tienes un sistema distribuido y necesitas rastrear una solicitud a través de múltiples servicios.
En este sitio lo estoy utilizando para rastrear los problemas rápidamente; sólo porque este es un sitio de hobby no significa que renuncie a mis estándares profesionales.

## Configuración de Serilog

Configurar el rastreo con Serilog es realmente bastante simple usando el [Serilog Tracing](https://github.com/serilog-tracing/serilog-tracing) paquete. Primero tiene que instalar los paquetes:

Aquí también añadimos el fregadero Consola y el fregadero Seq

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Consola es siempre útil para la depuración y Seq es para lo que estamos aquí. Seq también cuenta con un montón de 'enrichers' que pueden añadir información adicional a sus registros.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

Para utilizar estos enriquecedores es necesario añadirlos a su `Serilog` configuración en su `appsettings.json` archivo. También es necesario instalar todos los enriquecedores separados usando Nuget.

Es una de las cosas buenas y malas de Serilog, usted termina instalando un BUNCH de paquetes; pero significa que usted sólo añade lo que necesita y no sólo un paquete monolítico.
Aquí está el mío.

![Serilog Enrichers](serilogenrichers.png)

Con todos estos bombardeados obtengo una muy buena salida de registro en Seq.

![Error Serilog Seq](serilogerror.png)

Aquí puede ver el mensaje de error, el rastro de pila, el id de hilo, el id de proceso y el nombre del proceso. Esta es toda la información útil cuando usted está tratando de localizar un problema.

Una cosa a tener en cuenta es que he establecido el `  "MinimumLevel": "Warning",` en mi `appsettings.json` archivo. Esto significa que sólo las advertencias y superiores se registrarán en Seq. Esto es útil para mantener el ruido en sus registros.

Sin embargo en Seq también puede especificar esto por clave Api; por lo que puede tener `Information` (o si eres realmente entusiasta `Debug`) el registro establecido aquí y limitar lo que Seq realmente captura por la clave de API.

![Seq Api Key](apikey.png)

Nota: todavía tienes la aplicación de arriba, también puedes hacer esto más dinámico para que puedas ajustar el nivel sobre la marcha). Ver la [Sumidero Seq ](https://github.com/datalust/serilog-sinks-seq)para más detalles.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## Rastreo

Ahora añadimos Tracing, otra vez usando SerilogTracing es bastante simple. Tenemos la misma configuración que antes, pero añadimos un nuevo fregadero para rastrear.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

También agregamos un paquete adicional para registrar información más detallada del núcleo de aspnet.

### Configuración en `Program.cs`

Ahora podemos empezar a usar el rastreo. Primero tenemos que añadir el rastreo a nuestro `Program.cs` archivo.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

La localización utiliza el concepto de "Actividades" que representan una unidad de trabajo. Puedes iniciar una actividad, hacer algo de trabajo y luego detenerla. Esto es útil para el seguimiento de una solicitud a través de múltiples servicios.

En este caso añadimos rastreo adicional para solicitudes HttpClient y solicitudes AspNetCore. También añadimos un `TraceToSharedLogger` que registrará la actividad en el mismo registrador que el resto de nuestra aplicación.

## Uso de la localización en un servicio

Ahora tenemos la configuración de rastreo podemos empezar a usarlo en nuestra aplicación. Aquí hay un ejemplo de un servicio que utiliza el rastreo.

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

Las líneas importantes aquí son:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Esto inicia una nueva "actividad" que es una unidad de trabajo. Es útil para rastrear una solicitud a través de múltiples servicios.
Como lo tenemos envuelto en una declaración de uso, esto completará y eliminará al final de nuestro método, pero es una buena práctica completarlo explícitamente.

```csharp
            activity.Complete();
```

En nuestra excepción de manejo de capturas también completamos la actividad, pero con un nivel de error y la excepción. Esto es útil para rastrear problemas en su aplicación.

## Uso de trazas

Ahora tenemos toda esta configuración podemos empezar a usarla. Aquí hay un ejemplo de un rastro en mi solicitud.

![Traza Http](httptrace.png)

Esto le muestra la traducción de un solo mensaje de marcado hacia abajo. Puedes ver los múltiples pasos para un solo post y todas las peticiones y cronometraciones de HttpClient.

Nota Uso Postgres para mi base de datos, a diferencia del servidor SQL, el controlador npgsql tiene soporte nativo para el rastreo para que pueda obtener datos muy útiles de sus consultas de base de datos como el SQL ejecutado, cronometrajes, etc. Estos se guardan como 'panes' a Seq y se ven liek lo siguiente:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

Puede ver que esto incluye prácticamente todo lo que necesita saber sobre la consulta, el SQL ejecutado, la cadena de conexión, etc. Esta es toda la información útil cuando usted está tratando de localizar un problema. En una aplicación más pequeña como esta esto es simplemente interesante, en una aplicación distribuida es información de oro sólido para rastrear problemas.

# Conclusión

Sólo he arañado la superficie de Tracing aquí, es un área un poco con defensores apasionados. Con suerte he demostrado lo sencillo que es seguir adelante con el rastreo simple usando Seq & Serilog para aplicaciones ASP.NET Core. De esta manera puedo obtener gran parte del beneficio de herramientas más poderosas como Application Insights sin el costo de Azure (estos objetos pueden gastarse cuando los registros son grandes).