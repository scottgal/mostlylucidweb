# Añadiendo un cliente de seguimiento de C# Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Introducción

En un post anterior hemos añadido un cliente para buscar [Datos analíticos de Umami](/blog/addingascsharpclientforumamiapi). En este post añadiremos un cliente para enviar datos de seguimiento a Umami desde una aplicación C#.
[Umami](https://umami.is/) es un servicio de análisis ligero que puede ser alojado por sí mismo. Es una gran alternativa a Google Analytics y se centra en la privacidad.
Sin embargo, por defecto sólo tiene un cliente de Nodo para el seguimiento de datos (e incluso entonces no es GREAT). Así que decidí escribir un cliente C# para rastrear datos.

### <span style="color:red"> **NOTA He actualizado esto justo ahora, Voy a actualizar la entrada del blog más tarde - Sólo que ahora es 26/08/2024**  </span>

[TOC]

## Requisitos previos

Instalar Umami [Puedes ver cómo hago esto aquí.](/blog/usingumamiforlocalanalytics).

## El cliente

Puede ver todo el código fuente del cliente [aquí](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Esto utiliza la configuración que he definido en mi `appsettings.json` archivo.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Como la pista API no está autenticada, no he añadido ninguna autenticación al cliente.

### Configuración

Con el fin de configurar el cliente que he añadido mi método de extensión habitual con se llama desde su `Program.cs` archivo.

```csharp
services.SetupUmamiClient(config);
```

Esto proporciona una manera sencilla de enganchar en el `UmamiClient` a su solicitud.

El siguiente código muestra el método de configuración.

```csharp
   public static void SetupUmamiClient(this IServiceCollection services, IConfiguration config)
    {
       var umamiSettings= services.ConfigurePOCO<UmamiClientSettings>(config.GetSection(UmamiClientSettings.Section));
       if(string.IsNullOrEmpty( umamiSettings.UmamiPath)) throw new Exception("UmamiUrl is required");
       if(string.IsNullOrEmpty(umamiSettings.WebsiteId)) throw new Exception("WebsiteId is required");
       services.AddTransient<HttpLogger>();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 Node/{Environment.Version}");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif
        
        services.AddHttpContextAccessor();
    }
    
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
```

Como se puede ver esto hace lo siguiente:

1. Configurar el objeto de configuración
2. Compruebe que la configuración es válida
3. Añadir un registrador (si está en modo de depuración)
4. Configure el HttpClient con la dirección base y una política de reintento.

### El propio cliente

Los `UmamiClient` es bastante simple. Tiene un método básico `Send` que envía los datos de seguimiento al servidor Umami.

```csharp
    public async Task<HttpResponseMessage> Send(UmamiPayload payload, string type = "event")
    {
        var jsonPayload = new { type, payload };
        logger.LogInformation("Sending data to Umami {Payload}", JsonSerializer.Serialize(jsonPayload, options));
        var response= await client.PostAsJsonAsync("/api/send", jsonPayload, options);
        if(!response.IsSuccessStatusCode)
        {
           logger.LogError("Failed to send data to Umami {Response}, {Message}", response.StatusCode, response.ReasonPhrase);
        }
        else
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Successfully sent data to Umami {Response}, {Message} {Content}", response.StatusCode, response.ReasonPhrase, content);
        }
        return response;
    }
```

Como verás, esto usa un objeto llamado `UmamiPayload` que contiene todos los parámetros posibles para el seguimiento de solicitudes en Umami.

```csharp
public class UmamiPayload
{
    public string Website { get; set; }=string.Empty;
    public string Hostname { get; set; }=string.Empty;
    public string Language { get; set; }=string.Empty;
    public string Referrer { get; set; }=string.Empty;
    public string Screen { get; set; }=string.Empty;
    public string Title { get; set; }   =string.Empty;
    public string Url { get; set; } =string.Empty;
    public string Name { get; set; } =string.Empty;
    public UmamiEventData? Data { get; set; }
}

public class UmamiEventData : Dictionary<string, object> { }
```

El único campo requerido es `Website` que es el id del sitio web. El resto son opcionales (pero `Url` ¡Es muy útil!).

En el cliente tengo un método llamado `GetPayload()` que envía pobla este objeto de carga útil automáticamente con la información de la solicitud (utilizando la inyección `IHttpContextAccessor`).

```csharp

public class UmamiClient(HttpClient client, ILogger<UmamiClient> logger, IHttpContextAccessor accessor, UmamiClientSettings settings)...

    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        // Initialize a new UmamiPayload object
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data ?? new UmamiEventData(),
            Url = url ?? "" // Default URL to empty string if null
        };

        // Check if HttpContext is available
        if (accessor.HttpContext != null)
        {
            var context = accessor.HttpContext;
            var headers = context.Request.Headers;

            // Fill payload details from HttpContext and headers
            payload.Hostname = context?.Request.Host.Host ?? "";  // Default to empty string if null
            payload.Language = headers?["Accept-Language"].ToString() ?? "";  // Safely retrieve Accept-Language header
            payload.Referrer = headers?["Referer"].ToString() ?? "";  // Safely retrieve Referer header
            payload.Screen = headers?["User-Agent"].ToString() ?? "";  // Safely retrieve User-Agent header
            payload.Title = headers?["Title"].ToString() ?? "";  // Safely retrieve Title header
            payload.Url = string.IsNullOrEmpty(url) ? context.Request.Path.ToString() : url;  // Use the passed URL or fallback to the request path
        }

        return payload;
    }
```

Esto es entonces utilizado por otros métodos de utilidad que dan una interfaz más agradable para estos datos.

```csharp
    public async Task<HttpResponseMessage> TrackUrl(string? url="", string? eventname = "event", UmamiEventData? eventData = null)
    {
        var payload = GetPayload(url);
        payload.Name = eventname;
        return await Track(payload, eventData);
    }

    public async Task<HttpResponseMessage> Track(string eventObj, UmamiEventData? eventData = null)
    {
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Name = eventObj,
            Data = eventData ?? new UmamiEventData()
        };

        return await Send(payload);
    }

    public async Task<HttpResponseMessage> Track(UmamiPayload eventObj, UmamiEventData? eventData = null)
    {
        var payload = eventObj;
        payload.Data = eventData ?? new UmamiEventData();
        payload.Website = settings.WebsiteId;
        return await Send(payload);
    }

    public async Task<HttpResponseMessage> Identify(UmamiEventData eventData)
    {
        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = eventData ?? new()
        };

        return await Send(payload, "identify");
    }
```

Esto le permite realizar un seguimiento de eventos, URLs e identificar a los usuarios.

## Nuget

En el futuro planeo convertir esto en un paquete NuGet. Probando para eso tengo una entrada en el `Umami.Client.csproj` archivo que genera un nuevo paquete 'preview' versión cuando se construye en modo de depuración.

```xml
   <Target Name="NugetPackAutoVersioning" AfterTargets="Build" Condition="'$(Configuration)' == 'Debug'">
    <!-- Delete the contents of the target directory -->
    <RemoveDir Directories="$(SolutionDir)nuget" />
    <!-- Recreate the target directory -->
    <MakeDir Directories="$(SolutionDir)nuget" />
    <!-- Run the dotnet pack command -->
    <Exec Command="dotnet pack -p:PackageVersion=$([System.DateTime]::Now.ToString(&quot;yyyy.MM.dd.HHmm&quot;))-preview -p:V --no-build --configuration $(Configuration) --output &quot;$(SolutionDir)nuget&quot;" />
    <Exec Command="dotnet nuget push $(SolutionDir)nuget\*.nupkg --source Local" />
    <Exec Command="del /f /s /q $(SolutionDir)nuget\*.nupkg" />
</Target>
```

Esto se añade justo antes de terminar `</Project>` etiqueta en la ventana `.csproj` archivo.

Depende de una ubicación nuget llamada 'local' que se define en el `Nuget.config` archivo. Que he mapeado a una carpeta local en mi máquina.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="Local" value="e:\nuget" />
    <add key="Microsoft Visual Studio Offline Packages" value="C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\" />
  </packageSources>
</configuration>
```

## Conclusión

En el futuro planeo hacer de esto un paquete NuGet.
Utilizo esto en el blog ahora, por ejemplo para rastrear cuánto tiempo tardan las traducciones

```csharp
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (translationTask == null) return TypedResults.BadRequest("Task not found");
        await  umamiClient.Send(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
```