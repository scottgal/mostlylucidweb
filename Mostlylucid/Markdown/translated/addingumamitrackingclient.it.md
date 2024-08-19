# Aggiunta di un client di monitoraggio C# Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Introduzione

In un post precedente abbiamo aggiunto un client per il recupero [Dati analitici di Umami](/blog/addingascsharpclientforumamiapi). In questo post aggiungeremo un client per l'invio di dati di tracciamento a Umami da un'applicazione C#.
[UmamiCity name (optional, probably does not need a translation)](https://umami.is/) è un servizio di analisi leggero che può essere self-hosted. È una grande alternativa a Google Analytics ed è focalizzata sulla privacy.
Tuttavia per impostazione predefinita ha solo un client Node per il monitoraggio dei dati (e anche allora non è GRANDE). Così ho deciso di scrivere un client C# per tracciare i dati.

[TOC]

## Prerequisiti

Installa Umami [Puoi vedere come faccio qui.](/blog/usingumamiforlocalanalytics).

## Il client

Puoi vedere tutto il codice sorgente del client [qui](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Questo usa le impostazioni che ho definito nel mio `appsettings.json` Archivio.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Poiché la traccia API non è autenticata non ho aggiunto alcuna autenticazione al client.

### Configurazione

Al fine di impostare il client ho aggiunto il mio metodo di estensione con è chiamato dal vostro `Program.cs` Archivio.

```csharp
services.SetupUmamiClient(config);
```

Questo fornisce un modo semplice per agganciare nel `UmamiClient` alla sua domanda.

Il codice sottostante mostra il metodo di configurazione.

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

Come potete vedere questo fa il seguente:

1. Imposta l'oggetto di configurazione
2. Controllare che le impostazioni siano valide
3. Aggiunge un logger (se in modalità debug)
4. Impostare l'HttpClient con l'indirizzo di base e una politica di riprova.

### Il cliente stesso

La `UmamiClient` è abbastanza semplice. Ha un unico metodo di base `Send` che invia i dati di tracciamento al server Umami.

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

Come vedrai questo usa un oggetto chiamato `UmamiPayload` che contiene tutti i parametri possibili per le richieste di tracciamento in Umami.

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

L'unico campo richiesto è `Website` che è l'ID del sito web. Il resto è facoltativo (ma `Url` è davvero utile!).

Nel cliente ho un metodo chiamato `GetPayload()` che invia automaticamente questo oggetto payload con informazioni provenienti dalla richiesta (utilizzando l'iniezione) `IHttpContextAccessor`).

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

Questo viene poi utilizzato da ulteriori metodi di utilità che danno un'interfaccia più piacevole per questi dati.

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

Questo consente di monitorare eventi, URL e identificare gli utenti.

## NugetCity name (optional, probably does not need a translation)

In futuro ho intenzione di trasformarlo in un pacchetto NuGet. Prova per questo ho una voce nel `Umami.Client.csproj` file che genera un nuovo pacchetto "anteprima" versione quando è stato creato in modalità debug.

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

Questo viene aggiunto prima della fine `</Project>` tag nel `.csproj` Archivio.

Dipende da una posizione nuget chiamata 'local' che è definita nel `Nuget.config` Archivio. Che ho mappato in una cartella locale sulla mia macchina.

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

## In conclusione

In futuro ho intenzione di rendere questo un NuGet pa