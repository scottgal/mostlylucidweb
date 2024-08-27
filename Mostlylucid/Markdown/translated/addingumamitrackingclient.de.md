# Hinzufügen eines C# Umami-Tracking-Clients

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Einleitung

In einem früheren Beitrag haben wir einen Client zum Abrufen hinzugefügt [Daten zur Umami-Analyse](/blog/addingascsharpclientforumamiapi)......................................................................................................... In diesem Beitrag werden wir einen Client für das Senden von Tracking-Daten an Umami von einer C#-Anwendung hinzufügen.
[Umami](https://umami.is/) ist ein leichtgewichtiger Analysedienst, der selbst gehostet werden kann. Es ist eine großartige Alternative zu Google Analytics und ist datenschutzorientiert.
Allerdings hat es standardmäßig nur einen Node-Client für das Tracking von Daten (und selbst dann ist es nicht GREAT). Also beschloss ich, einen C#-Client zu schreiben, um Daten zu verfolgen.

### <span style="color:red"> **HINWEIS Ich aktualisierte dies gerade jetzt, Ich werde den Blog-Post später aktualisieren - Gerade jetzt ist 26/08/2024**  </span>

[TOC]

## Voraussetzungen

Umami installieren [Sie können sehen, wie ich das hier mache](/blog/usingumamiforlocalanalytics).

## Der Kunde

Sie können alle Quellcode für den Client sehen [Hierher](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Dies benutzt Einstellungen, die ich in meinem `appsettings.json` ..............................................................................................................................

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Da die Track-API nicht authentifiziert ist, habe ich dem Client keine Authentifizierung hinzugefügt.

### Einrichtung

Um den Client einzurichten, habe ich meine übliche Erweiterungsmethode mit hinzugefügt wird von Ihrem aufgerufen `Program.cs` ..............................................................................................................................

```csharp
services.SetupUmamiClient(config);
```

Dies bietet eine einfache Möglichkeit, in der `UmamiClient` zu Ihrer Bewerbung.

Der Code unten zeigt die Setup-Methode an.

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

Wie Sie sehen können, macht dies folgendes:

1. Einrichtung des Config-Objekts
2. Überprüfen Sie die Einstellungen sind gültig
3. Logger hinzufügen (falls im Debug-Modus)
4. Richten Sie den HttpClient mit der Basisadresse und einer Retry-Richtlinie ein.

### Der Kunde selbst

Das `UmamiClient` ist ziemlich einfach. Es hat eine Kernmethode `Send` die die Tracking-Daten an den Umami-Server sendet.

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

Wie Sie sehen werden, verwendet dies ein Objekt namens `UmamiPayload` die alle möglichen Parameter für die Verfolgung von Anfragen in Umami enthält.

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

Das einzige erforderliche Feld ist `Website` das ist die Website-ID. Der Rest ist optional (aber `Url` ist wirklich nützlich!)== Einzelnachweise ==

Im Client habe ich eine Methode namens `GetPayload()` die dieses Nutzlastobjekt automatisch mit Informationen aus der Anfrage bevölkert (mit dem injizierten `IHttpContextAccessor`).

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

Dies wird dann durch weitere Utility-Methoden, die eine schönere Schnittstelle für diese Daten geben verwendet.

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

Auf diese Weise können Sie Ereignisse, URLs verfolgen und Benutzer identifizieren.

## Nuget

In Zukunft plane ich, dies zu einem NuGet-Paket zu machen. Testen dafür habe ich einen Eintrag in der `Umami.Client.csproj` Datei, die beim Bau im Debug-Modus ein neues versioniertes 'Preview'-Paket generiert.

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

Dies wird direkt vor Ende hinzugefügt. `</Project>` tag in der `.csproj` ..............................................................................................................................

Es hängt von einem Nuget-Standort namens 'lokal' ab, der im `Nuget.config` .............................................................................................................................. Was ich in einen lokalen Ordner auf meinem Rechner gemappt habe.

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

## Schlussfolgerung

In Zukunft plane ich, dies zu einem NuGet-Paket zu machen.
Ich benutze dies jetzt im Blog, zum Beispiel, um zu verfolgen, wie lange Übersetzungen dauern

```csharp
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (translationTask == null) return TypedResults.BadRequest("Task not found");
        await  umamiClient.Send(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
```