# Ajout d'un client C# Umami Tracking

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Présentation

Dans un post précédent, nous avons ajouté un client pour récupérer [Données analytiques Umami](/blog/addingascsharpclientforumamiapi)C'est ce que j'ai dit. Dans ce post, nous ajouterons un client pour envoyer des données de suivi à Umami à partir d'une application C#.
[Umami](https://umami.is/) est un service d'analyse léger qui peut être auto-hébergé. C'est une excellente alternative à Google Analytics et est axé sur la vie privée.
Cependant par défaut, il n'a qu'un client Node pour le suivi des données (et même alors ce n'est pas GREAT). J'ai donc décidé d'écrire un client C# pour suivre les données.

### <span style="color:red"> **NOTE J'ai mis à jour ceci tout à l'heure, Je vais mettre à jour le blog post plus tard - Juste maintenant étant 26/08/2024**  </span>

[TOC]

## Préalables

Installer Umami [Tu peux voir comment je fais ça ici.](/blog/usingumamiforlocalanalytics).

## Le client

Vous pouvez voir tout le code source pour le client [Ici.](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Ceci utilise les paramètres que j'ai définis dans mon `appsettings.json` fichier.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Comme l'API de piste n'est pas authentifiée, je n'ai pas ajouté d'authentification au client.

### Configuration

Afin de configurer le client, j'ai ajouté ma méthode d'extension habituelle avec est appelé de votre `Program.cs` fichier.

```csharp
services.SetupUmamiClient(config);
```

Cela fournit un moyen simple d'accrocher dans le `UmamiClient` à votre demande.

Le code ci-dessous montre la méthode de configuration.

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

Comme vous pouvez le voir, cela fait ce qui suit :

1. Configuration de l'objet config
2. Vérifiez que les paramètres sont valides
3. Ajouter un enregistreur (si en mode de débogage)
4. Mettre en place le HttpClient avec l'adresse de base et une politique de réessayer.

### Le client lui-même

Les `UmamiClient` est assez simple. Il a une méthode de base `Send` qui envoie les données de suivi au serveur Umami.

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

Comme vous le verrez, cela utilise un objet appelé `UmamiPayload` qui contient tous les paramètres possibles pour le suivi des requêtes dans Umami.

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

Le seul champ requis est : `Website` qui est l'identifiant du site Web. Le reste est facultatif (mais `Url` est vraiment utile!).............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................

Dans le client, j'ai une méthode appelée `GetPayload()` qui envoie peuple cet objet de charge utile automatiquement avec les informations de la demande (en utilisant l'injecté `IHttpContextAccessor`).

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

Ceci est ensuite utilisé par d'autres méthodes d'utilité qui donnent une interface plus agréable pour ces données.

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

Cela vous permet de suivre les événements, les URL et d'identifier les utilisateurs.

## Nuget

À l'avenir, j'ai l'intention de faire de ça un paquet NuGet. Tests pour cela j'ai une entrée dans le `Umami.Client.csproj` fichier qui génère un nouveau paquet 'preview' versiond lorsqu'il est construit en mode debug.

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

Ceci est ajouté juste avant la fin `</Project>` tag dans le `.csproj` fichier.

Il dépend d'un emplacement nuget appelé 'local' qui est défini dans le `Nuget.config` fichier. Ce que j'ai cartographié dans un dossier local sur ma machine.

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

## En conclusion

À l'avenir, j'ai l'intention de faire un paquet NuGet.
J'utilise ceci dans le blog maintenant, par exemple pour suivre combien de temps les traductions prennent

```csharp
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if (translationTask == null) return TypedResults.BadRequest("Task not found");
        await  umamiClient.Send(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
```