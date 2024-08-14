# Hinzufügen eines C# Clients für Umami API

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14T01:27</datetime>

## Einleitung

In diesem Beitrag werde ich Ihnen zeigen, wie Sie einen C#-Client für die Umami Reporting API erstellen. Dies ist ein einfaches Beispiel, das zeigt, wie man sich mit der API authentifizieren und Daten daraus abrufen kann.

Hier finden Sie alle Quellcodes für diese [auf meinem GitHub Repo](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[TOC]

## Voraussetzungen

Umami installieren. Die Installationsanleitung finden Sie [Hierher](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) diese Details, wie ich Umami installieren und verwenden, um Analysen für diese Website zur Verfügung zu stellen.

Auch dies ist eine einfache Implementierung von ein paar der Umami Website Stats API-Endpunkte. Sie finden die vollständige API-Dokumentation [Hierher](https://umami.is/docs/api/website-stats).

Hier habe ich mich für die Implementierung der folgenden Endpunkte entschieden:

- `GET /api/websites/:websiteId/pageviews` - Wie der Name schon sagt, gibt dieser Endpunkt die Seitenansichten und "Sessions" für eine bestimmte Website über einen bestimmten Zeitraum zurück.

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

- `GET /api/websites/:websiteId/stats` - dies liefert grundlegende Statistiken für eine bestimmte Website.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - dies liefert die Metriken für eine bestimmte Website bu URL etc...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

Wie Sie an den docs sehen können, akzeptieren diese alle eine Reihe von Parametern (und ich habe diese als Abfrageparameter im Code unten dargestellt).

## Testen im Rider httpClient

Ich starte immer mit dem Testen der API in Riders integriertem HTTP-Client. Dies ermöglicht es mir, die API schnell zu testen und die Antwort zu sehen.

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

Es ist eine gute Praxis, die variablen Namen hier zu behalten. `{{}}` eine env.json-Datei, auf die Sie unten verweisen können.

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

## Einrichtung

Zuerst müssen wir HttpClient und die Dienste konfigurieren, die wir verwenden, um die Anfragen zu stellen.

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

Hier konfigurieren wir die Einstellungsklasse `UmamiSettings` und fügen Sie die `AuthService` und `UmamiService` zur Erhebung von Dienstleistungen. Wir fügen dem HttpClient auch eine Retry-Richtlinie hinzu, um vorübergehende Fehler zu handhaben.

Als nächstes müssen wir die `UmamiService` und `AuthService` Unterricht.

Das `AuthService` ist einfach verantwortlich für den Erhalt des JWT-Tokens von der API.

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

Hier haben wir eine einfache Methode `LoginAsync` die eine POST-Anfrage an die `/api/auth/login` Endpunkt mit Benutzername und Passwort. Wenn die Anfrage erfolgreich ist, speichern wir das JWT Token im `_token` Feld und setzen Sie die `Authorization` Header auf dem HttpClient.

Das `UmamiService` ist verantwortlich für die Anfragen an die API.
Für jede der Hauptmethoden habe ich Request-Objekte definiert, die alle Parameter für jeden Endpunkt akzeptieren. Dadurch ist es einfacher, den Code zu testen und zu pflegen.

Sie alle folgen einem ähnlichen Muster, also zeige ich einfach einen von ihnen hier.

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

Hier können Sie sehen, dass ich das Anfrageobjekt nehme

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

Und den Abfragestring aus den Parametern erstellen. Wenn die Anfrage erfolgreich ist, geben wir den Inhalt als `UmamiResult` Gegenstand. Wenn die Anfrage mit einem 401 Statuscode fehlschlägt, rufen wir die `LoginAsync` Methode und wiederholen Sie die Anfrage. Dies stellt sicher, dass wir "elegant" den Token-Auslauf handhaben.

## Schlußfolgerung

Dies ist ein einfaches Beispiel, wie man einen C#-Client für die Umami API erstellt. Sie können dies als Ausgangspunkt nutzen, um komplexere Clients zu erstellen oder die API in Ihre eigenen Anwendungen zu integrieren.