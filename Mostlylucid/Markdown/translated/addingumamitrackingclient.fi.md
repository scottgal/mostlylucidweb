# Lisään C# Umami -seuranta- asiakkaan

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-18T20-13</datetime>

## Johdanto

Edellisessä viestissä lisäsimme asiakkaan noudettavaksi [Umamin analytiikkatiedot](/blog/addingascsharpclientforumamiapi)...................................................................................................................................... Tässä viestissä lisäämme asiakkaan seurantatietojen lähettämiseen Umamille C#-sovelluksesta.
[Umami](https://umami.is/) on kevyt analytiikkapalvelu, jota voi isännöidä itse. Se on loistava vaihtoehto Google Analyticsille ja keskittyy yksityisyyteen.
Sillä on kuitenkin oletuksena vain solmuasiakas tietojen seurantaan (ja silloinkaan se ei ole SUURI). Joten päätin kirjoittaa C#-asiakkaan datan jäljittämiseen.

[TÄYTÄNTÖÖNPANO

## Edeltävät opinnot

Asenna Umami [näet, miten teen tämän täällä](/blog/usingumamiforlocalanalytics).

## Asiakas

Asiakkaan kaikki lähdekoodit ovat nähtävillä [täällä](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net).

Tämä käyttää asetuksia, jotka olen määritellyt omassani `appsettings.json` Kansio.

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo"
 },
```

Koska radan API-rajapintaa ei ole vahvistettu, en ole lisännyt asiakkaan varmennusta.

### Asetukset

Tavanomaiseen laajennusmenetelmääni lisänneen asiakkaan lavastamiseksi kutsutaan sinun `Program.cs` Kansio.

```csharp
services.SetupUmamiClient(config);
```

Tämä tarjoaa yksinkertaisen tavan koukuttaa `UmamiClient` hakemukseenne.

Alla oleva koodi näyttää asetusmenetelmän.

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

Kuten näette, tämä toimii seuraavasti:

1. Aseta konfigurointiobjekti
2. Tarkista asetukset kelvollisiksi
3. Lisää loggeri (jos vianetsintätilassa)
4. Aseta HttpClient perusosoitteella ja uusintakäytännöllä.

### Asiakas itse

Erytropoietiini `UmamiClient` Se on melko yksinkertaista. Sillä on yksi ydinmenetelmä `Send` joka lähettää seurantatiedot Umami-palvelimelle.

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

Kuten näette, tämä käyttää esinettä nimeltä `UmamiPayload` joka sisältää kaikki mahdolliset parametrit pyyntöjen seuraamiseksi Umamissa.

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

Ainoa vaadittu kenttä on `Website` joka on verkkosivun tunniste. Loput ovat valinnaisia (mutta `Url` siitä on todella hyötyä!).

Asiakkaassa minulla on menetelmä nimeltä `GetPayload()` joka lähettää tämän hyötykuormaobjektin väkijoukon automaattisesti pyynnöstä saaduilla tiedoilla (käyttäen injektoitua `IHttpContextAccessor`).

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

Tämän jälkeen käytetään muita hyödyllisyysmenetelmiä, jotka antavat mukavamman käyttöliittymän näille tiedoille.

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

Näin voit seurata tapahtumia, URL-osoitteita ja tunnistaa käyttäjät.

## Nuget

Tulevaisuudessa aion tehdä tästä NuGet-paketin. Testaa sitä varten minulla on merkintä `Umami.Client.csproj` Tiedosto, joka luo uuden versioidun "esikatselupaketin", kun se on rakennettu vianetsintätilaan.

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

Tämä lisätään juuri ennen loppua `</Project>` lapussa `.csproj` Kansio.

Se riippuu "paikallisesta" niukasta sijainnista, joka määritellään `Nuget.config` Kansio. Jonka olen kartoittanut paikalliseen kansioon koneessani.

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

## Johtopäätöksenä

Tulevaisuudessa aion tehdä tästä NuGet pa