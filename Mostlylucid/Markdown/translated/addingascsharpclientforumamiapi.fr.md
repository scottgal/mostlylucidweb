# Ajout d'un client C# pour l'API Umami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14T01:27</datetime>

## Imtroduction

Dans ce post, je vais vous montrer comment créer un client C# pour l'API de reporting Umami. C'est un exemple simple qui montre comment authentifier avec l'API et récupérer les données de celui-ci.

Vous pouvez trouver tout le code source pour ceci [sur ma repo GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[TOC]

## Préalables

Installez Umami. Vous pouvez trouver les instructions d'installation [Ici.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) ce détail comment j'installe et utilise Umami pour fournir des analyses pour ce site.

Encore une fois, il s'agit d'une implémentation simple de quelques-uns des paramètres de l'API Umami Website Stats. Vous pouvez trouver la documentation complète de l'API [Ici.](https://umami.is/docs/api/website-stats).

Dans ce cas, j'ai choisi de mettre en œuvre les paramètres suivants:

- `GET /api/websites/:websiteId/pageviews` - Comme son nom l'indique, ce paramètre renvoie les pagesviews et les «sessions» d'un site web donné sur une période donnée.

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

- `GET /api/websites/:websiteId/stats` - qui renvoie des statistiques de base pour un site web donné.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - cela retourne les métriques d'une URL bu de site web donnée etc...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

Comme vous pouvez le voir dans les documents, ils acceptent tous un certain nombre de paramètres (et j'ai représenté ceux-ci comme paramètres de requête dans le code ci-dessous).

## Essais dans Rider httpClient

Je commence toujours par tester l'API dans le client HTTP intégré de Rider. Cela me permet de tester rapidement l'API et de voir la réponse.

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

C'est une bonne pratique de garder les noms de variables ici dans `{{}}` un fichier env.json auquel vous pouvez vous référer comme ci-dessous.

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

## Configuration

Nous devons d'abord configurer HttpClient et les services que nous utiliserons pour faire les requêtes.

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

Ici, nous configurerons la classe de paramètres `UmamiSettings` et ajouter le `AuthService` et `UmamiService` à la collecte des services. Nous ajoutons également une politique de réessayer au HttpClient pour gérer les erreurs transitoires.

Ensuite, nous devons créer le `UmamiService` et `AuthService` les cours.

Les `AuthService` est simplement responsable d'obtenir le jeton JWT de l'API.

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

Ici, nous avons une méthode simple `LoginAsync` qui envoie une demande POST à la `/api/auth/login` endpoint avec le nom d'utilisateur et le mot de passe. Si la demande est acceptée, nous stockons le jeton JWT dans le `_token` champ et définir le `Authorization` l'en-tête sur le HttpClient.

Les `UmamiService` est responsable de faire les demandes à l'API.
Pour chacune des méthodes principales, j'ai défini des objets request qui acceptent tous les paramètres de chaque paramètre. Il est ainsi plus facile de tester et de maintenir le code.

Ils suivent tous un schéma simiaire, donc je vais juste en montrer un ici.

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

Ici vous pouvez voir que je prends l'objet request

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

Et construisez la chaîne de requête à partir des paramètres. Si la demande est acceptée, nous renvoyons le contenu en tant que `UmamiResult` objet. Si la requête échoue avec un code d'état 401, nous appelons le `LoginAsync` méthode et réessayer la demande. Cela garantit que nous nous occupons 'élégamment' de l'expiration du jeton.

## Conclusion

C'est un exemple simple de la façon de créer un client C# pour l'API Umami. Vous pouvez l'utiliser comme point de départ pour construire des clients plus complexes ou intégrer l'API dans vos propres applications.