# Προσθήκη ενός πελάτη εντοπισμού C# Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20:13</datetime>

## Εισαγωγή

Σε μια προηγούμενη ανάρτηση προσθέσαμε έναν πελάτη για την απόκτηση [Δεδομένα ανάλυσης Umami](/blog/addingascsharpclientforumamiapi). Σε αυτή την ανάρτηση θα προσθέσουμε έναν πελάτη για την αποστολή δεδομένων παρακολούθησης στο Umami από μια εφαρμογή C#.
[ΟυμάμιCity name (optional, probably does not need a translation)](https://umami.is/) είναι μια ελαφριά υπηρεσία ανάλυσης που μπορεί να αυτο-φιλοξενηθεί. Είναι μια μεγάλη εναλλακτική λύση για το Google Analytics και είναι εστιασμένη στην ιδιωτικότητα.
Ωστόσο, από προεπιλογή έχει μόνο ένα πελάτη κόμβου για την παρακολούθηση δεδομένων (και ακόμη και τότε δεν είναι GREAT). Έτσι αποφάσισα να γράψω έναν πελάτη C# για την παρακολούθηση δεδομένων.

[TOC]

## Προαπαιτούμενα

Εγκαταστήστε το Umami [Μπορείς να δεις πώς το κάνω αυτό εδώ.](/blog/usingumamiforlocalanalytics).

## Ο Πελάτης

Μπορείτε να δείτε όλο τον πηγαίο κώδικα για τον πελάτη [Ορίστε.](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Αυτό χρησιμοποιεί ρυθμίσεις που έχω καθορίσει στο `appsettings.json` Φάκελος.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Δεδομένου ότι το κομμάτι API δεν είναι επικυρωμένο δεν έχω προσθέσει καμία εξακρίβωση ταυτότητας στον πελάτη.

### Ρύθμιση

Για να ρυθμίσετε τον πελάτη Έχω προσθέσει τη συνήθη μέθοδο επέκτασης μου με καλείται από σας `Program.cs` Φάκελος.

```csharp
services.SetupUmamiClient(config);
```

Αυτό παρέχει έναν απλό τρόπο για να γαντζωθεί στο `UmamiClient` στην αίτησή σας.

Ο παρακάτω κωδικός δείχνει τη μέθοδο ρύθμισης.

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

Όπως μπορείτε να δείτε αυτό κάνει τα ακόλουθα:

1. Ρυθμίστε το αντικείμενο ρυθμίσεων
2. Έλεγχος των ρυθμίσεων είναι έγκυρες
3. Προσθήκη ενός logger (αν σε λειτουργία αποσφαλμάτωσης)
4. Ρυθμίστε το HttpClient με τη βασική διεύθυνση και μια πολιτική επαναπροσπάθειας.

### Ο ίδιος ο Πελάτης

Η `UmamiClient` Είναι αρκετά απλό. Έχει μία βασική μέθοδο. `Send` Που στέλνει τα δεδομένα εντοπισμού στον διακομιστή Umami.

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

Όπως θα δείτε αυτό χρησιμοποιεί ένα αντικείμενο που ονομάζεται `UmamiPayload` που περιέχει όλες τις πιθανές παραμέτρους για την παρακολούθηση των αιτήσεων στο Umami.

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

Το μόνο απαιτούμενο πεδίο είναι `Website` που είναι η ταυτότητα της ιστοσελίδας. Τα υπόλοιπα είναι προαιρετικά (αλλά `Url` Είναι πραγματικά χρήσιμο!).

Στον πελάτη έχω μια μέθοδο που ονομάζεται `GetPayload()` που στέλνει αυτό το αντικείμενο ωφέλιμο φορτίο αυτόματα με πληροφορίες από το αίτημα (χρησιμοποιώντας την ένεση) `IHttpContextAccessor`).

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

Αυτό χρησιμοποιείται στη συνέχεια από περαιτέρω μεθόδους χρησιμότητας που δίνουν μια καλύτερη διεπαφή για αυτά τα δεδομένα.

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

Αυτό σας επιτρέπει να παρακολουθείτε τα γεγονότα, τα URL και την αναγνώριση των χρηστών.

## ΝαγκέτCity name (optional, probably does not need a translation)

Στο μέλλον σκοπεύω να το κάνω πακέτο NuGet. Δοκιμή γι 'αυτό Έχω μια καταχώρηση στο `Umami.Client.csproj` αρχείο που δημιουργεί ένα νέο εκδοθέν πακέτο 'προεπισκόπηση' όταν είναι ενσωματωμένο σε λειτουργία αποσφαλμάτωσης.

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

Αυτό προστίθεται ακριβώς πριν από το τέλος `</Project>` tag in the `.csproj` Φάκελος.

Εξαρτάται από μια τοποθεσία nuget που ονομάζεται "τοπική" η οποία ορίζεται στο `Nuget.config` Φάκελος. Το οποίο έχω χαρτογραφήσει σε έναν τοπικό φάκελο στο μηχάνημά μου.

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

## Συμπέρασμα

Στο μέλλον σκοπεύω να κάνω αυτό ένα NuG πάρε pa