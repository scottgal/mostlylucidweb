# Een C#-client toevoegen voor Umami API

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14T01:27</datetime>

## Imtroductie

In dit bericht, zal ik u laten zien hoe u een C# client voor de Umami rapportage API. Dit is een eenvoudig voorbeeld dat laat zien hoe je met de API authenticeert en er gegevens uit ophaalt.

Hiervoor kunt u alle broncode vinden [op mijn GitHub repo](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[TOC]

## Vereisten

Installeer Umami. U kunt de installatie-instructies vinden [Hier.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) deze details hoe ik Umami installeer en gebruik om analytics voor deze site te leveren.

Nogmaals, dit is een eenvoudige implementatie van een paar van de Umami Website Stats API eindpunten. U vindt de volledige API documentatie [Hier.](https://umami.is/docs/api/website-stats).

Hierin heb ik ervoor gekozen om de volgende eindpunten te implementeren:

- `GET /api/websites/:websiteId/pageviews` - Zoals de naam al doet vermoeden, geeft dit eindpunt de pageviews en'sessies' terug voor een bepaalde website over een bepaalde periode.

```json
{
  "pageviews": [
    { "x": "2020-04-20 01:00:00", "y": 3 },
    { "x": "2020-04-20 02:00:00", "y": 7 }
  ],
  "sessions": [
    { "x": "2020-04-20 01:00:00", "y": 2 },
    { "x": "2020-04-20 02:00:00", "y": 4 }
  ]
}
```

- `GET /api/websites/:websiteId/stats` - dit geeft basisstatistieken voor een bepaalde website terug.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - dit geeft de metrics terug voor een bepaalde website bu URL etc...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

Zoals je kunt zien uit de documenten, accepteren deze allemaal een aantal parameters (en ik heb deze weergegeven als query parameters in de onderstaande code).

## Testen in Rider httpClient

Ik begin altijd met het testen van de API in Rider's ingebouwde HTTP client. Dit stelt me in staat om snel de API te testen en de reactie te zien.

```http
### Login Request and Store Token
POST https://{{umamiurl}}/api/auth/login
Content-Type: application/json

{
  "username": "{{username}}",

  "password": "{{password}}"
}
> {% client.global.set("auth_token", response.body.token);
    client.global.set("endAt", Math.round(new Date().getTime()).toString() );
    client.global.set("startAt", Math.round(new Date().getTime() - 7 * 24 * 60 * 60 * 1000).toString());
%}


### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/stats?endAt={{endAt}}&startAt={{startAt}}
Authorization: Bearer {{auth_token}}

### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/pageviews?endAt={{endAt}}&startAt={{startAt}}&unit=day
Authorization: Bearer {{auth_token}}


###
GET https://{{umamiurl}}}}/api/websites/{{websiteid}}/metrics?endAt={{endAt}}&startAt={{startAt}}&type=url
Authorization: Bearer {{auth_token}}
```

Het is een goede gewoonte om de variabele namen hier in te houden. `{{}}` een env.json-bestand waarnaar u zoals hieronder kunt verwijzen.

```json
{
  "local": {
    "umamiurl":"umamilocal.mostlylucid.net",
    "username": "admin",
    "password": "<password{>",
    "websiteid" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  }
}
```

## Instellen

Eerst moeten we HttpClient configureren en de diensten die we zullen gebruiken om de verzoeken te doen.

```csharp

public static class UmamiSetup
{
    public static void SetupUmamiServices(this IServiceCollection services, IConfiguration config)
    {
        var umamiSettings = services.ConfigurePOCO<UmamiSettings>(config.GetSection(UmamiSettings.Section));
        services.AddHttpClient<AuthService>(options =>
        {
            options.BaseAddress = new Uri(umamiSettings.BaseUrl);
            
        }) .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy());;
        services.AddScoped<UmamiService>();
        services.AddScoped<AuthService>();

    }
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

}
```

Hier configureren we de instellingenklasse `UmamiSettings` en voeg de `AuthService` en `UmamiService` de verzameling van diensten. We voegen ook een retrybeleid toe aan de HttpClient om tijdelijke fouten te verwerken.

Vervolgens moeten we de `UmamiService` en `AuthService` Lessen.

De `AuthService` is gewoon verantwoordelijk voor het verkrijgen van de JWT token van de API.

```csharp
public class AuthService(HttpClient httpClient, UmamiSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token;
    public HttpClient HttpClient => httpClient;

    public async Task<bool> LoginAsync()
    {
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();


            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
}
```

Hier hebben we een eenvoudige methode `LoginAsync` die een POST-verzoek naar de `/api/auth/login` eindpunt met gebruikersnaam en wachtwoord. Als het verzoek succesvol is, slaan we de JWT token op in de `_token` veld en stel de `Authorization` Header op de HttpClient.

De `UmamiService` is verantwoordelijk voor het indienen van de verzoeken aan de API.
Voor elk van de belangrijkste methoden heb ik objecten gedefinieerd die alle parameters voor elk eindpunt accepteren. Dit maakt het makkelijker om de code te testen en te onderhouden.

Ze volgen allemaal een gelijkaardig patroon, dus ik zal er hier een laten zien.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStatsAsync(StatsRequest statsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={statsRequest.StartAt}",
        $"end={statsRequest.EndAt}"
    };

    // Add optional parameters if they are not null
    if (!string.IsNullOrEmpty(statsRequest.Url)) queryParams.Add($"url={statsRequest.Url}");
    if (!string.IsNullOrEmpty(statsRequest.Referrer)) queryParams.Add($"referrer={statsRequest.Referrer}");
    if (!string.IsNullOrEmpty(statsRequest.Title)) queryParams.Add($"title={statsRequest.Title}");
    if (!string.IsNullOrEmpty(statsRequest.Query)) queryParams.Add($"query={statsRequest.Query}");
    if (!string.IsNullOrEmpty(statsRequest.Event)) queryParams.Add($"event={statsRequest.Event}");
    if (!string.IsNullOrEmpty(statsRequest.Host)) queryParams.Add($"host={statsRequest.Host}");
    if (!string.IsNullOrEmpty(statsRequest.Os)) queryParams.Add($"os={statsRequest.Os}");
    if (!string.IsNullOrEmpty(statsRequest.Browser)) queryParams.Add($"browser={statsRequest.Browser}");
    if (!string.IsNullOrEmpty(statsRequest.Device)) queryParams.Add($"device={statsRequest.Device}");
    if (!string.IsNullOrEmpty(statsRequest.Country)) queryParams.Add($"country={statsRequest.Country}");
    if (!string.IsNullOrEmpty(statsRequest.Region)) queryParams.Add($"region={statsRequest.Region}");
    if (!string.IsNullOrEmpty(statsRequest.City)) queryParams.Add($"city={statsRequest.City}");

    // Combine the query parameters into a query string
    var queryString = string.Join("&", queryParams);

    // Make the HTTP request
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/stats?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<StatsResponseModels>();
        return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new StatsResponseModels());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetStatsAsync(statsRequest);
    }

    logger.LogError("Failed to get stats");
    return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get stats", null);
}

```

Hier kunt u zien Ik neem het verzoek object

```csharp
public class BaseRequest
{
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
public class StatsRequest : BaseRequest
{
    // Optional properties
    public string? Url { get; set; } // Name of URL
    public string? Referrer { get; set; } // Name of referrer
    public string? Title { get; set; } // Name of page title
    public string? Query { get; set; } // Name of query
    public string? Event { get; set; } // Name of event
    public string? Host { get; set; } // Name of hostname
    public string? Os { get; set; } // Name of operating system
    public string? Browser { get; set; } // Name of browser
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    public string? Country { get; set; } // Name of country
    public string? Region { get; set; } // Name of region/state/province
    public string? City { get; set; } // Name of city
}
```

En bouw de query string van de parameters. Als het verzoek succesvol is, retourneren we de inhoud als een `UmamiResult` object. Als het verzoek mislukt met een 401 status code, bellen we de `LoginAsync` methode en opnieuw proberen van het verzoek. Dit zorgt ervoor dat we 'elegant' omgaan met de token verlopen.

## Conclusie

Dit is een eenvoudig voorbeeld van hoe je een C# client maakt voor de Umami API. U kunt dit als uitgangspunt gebruiken om complexere klanten te bouwen of de API te integreren in uw eigen toepassingen.