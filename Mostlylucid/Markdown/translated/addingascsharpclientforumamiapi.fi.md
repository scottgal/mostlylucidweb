# Lisää C#-asiakas Umamin sovellusrajapinnalle

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14T01:27</datetime>

## Imtroduktio

Tässä viestissä näytän, miten luodaan C#-asiakas Umamin raportointirajapinnalle. Tämä on yksinkertainen esimerkki, joka osoittaa, miten API voidaan todentaa ja hakea siitä tietoja.

Kaikki lähdekoodit löydät tästä [GitHub-repollani](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[TÄYTÄNTÖÖNPANO

## Edeltävät opinnot

Asenna Umami. Asennusohjeet löydät [täällä](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) tässä yksityiskohtaiset tiedot siitä, miten asennan ja käytän Umamia analytiikan toimittamiseen tälle sivustolle.

Tämäkin on yksinkertainen toteutus muutamalle Umami Website Stats API -päätteelle. API-dokumentaatio löytyy kokonaisuudessaan [täällä](https://umami.is/docs/api/website-stats).

Tässä olen päättänyt toteuttaa seuraavat päätetapahtumat:

- `GET /api/websites/:websiteId/pageviews` - Kuten nimestä voi päätellä, tämä päätetapahtuma palauttaa sivut ja "istunnot" tietylle nettisivulle tietyn ajan kuluessa.

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

- `GET /api/websites/:websiteId/stats` - tämä palauttaa perustilastot tietylle nettisivulle.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - tämä palauttaa metrit tietyn verkkosivuston bu URL-osoitteelle jne....

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

Kuten dokumenteista käy ilmi, nämä kaikki hyväksyvät useita parametreja (ja olen edustanut näitä kyselyparametreina alla olevassa koodissa).

## Testaaminen Riderissä http: / / www.iltasanomat.fi / haku /? search-term = Lient% 20Client% C3% A4

Aloitan aina testaamalla Riderin sisäänrakennetun HTTP-asiakkaan API:tä. Näin voin nopeasti testata API:n ja nähdä vastauksen.

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

On hyvä käytäntö pitää muuttujanimet täällä `{{}}` Env.json-tiedosto, johon voit viitata kuten alla.

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

## Asetukset

Ensin on määriteltävä HttpClient ja palvelut, joita käytämme pyyntöjen tekemiseen.

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

Määrittelemme tässä asetusluokan `UmamiSettings` ja lisää `AuthService` sekä `UmamiService` palvelukokoelmaan. Lisäämme HttpClientiin myös uuden käytännön, jolla käsitellään ohimeneviä virheitä.

Seuraavaksi meidän on luotava `UmamiService` sekä `AuthService` kursseja.

Erytropoietiini `AuthService` on yksinkertaisesti vastuussa JWT-todennuksen saamisesta API:stä.

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

Tässä meillä on yksinkertainen menetelmä `LoginAsync` joka lähettää POST-pyynnön `/api/auth/login` päätetapahtuma käyttäjätunnuksella ja salasanalla. Jos pyyntö onnistuu, tallennamme JWT-kyltin `_token` Kenttä ja asettaa `Authorization` HttpClientin otsikko.

Erytropoietiini `UmamiService` vastaa API:n pyyntöjen tekemisestä.
Jokaiselle päämenetelmälle olen määritellyt pyyntökohteet, jotka hyväksyvät jokaisen päätetapahtuman kaikki muuttujat. Tämä helpottaa koodin testausta ja ylläpitoa.

He kaikki noudattavat simiirikuviota, joten näytän yhden heistä täällä.

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

Tässä näette, että otan pyynnön vastaan

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

Ja rakenna kyselyjono parametreista. Jos pyyntö onnistuu, palautamme sisällön `UmamiResult` Esine. Jos pyyntö epäonnistuu 401 tilakoodilla, soitamme `LoginAsync` metodi ja kokeile pyyntöä uudelleen. Tämä takaa sen, että käsittelemme "eleganttisti" symbolisen vanhenemisen.

## Päätelmät

Tämä on yksinkertainen esimerkki siitä, miten luodaan C#-asiakas Umamin sovellusrajapinnalle. Voit käyttää tätä lähtökohtana rakentaaksesi monimutkaisempia asiakkaita tai integroidaksesi API:n omiin sovelluksiisi.