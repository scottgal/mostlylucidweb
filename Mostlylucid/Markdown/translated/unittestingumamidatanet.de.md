# Unit Testing Umami.Net - Testen von Umami-Daten ohne Moq

# Einleitung

Im vorherigen Teil dieser Serie, wo ich getestet[ Umami.Net Tracking Methoden ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Das Problem

Im vorherigen Teil habe ich Moq benutzt, um mir eine `Mock<HttpMessageHandler>` und den benutzten Handler zurückgeben `UmamiClient`, ist dies ein häufiges Muster beim Testen von Code, der verwendet `HttpClient`......................................................................................................... In diesem Beitrag werde ich Ihnen zeigen, wie man die neue testen `UmamiDataService` ohne Verwendung von Moq.

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

## Warum Moq benutzen?

Moq ist eine leistungsstarke Spoting-Bibliothek, mit der Sie Spot-Objekte für Interfaces und Klassen erstellen können. Es ist weit verbreitet in Unit-Tests verwendet, um den Code unter Test von seinen Abhängigkeiten zu isolieren. Allerdings gibt es einige Fälle, in denen die Verwendung von Moq schwerfällig oder sogar unmöglich sein kann. Zum Beispiel beim Testen von Code, der statische Methoden verwendet oder wenn der zu testende Code fest an seine Abhängigkeiten gekoppelt ist.

Das Beispiel, das ich oben gab, gibt eine Menge Flexibilität bei der Prüfung der `UmamiClient` Klasse, aber es hat auch einige Nachteile. Es ist UGLY Code und macht eine Menge Dinge, die ich nicht wirklich brauche. Also bei der Prüfung `UmamiDataService` Ich beschloss, einen anderen Ansatz zu versuchen.

# Testen von UmamiDataService

Das `UmamiDataService` ist eine zukünftige Ergänzung der Umami.Net-Bibliothek, die es Ihnen ermöglicht, Daten von Umami für Dinge wie sehen, wie viele Ansichten eine Seite hatte, welche Ereignisse von einem bestimmten Typ, gefiltert durch eine Tonne Parameter Liek Land, Stadt, OS, Bildschirmgröße, etc. Dies ist eine sehr mächtige, aber im Moment die [Umami API funktioniert nur über JavaScript](https://umami.is/docs/api/website-stats)......................................................................................................... Also mit diesen Daten spielen zu wollen, machte ich die Mühe, einen C#-Client dafür zu erstellen.

Das `UmamiDataService` class ist in multiple Teilklassen (die Methoden sind SUPER lang) unterteilt, zum Beispiel ist hier die `PageViews` verfahren.

Sie können sehen, dass MUCH des Codes die QueryString aus der übergebenen in PageViewsRequest-Klasse erstellt (es gibt andere Möglichkeiten, dies zu tun, aber dies, zum Beispiel mit Attributen oder Reflexion funktioniert hier).

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
Wie Sie sehen können, konstruiert dies wirklich nur einen Query String. authentifiziert den Anruf (siehe [letzter Artikel](/blog/unittestinglogginginaspnetcore) für einige Details dazu) und macht dann den Anruf an die Umami API. Wie testen wir das?

## Testen des UmamiDataService

Im Gegensatz zum Testen von UmamiClient entschied ich mich, die `UmamiDataService` ohne Verwendung von Moq. Stattdessen habe ich eine einfache `DelegatingHandler` Klasse, die es mir erlaubt, die Anfrage zu verhören und dann eine Antwort zurückzugeben. Dies ist ein viel einfacherer Ansatz als die Verwendung von Moq und ermöglicht es mir, die `UmamiDataService` ohne zu verspotten die `HttpClient`.

Im Code unten sehen Sie, dass ich einfach expandiere `DelegatingHandler` und überschreiben die `SendAsync` verfahren. Diese Methode ermöglicht es mir, die Anfrage zu prüfen und eine Antwort basierend auf der Anfrage zurückzugeben.

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

## Einrichtung

Um die neue `UmamiDataService` Dieser Handler ist ähnlich einfach zu verwenden.

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

Du wirst sehen, dass ich gerade die `ServiceCollection`, fügen Sie die `FakeLogger<T>` (siehe auch die [letzter Artikel für Details zu diesem](/blog/unittestinglogginginaspnetcore) und dann die Einrichtung der `UmamiData` Service mit dem Benutzernamen und Passwort, das ich verwenden möchte (damit ich Fehler testen kann).

Ich rufe dann in `services.SetupUmamiData(username, password);` die eine Erweiterungsmethode ist, die ich erstellt habe, um die `UmamiDataService` mit der `UmamiDataDelegatingHandler` und der `AuthService`;

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

Sie können sehen, dass dies ist, wo ich haken in der `UmamiDataDelegatingHandler` und der `AuthService` zu dem `UmamiDataService`......................................................................................................... Die Art und Weise, wie dies strukturiert ist, ist, dass die `AuthService` "Eigene" `HttpClient` und der `UmamiDataService` verwendet die `AuthService` um die Anrufe zur Umami API mit dem `bearer` Zeichen und `BaseAddress` Schon fertig.

## Die Prüfungen

Das macht es wirklich einfach, das zu testen. Es ist nur ein bisschen verbal, da ich auch die Protokollierung testen wollte. Alles, was es tut, ist, durch meine `DelegatingHandler` und ich simulieren eine Antwort basierend auf der Anfrage.

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

### Simulation der Reaktion

Um die Reaktion für diese Methode zu simulieren, werden Sie sich erinnern, dass ich diese Zeile in der `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Alles, was dies tut, ist, Informationen aus dem Querystring zu ziehen und eine'realistische' Antwort zu konstruieren (basierend auf Live Tests, die ich zusammengestellt habe, wieder sehr wenig Docs dazu). Sie werden sehen, ich teste für die Anzahl der Tage zwischen dem Start-und Enddatum und dann eine Antwort mit der gleichen Anzahl von Tagen zurück.

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

# Schlussfolgerung

Also das ist es wirklich, es ist ziemlich einfach, ein zu testen `HttpClient` Anfrage ohne Moq und ich denke, es ist weit sauberer auf diese Weise. Sie verlieren einige der Raffinesse möglich gemacht in Moq, aber für einfache Tests wie diese, Ich denke, es ist ein guter Kompromiss.