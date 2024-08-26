# Προσθήκη ενός πελάτη C# για Umami API

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-14T01:27</datetime>

## Εμβολή

Σε αυτό το άρθρο, θα σας δείξω πώς να δημιουργήσετε έναν πελάτη C# για την αναφορά API Umami. Αυτό είναι ένα απλό παράδειγμα που δείχνει πώς να πιστοποιήσει με το API και να ανακτήσει τα δεδομένα από αυτό.

Μπορείτε να βρείτε όλο τον πηγαίο κώδικα για αυτό [στο GitHub repo μου](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[TOC]

## Προαπαιτούμενα

Εγκαταστήστε το Umami. Μπορείτε να βρείτε τις οδηγίες εγκατάστασης [Ορίστε.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) Αυτή η λεπτομέρεια πώς εγκαθιστώ και χρησιμοποιώ Umami για να παρέχω analytics για αυτό το site.

Και πάλι, αυτό είναι μια απλή εφαρμογή μερικών από τα τελικά σημεία της ιστοσελίδας Umami API. Μπορείτε να βρείτε την πλήρη τεκμηρίωση API [Ορίστε.](https://umami.is/docs/api/website-stats).

Σε αυτό επέλεξα να εφαρμόσω τα ακόλουθα τελικά σημεία:

- `GET /api/websites/:websiteId/pageviews` - Όπως υποδηλώνει το όνομα, αυτό το τελικό σημείο επιστρέφει τις εικόνες και τις "συνεδριάσεις" για μια δεδομένη ιστοσελίδα για μια χρονική περίοδο.

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

- `GET /api/websites/:websiteId/stats` - αυτό επιστρέφει βασικά στατιστικά στοιχεία για μια δεδομένη ιστοσελίδα.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - αυτό επιστρέφει τις μετρήσεις για μια δεδομένη ιστοσελίδα bu URL κλπ...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

Όπως μπορείτε να δείτε από τα έγγραφα, όλα αυτά δέχονται μια σειρά παραμέτρων (και εγώ τις εκπροσωπώ ως παραμέτρους ερώτημα στον παρακάτω κώδικα).

## Δοκιμή σε Rider httpClient

Ξεκινάω πάντα δοκιμάζοντας το API στο ενσωματωμένο πελάτη HTTP του Rider. Αυτό μου επιτρέπει να δοκιμάσω γρήγορα το API και να δω την απάντηση.

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

Είναι καλή πρακτική να κρατάς τα ονόματα των μεταβλητών εδώ μέσα. `{{}}` ένα αρχείο env.json το οποίο μπορείτε να αναφέρετε όπως παρακάτω.

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

## Ρύθμιση

Πρώτα πρέπει να ρυθμίσουμε HttpClient και τις υπηρεσίες που θα χρησιμοποιήσουμε για να κάνουμε τα αιτήματα.

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

Εδώ θα ρυθμίσουμε την τάξη ρυθμίσεων `UmamiSettings` και προσθέστε το `AuthService` και `UmamiService` στη συλλογή των υπηρεσιών. Προσθέτουμε επίσης μια πολιτική επαναπροσπάθειας στο HttpClient για να χειριστεί παροδικά λάθη.

Στη συνέχεια πρέπει να δημιουργήσουμε το `UmamiService` και `AuthService` Μαθήματα.

Η `AuthService` είναι απλά υπεύθυνος για να πάρει το σύμβολο JWT από το API.

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

Εδώ έχουμε μια απλή μέθοδο. `LoginAsync` που στέλνει ένα αίτημα POST στην `/api/auth/login` τελικό σημείο με το όνομα χρήστη και τον κωδικό πρόσβασης. Αν η αίτηση είναι επιτυχής, θα αποθηκεύσουμε το σήμα JWT στο `_token` πεδίο και να ρυθμίσετε το `Authorization` Επικεφαλίδα στο HttpClient.

Η `UmamiService` είναι υπεύθυνος για την υποβολή των αιτήσεων στο API.
Για κάθε μία από τις κύριες μεθόδους έχω ορίσει αντικείμενα αίτησης που δέχονται όλες τις παραμέτρους για κάθε τελικό σημείο. Αυτό διευκολύνει τη δοκιμή και τη διατήρηση του κώδικα.

Όλοι ακολουθούν ένα παρόμοιο μοτίβο, οπότε θα δείξω ένα από αυτά εδώ.

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

Εδώ μπορείτε να δείτε ότι παίρνω το αντικείμενο αίτησης

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

Και φτιάξε τη συμβολοσειρά από τις παραμέτρους. Εάν η αίτηση είναι επιτυχής, επιστρέφουμε το περιεχόμενο ως `UmamiResult` αντικείμενο. Εάν το αίτημα αποτύχει με έναν κωδικό κατάστασης 401, καλούμε το `LoginAsync` μέθοδος και ξαναδοκιμάστε το αίτημα. Αυτό εξασφαλίζει ότι "κομψά" χειριστούμε τη συμβολική λήξη.

## Συμπέρασμα

Αυτό είναι ένα απλό παράδειγμα του πώς να δημιουργήσετε έναν πελάτη C# για το Umami API. Μπορείτε να το χρησιμοποιήσετε ως σημείο εκκίνησης για να οικοδομήσετε πιο πολύπλοκους πελάτες ή να ενσωματώσετε το API στις δικές σας εφαρμογές.