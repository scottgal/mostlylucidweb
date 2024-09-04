# Unit Testing Umami.Net - Umami-gegevens testen zonder Moq te gebruiken

# Inleiding

In het vorige deel van deze serie waar ik getest[ Umami.Net tracking methoden ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Het probleem

In het vorige deel gebruikte ik Moq om me een `Mock<HttpMessageHandler>` en terug te brengen de handler gebruikt in `UmamiClient`, dit is een gemeenschappelijk patroon bij het testen van code die gebruikt `HttpClient`. In dit bericht zal ik u laten zien hoe om de nieuwe te testen `UmamiDataService` zonder Moq te gebruiken.

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

## Waarom Moq gebruiken?

Moq is een krachtige spotbibliotheek waarmee je spotobjecten kunt maken voor interfaces en klassen. Het wordt op grote schaal gebruikt in het testen van eenheden om de te testen code te isoleren van de afhankelijkheden ervan. Er zijn echter enkele gevallen waarin het gebruik van Moq omslachtig of zelfs onmogelijk kan zijn. Bijvoorbeeld bij het testen van code die statische methoden gebruikt of wanneer de te testen code strak gekoppeld is aan de afhankelijkheden ervan.

Het voorbeeld dat ik hierboven gaf geeft veel flexibiliteit bij het testen van de `UmamiClient` klasse, maar het heeft ook een aantal nadelen. Het is lelijk en doet veel dingen die ik niet echt nodig heb. Dus bij het testen `UmamiDataService` Ik besloot om een andere aanpak te proberen.

# UmamiDataService testen

De `UmamiDataService` is een toekomstige toevoeging aan de Umami.Net bibliotheek die u zal toestaan om gegevens van Umami op te halen voor dingen zoals het zien van hoeveel views een pagina had, wat gebeurtenissen gebeurde van een bepaald type, gefilterd door een ton van parameters liek land, stad, OS, schermgrootte, enz. Dit is een zeer krachtige, maar op dit moment de [Umami API werkt alleen via JavaScript](https://umami.is/docs/api/website-stats). Dus wilde ik met die data spelen... ik heb geprobeerd er een C# client voor te maken.

De `UmamiDataService` klasse is verdeeld in meerdere partiële klassen (de methoden zijn SUPER lang) bijvoorbeeld hier is de `PageViews` methode.

Je kunt zien dat MEEL van de code de QueryString bouwt vanuit de doorgegeven in PageViewsRequest class (er zijn andere manieren om dit te doen, maar dit, bijvoorbeeld met behulp van attributen of reflectie werkt hier).

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
Zoals je kunt zien bouwt dit eigenlijk gewoon een query string. Authenticeert de oproep (zie de [laatste artikel](/blog/unittestinglogginginaspnetcore) voor wat details hierover) en belt vervolgens naar de Umami API. Hoe testen we dit?

## Testen van de UmamiDataService

In tegenstelling tot het testen van UmamiClient, besloot ik om de `UmamiDataService` zonder Moq te gebruiken. In plaats daarvan creëerde ik een eenvoudige `DelegatingHandler` klas die me in staat stelt om het verzoek te ondervragen en dan een antwoord terug te geven. Dit is een veel eenvoudiger aanpak dan het gebruik van Moq en stelt me in staat om de `UmamiDataService` zonder de spot te hoeven drijven met de `HttpClient`.

In de onderstaande code kun je zien dat ik gewoon uitbreid `DelegatingHandler` en overschrijft de `SendAsync` methode. Deze methode stelt me in staat om het verzoek te inspecteren en een antwoord terug te geven op basis van het verzoek.

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

## Instellen

Het opzetten van de nieuwe `UmamiDataService` om deze handler te gebruiken is even eenvoudig.

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

Je zult zien dat ik net het opzetten van de `ServiceCollection`, voeg de `FakeLogger<T>` (zie opnieuw de [laatste artikel voor details hierover](/blog/unittestinglogginginaspnetcore) en dan het opzetten van de `UmamiData` service met de gebruikersnaam en wachtwoord die ik wil gebruiken (zodat ik een storing kan testen).

Ik roep u dan op... `services.SetupUmamiData(username, password);` Dat is een extensie methode die ik heb gemaakt om de `UmamiDataService` met de `UmamiDataDelegatingHandler` en de `AuthService`;

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

Je kunt zien dat dit is waar ik haak in de `UmamiDataDelegatingHandler` en de `AuthService` aan de `UmamiDataService`. De manier waarop dit is gestructureerd is dat de `AuthService` "eigen" de `HttpClient` en de `UmamiDataService` maakt gebruik van de `AuthService` om te bellen naar de Umami API met de `bearer` token en `BaseAddress` Ik ben er al klaar voor.

## De tests

Echt dit maakt eigenlijk het testen van dit echt heel eenvoudig. Het is gewoon een beetje verboos omdat ik ook de houtkap wilde testen. Alles wat het doet is posten via mijn `DelegatingHandler` en ik simuleer een reactie op basis van het verzoek.

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

### Simulatie van de respons

Om de reactie voor deze methode te simuleren, herinner je je dat ik deze lijn in de `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Het enige wat dit doet is info halen uit de querystring en een'realistische' respons construeren (op basis van Live Tests heb ik samengesteld, opnieuw heel weinig docs op dit). Je zult zien dat ik test voor het aantal dagen tussen de begin- en einddatum en dan een antwoord terug te geven met hetzelfde aantal dagen.

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

# Conclusie

Dus dat is het echt het is vrij eenvoudig om te testen een `HttpClient` verzoek zonder Moq te gebruiken en ik denk dat het op deze manier veel schoner is. Je verliest wel wat van de verfijning die mogelijk is gemaakt in Moq... maar voor eenvoudige tests als deze, denk ik dat het een goede ruil is.