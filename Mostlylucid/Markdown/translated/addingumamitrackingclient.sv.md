# Lägga till en C# Umami Tracking- klient

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Inledning

I ett tidigare inlägg lade vi till en klient för att hämta [Uppgifter om Umamianalys](/blog/addingascsharpclientforumamiapi)....................................... I det här inlägget kommer vi att lägga till en klient för att skicka spårningsdata till Umami från ett C#-program.
[Umami Ordförande](https://umami.is/) är en lättviktig analystjänst som kan vara självförsörjande. Det är ett bra alternativ till Google Analytics och är integritetsfokuserat.
Men som standard har den bara en Node-klient för att spåra data (och även då är det inte GREAT). Så jag bestämde mig för att skriva en C#-klient för att spåra data.

[TOC]

## Förutsättningar

Installera Umami [Du kan se hur jag gör det här.](/blog/usingumamiforlocalanalytics).

## Kunden

Du kan se alla källkoden för klienten [här](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Detta använder inställningar som jag har definierat i min `appsettings.json` En akt.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Eftersom spår API:et inte är autentiserat har jag inte lagt till någon autentisering till klienten.

### Ställ in

För att ställa in klienten jag har lagt till min sedvanliga förlängning metod med kallas från din `Program.cs` En akt.

```csharp
services.SetupUmamiClient(config);
```

Detta ger ett enkelt sätt att kroka i `UmamiClient` till din ansökan.

Koden nedan visar inställningsmetoden.

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

Som du kan se gör detta följande:

1. Ställ in inställningsobjektet
2. Kontrollera att inställningarna är giltiga
3. Lägg till en logger (om i felsökningsläge)
4. Sätt upp HttpClienten med basadressen och en ny policy.

### Kunden själv

I detta sammanhang är det viktigt att se till att `UmamiClient` är ganska enkelt. Det har en kärnmetod `Send` som skickar spårningsdata till Umami-servern.

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

Som du kommer att se detta använder ett objekt som kallas `UmamiPayload` som innehåller alla möjliga parametrar för att spåra förfrågningar i Umami.

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

Det enda fält som krävs är `Website` vilket är webbplatsens id. Resten är frivilliga (men `Url` är verkligen användbart!)..............................................................................................

I klienten har jag en metod som kallas `GetPayload()` som skickar populates detta nyttolast objekt automatiskt med information från begäran (med hjälp av den injicerade `IHttpContextAccessor`).

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

Detta används sedan av ytterligare nyttometoder som ger en trevligare gränssnitt för dessa data.

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

Detta gör att du kan spåra händelser, webbadresser och identifiera användare.

## Nugga

I framtiden planerar jag att göra detta till ett NuGet-paket. Test för att jag har en post i `Umami.Client.csproj` fil som genererar ett nytt versionsanpassat "förhandsgranskningspaket" när det är byggt i felsökningsläge.

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

Detta läggs till precis före slutet `</Project>` taggen i `.csproj` En akt.

Det beror på en nuget plats som kallas "lokal" som definieras i `Nuget.config` En akt. Som jag har kartlagt till en lokal mapp på min maskin.

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

## Slutsatser

I framtiden planerar jag att göra detta en NuGet pa