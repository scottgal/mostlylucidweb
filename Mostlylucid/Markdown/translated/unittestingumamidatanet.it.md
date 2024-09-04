# Unit Testing Umami.Net - Testing Umami Data Senza l'utilizzo di Moq

# Introduzione

Nella parte precedente di questa serie dove ho testato[ Metodi di tracciamento Umami.Net ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Il problema

Nella parte precedente ho usato Moq per darmi un `Mock<HttpMessageHandler>` e restituire il gestore utilizzato in `UmamiClient`, questo è un modello comune quando si testa il codice che utilizza `HttpClient`. In questo post vi mostrerò come testare il nuovo `UmamiDataService` senza usare Moq.

```csharp
    public static HttpMessageHandler Create()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri.ToString().Contains("api/send")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                // Read the request content
                var requestBody = request.Content != null
                    ? request.Content.ReadAsStringAsync(cancellationToken).Result
                    : null;

                // Create a response that echoes the request body
                var responseContent = requestBody != null
                    ? requestBody
                    : "No request body";


                // Return the response
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        return mockHandler.Object;
    }
```

## Perche' usare Moq?

Moq è una potente libreria di gioco che consente di creare oggetti di gioco per interfacce e classi. Viene ampiamente utilizzato nei test di unità per isolare il codice in prova dalle sue dipendenze. Tuttavia, ci sono alcuni casi in cui l'utilizzo di Moq può essere ingombrante o addirittura impossibile. Per esempio, quando il codice di prova che utilizza metodi statici o quando il codice in prova è strettamente accoppiato alle sue dipendenze.

L'esempio che ho dato sopra dà un sacco di flessibilità nel testare il `UmamiClient` classe, ma ha anche alcuni svantaggi. E' un codice brutto e fa un sacco di cose di cui non ho davvero bisogno. Quindi quando si prova `UmamiDataService` Ho deciso di provare un approccio diverso.

# Prova UmamiDataService

La `UmamiDataService` è una futura aggiunta alla libreria Umami.Net che vi permetterà di recuperare i dati da Umami per cose come vedere quante viste una pagina ha avuto, quali eventi è successo di un certo tipo, filtrata da una tonnellata di parametri paese liek, città, sistema operativo, dimensioni dello schermo, ecc. Questo è un molto potente, ma in questo momento il [API Umami funziona solo tramite JavaScript](https://umami.is/docs/api/website-stats). Così volendo giocare con quei dati ho passato attraverso lo sforzo di creare un client C# per esso.

La `UmamiDataService` classe è diviso in classi parziali multple (i metodi sono lunghi SUPER) per esempio qui è il `PageViews` metodo.

Potete vedere che MOLTO del codice sta costruendo la QueryString dalla classe passata in PageViewsRequest (ci sono altri modi per farlo, ma questo, per esempio usando Attributi o riflessioni funziona qui).

<details>
<summary>GetPageViews</summary>

```csharp
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(PageViewsRequest pageViewsRequest)
    {
        if (await authService.LoginAsync() == false)
            return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
        // Start building the query string
        var queryParams = new List<string>
        {
            $"startAt={pageViewsRequest.StartAt}",
            $"endAt={pageViewsRequest.EndAt}",
            $"unit={pageViewsRequest.Unit.ToLowerString()}"
        };

        // Add optional parameters if they are not null
        if (!string.IsNullOrEmpty(pageViewsRequest.Timezone)) queryParams.Add($"timezone={pageViewsRequest.Timezone}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Url)) queryParams.Add($"url={pageViewsRequest.Url}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Referrer)) queryParams.Add($"referrer={pageViewsRequest.Referrer}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Title)) queryParams.Add($"title={pageViewsRequest.Title}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Host)) queryParams.Add($"host={pageViewsRequest.Host}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Os)) queryParams.Add($"os={pageViewsRequest.Os}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Browser)) queryParams.Add($"browser={pageViewsRequest.Browser}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Device)) queryParams.Add($"device={pageViewsRequest.Device}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Country)) queryParams.Add($"country={pageViewsRequest.Country}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Region)) queryParams.Add($"region={pageViewsRequest.Region}");
        if (!string.IsNullOrEmpty(pageViewsRequest.City)) queryParams.Add($"city={pageViewsRequest.City}");

        // Combine the query parameters into a query string
        var queryString = string.Join("&", queryParams);

        // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews?{queryString}");

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully got page views");
            var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
            return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new PageViewsResponseModel());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.LoginAsync();
            return await GetPageViews(pageViewsRequest);
        }

        logger.LogError("Failed to get page views");
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode,
            response.ReasonPhrase ?? "Failed to get page views", null);
    }
```

</details>
Come puoi vedere questo costruisce davvero solo una stringa di query. autentica la chiamata (si veda il [ultimo articolo](/blog/unittestinglogginginaspnetcore) per alcuni dettagli su questo) e poi fa la chiamata all'API Umami. Allora, come facciamo a testarlo?

## Verifica del servizio UmamiData

In contrasto con il test UmamiClient, ho deciso di testare il `UmamiDataService` senza usare Moq. Invece, ho creato un semplice `DelegatingHandler` classe che mi permette di interrogare la richiesta poi restituire una risposta. Questo è un approccio molto più semplice rispetto all'utilizzo di Moq e mi permette di testare il `UmamiDataService` senza dover deridere il `HttpClient`.

Nel codice qui sotto potete vedere semplicemente estendere `DelegatingHandler` e scavalcare il `SendAsync` metodo. Questo metodo mi permette di ispezionare la richiesta e restituire una risposta in base alla richiesta.

```csharp
public class UmamiDataDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/api/auth/login":
                var authContent = await request.Content.ReadFromJsonAsync<AuthRequest>(cancellationToken);
                if (authContent?.username == "username" && authContent?.password == "password")
                    return ReturnAuthenticatedMessage();
                else if (authContent?.username == "bad")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
            default:

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/metrics"))
                {
                    var metricsRequest = GetParams<MetricsRequest>(request);
                    return ReturnMetrics(metricsRequest);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
 }
```

## Configurazione

Per impostare il nuovo `UmamiDataService` usare questo gestore è allo stesso modo semplice.

```csharp
    public IServiceProvider GetServiceProvider (string username="username", string password="password")
    {
        var services = new ServiceCollection();
        var mockLogger = new FakeLogger<UmamiDataService>();
        var authLogger = new FakeLogger<AuthService>();
        services.AddScoped<ILogger<UmamiDataService>>(_ => mockLogger);
        services.AddScoped<ILogger<AuthService>>(_ => authLogger);
        services.SetupUmamiData(username, password);
        return  services.BuildServiceProvider();
        
    }
```

Vedrete che ho appena organizzato il `ServiceCollection`, aggiungere il `FakeLogger<T>` (si veda di nuovo il [ultimo articolo per i dettagli su questo](/blog/unittestinglogginginaspnetcore) e poi impostare il `UmamiData` servizio con il nome utente e la password che voglio utilizzare (in modo da poter testare il guasto).

Allora chiamo per... `services.SetupUmamiData(username, password);` che è un metodo di estensione che ho creato per impostare il `UmamiDataService` con `UmamiDataDelegatingHandler` e della `AuthService`;

```csharp
    public static void SetupUmamiData(this IServiceCollection services, string username="username", string password="password")
    {
        var umamiSettings = new UmamiDataSettings()
        {
            UmamiPath = Consts.UmamiPath,
            Username = username,
            Password = password,
            WebsiteId = Consts.WebSiteId
        };
        services.AddSingleton(umamiSettings);
        services.AddHttpClient<AuthService>((provider,client) =>
        {
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            

        }).AddHttpMessageHandler<UmamiDataDelegatingHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));  //Set lifetime to five minutes

        services.AddScoped<UmamiDataDelegatingHandler>();
        services.AddScoped<UmamiDataService>();
    }
```

Potete vedere che qui è dove mi aggrappai `UmamiDataDelegatingHandler` e della `AuthService` alla `UmamiDataService`. Il modo in cui questo è strutturato è che il `AuthService` 'proprieta' `HttpClient` e della `UmamiDataService` usa il `AuthService` per effettuare le chiamate all'API Umami con il `bearer` token e `BaseAddress` E' gia' pronto.

## Le prove

Davvero questo rende effettivamente testare questo davvero semplice. E'solo un po' verboso come ho voluto testare anche la registrazione. Tutto quello che sta facendo è postare attraverso il mio `DelegatingHandler` e simulo una risposta basata sulla richiesta.

```csharp
public class UmamiData_PageViewsRequest_Test : UmamiDataBase
{
    private readonly DateTime StartDate = DateTime.ParseExact("2021-10-01", "yyyy-MM-dd", null);
    private readonly DateTime EndDate = DateTime.ParseExact("2021-10-07", "yyyy-MM-dd", null);
    
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var umamiDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var umamiDataLogger = serviceProvider.GetRequiredService<ILogger<UmamiDataService>>();
        var result = await umamiDataService.GetPageViews(StartDate, EndDate);
        var fakeAuthLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeAuthLogger.Collector; 
        IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
        Assert.Contains("Login successful", logs.Select(x => x.Message));
        
        var fakeUmamiDataLogger = (FakeLogger<UmamiDataService>)umamiDataLogger;
        FakeLogCollector umamiDataCollector = fakeUmamiDataLogger.Collector;
        IReadOnlyList<FakeLogRecord> umamiDataLogs = umamiDataCollector.GetSnapshot();
        Assert.Contains("Successfully got page views", umamiDataLogs.Select(x => x.Message));
        
        Assert.NotNull(result);
    }
}
```

### Simulazione della risposta

Per simulare la risposta per questo metodo vi ricorderete che ho questa linea nel `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Tutto questo non fa altro che estrarre informazioni dalla querystring e costruisce una risposta'realistica' (basata su Live Tests che ho compilato, ancora pochissimi documenti su questo). Vedrete che testerò per il numero di giorni tra la data di inizio e la data di fine e poi restituirò una risposta con lo stesso numero di giorni.

```csharp
    private static HttpResponseMessage ReturnPageViewsMessage(PageViewsRequest request)
    {
        var startAt = request.StartAt;
        var endAt = request.EndAt;
        var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startAt).DateTime;
        var endDate = DateTimeOffset.FromUnixTimeMilliseconds(endAt).DateTime;
        var days = (endDate - startDate).Days;

        var pageViewsList = new List<PageViewsResponseModel.Pageviews>();
        var sessionsList = new List<PageViewsResponseModel.Sessions>();
        for(int i=0; i<days; i++)
        {
            
            pageViewsList.Add(new PageViewsResponseModel.Pageviews()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*4
            });
            sessionsList.Add(new PageViewsResponseModel.Sessions()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*8
            });
        }
        var pageViewResponse = new PageViewsResponseModel()
        {
            pageviews = pageViewsList.ToArray(),
            sessions = sessionsList.ToArray()
        };
        var json = JsonSerializer.Serialize(pageViewResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
```

# In conclusione

Quindi questo è davvero semplice da testare un `HttpClient` richiesta senza utilizzare Moq e penso che sia molto più pulito in questo modo. Si perde una parte della sofisticazione resa possibile in Moq, ma per semplici test come questo, penso che sia un buon compromesso.