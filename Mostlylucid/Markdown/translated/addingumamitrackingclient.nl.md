# Een C# Umami Tracking-client toevoegen

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Inleiding

In een vorige post hebben we een klant toegevoegd voor het ophalen [Umami-analysegegevens](/blog/addingascsharpclientforumamiapi). In dit bericht voegen we een client toe voor het verzenden van trackinggegevens naar Umami vanuit een C# applicatie.
[Umami](https://umami.is/) is een lichtgewicht analytics service die zelf gehost kan worden. Het is een geweldig alternatief voor Google Analytics en is privacygericht.
Maar standaard heeft het alleen een Knooppunt client voor het bijhouden van gegevens (en zelfs dan is het niet geweldig). Dus besloot ik een C# client te schrijven voor het volgen van data.

[TOC]

## Vereisten

Umami installeren [Je kunt zien hoe ik dit hier doe.](/blog/usingumamiforlocalanalytics).

## De client

U kunt alle broncode voor de client zien [Hier.](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Dit maakt gebruik van instellingen die ik heb gedefinieerd in mijn `appsettings.json` bestand.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Aangezien de track API niet geauthenticeerd is heb ik geen authenticatie toegevoegd aan de client.

### Instellen

Om de client die ik heb toegevoegd mijn gebruikelijke extensie methode met is aangeroepen van uw `Program.cs` bestand.

```csharp
services.SetupUmamiClient(config);
```

Dit biedt een eenvoudige manier om te haak in de `UmamiClient` op uw aanvraag.

De code hieronder toont de setup methode.

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

Zoals je kunt zien doet dit het volgende:

1. Config object instellen
2. Controleer of de instellingen geldig zijn
3. Een logger toevoegen (als in debugmodus)
4. Stel de HttpClient in met het basisadres en een retry policy.

### De Klant zelf

De `UmamiClient` Het is vrij eenvoudig. Het heeft één kernmethode `Send` die de trackinggegevens naar de Umami-server stuurt.

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

Zoals je zult zien gebruikt dit een object genaamd `UmamiPayload` die alle mogelijke parameters bevat voor het volgen van verzoeken in Umami.

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

Het enige vereiste veld is `Website` dat is de website id. De rest is facultatief (maar `Url` is echt nuttig!).

In de client heb ik een methode genaamd `GetPayload()` die stuurt bevolkt dit payload object automatisch met informatie uit de aanvraag (met behulp van de geïnjecteerde `IHttpContextAccessor`).

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

Dit wordt vervolgens gebruikt door verdere utility methoden die een mooiere interface voor deze gegevens geven.

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

Hiermee kunt u gebeurtenissen, URL's bijhouden en gebruikers identificeren.

## Nuget

In de toekomst ben ik van plan om er een NuGet pakket van te maken. Testen voor dat ik heb een vermelding in de `Umami.Client.csproj` bestand dat een nieuw versioned 'preview' pakket genereert wanneer ingebouwd in debug-modus.

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

Dit wordt vlak voor het einde toegevoegd `</Project>` tag in de `.csproj` bestand.

Het hangt af van een nuget locatie genaamd 'local' die wordt gedefinieerd in de `Nuget.config` bestand. Die ik in kaart heb gebracht naar een lokale map op mijn machine.

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

## Conclusie

In de toekomst ben ik van plan om dit een NuGet pa te maken