# Enhetstestning Umami.Net - Test av Umami data utan att använda Moq

# Inledning

I den föregående delen av denna serie där jag testade[ Ummami.Net tracking methods ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Problemet

I förra delen använde jag Moq för att ge mig en `Mock<HttpMessageHandler>` och returnera den hanterare som används i `UmamiClient`, Detta är ett vanligt mönster vid testning kod som använder `HttpClient`....................................... I det här inlägget ska jag visa dig hur man testar det nya `UmamiDataService` utan att använda Moq.

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

## Varför använda Moq?

Moq är ett kraftfullt hånfulla bibliotek som låter dig skapa mock objekt för gränssnitt och klasser. Det används i stor utsträckning i enhetstest för att isolera koden under test från dess beroenden. Det finns dock vissa fall där användningen av Moq kan vara tungrodd eller till och med omöjlig. Till exempel när man testar kod som använder statiska metoder eller när koden under provning är tätt kopplad till dess beroenden.

Exemplet jag gav ovan ger en hel del flexibilitet i att testa `UmamiClient` klass, men det har också vissa nackdelar. Det är UGLY kod och gör en massa saker jag inte behöver. Så när du testar `UmamiDataService` Jag bestämde mig för att försöka med ett annat tillvägagångssätt.

# Testa UmamiDataService

I detta sammanhang är det viktigt att se till att `UmamiDataService` är ett framtida tillägg till Umami.Net-biblioteket som gör att du kan hämta data från Umami för saker som att se hur många vyer en sida hade, vad händelser hände av en viss typ, filtreras av en ton av parametrar liek land, stad, OS, skärmstorlek, etc. Detta är en mycket kraftfull men just nu [Umami API fungerar endast via JavaScript](https://umami.is/docs/api/website-stats)....................................... Så att vilja leka med den data jag gick igenom ansträngningen att skapa en C# klient för det.

I detta sammanhang är det viktigt att se till att `UmamiDataService` klass delas upp i multple partiella klasser (metoderna är SUPER lång) till exempel här är `PageViews` Metod.

Du kan se att MYCKET av koden är att bygga QueryString från passerad i PageViewsRequest klass (det finns andra sätt att göra detta men detta, till exempel att använda Attribut eller reflektion fungerar här).

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
Som ni kan se så konstruerar detta egentligen bara en frågesträng. autentiserar samtalet (se [förra artikeln](/blog/unittestinglogginginaspnetcore) för några detaljer om detta) och sedan ringer till Umami API. Hur testar vi det här?

## Testa UmamiDataService

I motsats till att testa UmamiClient, bestämde jag mig för att testa `UmamiDataService` utan att använda Moq. Istället skapade jag en enkel `DelegatingHandler` klass som tillåter mig att förhöra begäran och sedan returnera ett svar. Detta är en mycket enklare strategi än att använda Moq och tillåter mig att testa `UmamiDataService` utan att behöva håna `HttpClient`.

I koden nedan kan du se att jag helt enkelt förlänga `DelegatingHandler` och åsidosätter `SendAsync` Metod. Denna metod gör det möjligt för mig att kontrollera begäran och returnera ett svar baserat på begäran.

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

## Ställ in

Sätta upp det nya `UmamiDataService` att använda denna handler är på liknande sätt enkel.

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

Du ska få se att jag precis har satt upp `ServiceCollection`, lägga till `FakeLogger<T>` (Återigen se [förra artikeln för detaljer om detta](/blog/unittestinglogginginaspnetcore) och sedan sätta upp `UmamiData` tjänst med användarnamn och lösenord jag vill använda (så att jag kan testa fel).

Jag kallar sedan in `services.SetupUmamiData(username, password);` vilket är en förlängningsmetod jag skapade för att sätta upp `UmamiDataService` med `UmamiDataDelegatingHandler` och `AuthService`;

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

Du kan se att det är här jag krokar in `UmamiDataDelegatingHandler` och `AuthService` till `UmamiDataService`....................................... Det sätt på vilket detta är strukturerat är att `AuthService` "äger" `HttpClient` och `UmamiDataService` använder `AuthService` att göra samtalen till Umami API med `bearer` pollett och `BaseAddress` Jag är redan klar.

## Testerna

Det här gör det faktiskt enkelt att testa. Det är bara lite ordagrant eftersom jag också ville testa loggningen också. Allt den gör är att posta genom min `DelegatingHandler` och jag simulerar ett svar baserat på begäran.

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

### Simulera svaret

För att simulera svaret på denna metod kommer du ihåg Jag har denna linje i `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Allt detta gör är att dra info från frågesträngen och konstruerar en "realistisk" respons (baserat på Live Tests Jag har sammanställt, återigen mycket lite dokument på detta). Du ser att jag testar för antalet dagar mellan start och slutdatum och sedan returnerar ett svar med samma antal dagar.

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

# Slutsatser

Så det är faktiskt ganska enkelt att testa en `HttpClient` begäran utan att använda Moq och jag tycker att det är mycket renare på det här sättet. Du förlorar en del av den sofistikering som gjorts möjlig i Moq men för enkla tester som detta, jag tycker att det är en bra kompromiss.