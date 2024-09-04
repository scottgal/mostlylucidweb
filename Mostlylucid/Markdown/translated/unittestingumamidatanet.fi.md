# Unit Testaa Umami.Net - Testaa Umami Dataa ilman Moqia

# Johdanto

Tämän sarjan edellisessä osassa, jossa testasin[ Umami.Net tracking methods ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024–09–04T20–30</datetime>
[TOC]

## Ongelma

Edellisessä osassa käytin Moqia antaakseni minulle `Mock<HttpMessageHandler>` ja palauta käsittelijä, jota käytetään `UmamiClient`, Tämä on yleinen kaava, kun testataan koodia, joka käyttää `HttpClient`...................................................................................................................................... Tässä viestissä näytän sinulle, miten testata uutta `UmamiDataService` Moqia käyttämättä.

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

## Miksi käyttää Moqia?

Moq on tehokas pilkkaava kirjasto, jonka avulla voit luoda valeobjekteja rajapintoja ja luokkia varten. Sitä käytetään laajalti yksikkötestauksessa, jossa testattava koodi eristetään sen riippuvuuksista. On kuitenkin tapauksia, joissa Moqin käyttö voi olla hankalaa tai jopa mahdotonta. Esimerkiksi staattisia menetelmiä käyttävän koodin testauksessa tai kun testattava koodi kytketään tiukasti sen riippuvuuteen.

Edellä antamani esimerkki antaa paljon joustavuutta testauksessa. `UmamiClient` Luokkaa, mutta siinä on myös huonoja puolia. Se on UGLY-koodi ja tekee paljon sellaista, mitä en oikeastaan tarvitse. Joten testattaessa `UmamiDataService` Päätin kokeilla eri lähestymistapaa.

# UmamiDataServicen testaus

Erytropoietiini `UmamiDataService` on tuleva lisä Umami.Net-kirjastoon, jonka avulla voit noutaa tietoja Umami-kirjastosta esimerkiksi katsomalla, kuinka monta näkymää sivulla oli, mitä tietyn tyyppisiä tapahtumia tapahtui, joita suodattivat tonneittain muuttujat liek country, city, OS, screen size, jne. Tämä on hyvin voimakas, mutta juuri nyt [Umami API toimii vain JavaScriptin kautta](https://umami.is/docs/api/website-stats) Joten haluan pelata sillä datalla, jonka tein luodakseni sille C#-asiakkaan.

Erytropoietiini `UmamiDataService` Luokka jakaantuu kullattuihin osittaisiin luokkiin (menetelmät ovat SUPER long) esimerkiksi tässä `PageViews` menetelmä.

Huomaat, että suuri osa koodista rakentaa QueryStringiä PageViewsRequest -kurssin läpimenosta (tähän on muitakin tapoja, mutta tämä, esimerkiksi attribuuttien tai heijastusten käyttö, toimii täällä).

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
Kuten näette, tämä todella rakentaa kyselyjonon. Vahvistaa puhelun (ks. [viimeinen artikkeli](/blog/unittestinglogginginaspnetcore) Lisätietoja tästä) ja sitten soittaa Umamin API-puhelimeen. Miten testaamme tätä?

## Umamidatapalvelun testaaminen

Toisin kuin UmamiClient, päätin testata `UmamiDataService` Moqia käyttämättä. Sen sijaan loin yksinkertaisen `DelegatingHandler` Luokka, jonka avulla voin kuulustella pyyntöä ja sitten vastata. Tämä on paljon yksinkertaisempi lähestymistapa kuin Moqin käyttö, ja sen avulla voin testata `UmamiDataService` ilman, että on pakko pilkata `HttpClient`.

Alla olevassa koodissa näet, että yksinkertaisesti laajennan `DelegatingHandler` ja ohita `SendAsync` menetelmä. Tällä menetelmällä voin tarkastaa pyynnön ja palauttaa pyynnön mukaisen vastauksen.

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

## Asetukset

Perustetaan uusi `UmamiDataService` Tämän käsittelijän käyttö on yhtä yksinkertaista.

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

Huomaat, että järjestin juuri `ServiceCollection`, lisätään `FakeLogger<T>` (ks. [viimeinen artikkeli tarkempia tietoja tästä](/blog/unittestinglogginginaspnetcore) ja sen jälkeen perustaa `UmamiData` Palvelu käyttäjätunnuksella ja salasanalla, jota haluan käyttää (jotta voin testata epäonnistumista).

Kutsun sitten `services.SetupUmamiData(username, password);` joka on laajennusmenetelmä, jonka loin perustaakseni `UmamiDataService`  `UmamiDataDelegatingHandler` ja `AuthService`;

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

Huomaat, että tässä kohtaa koukutan `UmamiDataDelegatingHandler` ja `AuthService` Euroopan unionin toiminnasta tehtyyn sopimukseen ja Euroopan unionin toiminnasta tehtyyn sopimukseen liitetyssä pöytäkirjassa N:o 2 olevan 1 ja 2 kohdan mukaisesti. `UmamiDataService`. Tämä on rakenteeltaan sellainen, että `AuthService` "Omistaa" `HttpClient` ja `UmamiDataService` käyttää `AuthService` soittaa puhelut Umami API kanssa `bearer` kuponki ja `BaseAddress` Se on jo valmiina.

## Testit

Tämä tekee tämän testaamisesta todella yksinkertaista. Se on vain hieman sanavalmis, koska halusin myös testata puunkorjuuta. Se ei tee muuta kuin lähettää minun kauttani. `DelegatingHandler` ja simuloin vastausta pyynnön perusteella.

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

### Vastauksen simulointi

Simuloidakseni vastausta tähän menetelmään muistatte, että minulla on tämä rivi... `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Kaikki tämä vain vetää tietoa kyselystä ja muodostaa "realistisen" vastauksen (olen koonnut Live Tests, jälleen hyvin vähän dokumentteja tästä). Näet, kuinka monta päivää testaan aloitus- ja lopetuspäivän välillä ja sitten palautan vastauksen, jossa on sama määrä päiviä.

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

# Johtopäätöksenä

Joten se on se, että se on aika helppo testata `HttpClient` Pyyntö ilman Moqin käyttöä, ja mielestäni se on paljon puhtaampi näin. Menetät osan Moqissa mahdollistamasta hienostuneisuudesta, mutta tällaisissa yksinkertaisissa testeissä se on mielestäni hyvä vaihtokauppa.