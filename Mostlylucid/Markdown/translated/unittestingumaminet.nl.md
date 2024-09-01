# Unit Testing Umami.Net - Testing UmamiClient

# Inleiding

Nu heb ik de [Umami.Net pakket](https://www.nuget.org/packages/Umami.Net/) Ik wil er natuurlijk voor zorgen dat het allemaal werkt zoals verwacht. Om dit te doen de beste manier is om enigszins uitgebreid testen van alle methoden en klassen. Dit is waar het testen van units binnenkomt.
Opmerking: Dit is niet een 'perfecte aanpak' type post, het is gewoon hoe ik het op dit moment heb gedaan. In werkelijkheid heb ik niet echt nodig om de `IHttpMessageHandler` Hier kun je een Verwijderende MessageHandler aanvallen op een normale HttpClient om dit te doen. Ik wilde gewoon laten zien hoe je het kunt doen met een Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Eenheidstest

Eenheidstests hebben betrekking op het proces van het testen van individuele eenheden code om ervoor te zorgen dat ze werken zoals verwacht. Dit wordt gedaan door het schrijven van tests die de methoden en klassen op een gecontroleerde manier te noemen en vervolgens het controleren van de output is zoals verwacht.

Voor een pakket als Umami.Net dit is soewhat lastig als het beide noemt een remote client over `HttpClient` en heeft een `IHostedService` het gebruikt om het verzenden van nieuwe gebeurtenisgegevens zo naadloos mogelijk te maken.

## UmamiClient testen

Het grootste deel van het testen van een `HttpClient` gebaseerd bibliotheek is het vermijden van de werkelijke 'HttpClient' call. Dit wordt gedaan door het creëren van een `HttpClient` die gebruik maakt van een `HttpMessageHandler` Dat geeft een bekende reactie terug. Dit wordt gedaan door het creëren van een `HttpClient` met een `HttpMessageHandler` Dat geeft een bekende reactie terug; in dit geval echo gewoon terug de input reactie en controleer dat is niet verminkt door de `UmamiClient`.

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

Zoals je zult zien zet dit een `Mock<HttpMessageHandler>` Ik passeer dan in de `UmamiClient`.
In deze code haak ik dit in onze `IServiceCollection` set-up methode. Dit voegt alle diensten toe die nodig zijn voor de `UmamiClient` met inbegrip van onze nieuwe `HttpMessageHandler` en keert dan de `IServiceCollection` voor gebruik bij de tests.

```csharp
    public static IServiceCollection SetupServiceCollection(string webSiteId = Consts.WebSiteId,
        string umamiPath = Consts.UmamiPath, HttpMessageHandler? handler = null)
    {
        var services = new ServiceCollection();
        var umamiClientSettings = new UmamiClientSettings
        {
            WebsiteId = webSiteId,
            UmamiPath = umamiPath
        };
        services.AddSingleton(umamiClientSettings);
        services.AddScoped<PayloadService>();
        services.AddLogging(x => x.AddConsole());
        // Mocking HttpMessageHandler with Moq
        var mockHandler = handler ?? EchoMockHandler.Create();
        services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
        {
            var umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).ConfigurePrimaryHttpMessageHandler(() => mockHandler);
        return services;
    }
```

Om dit te gebruiken en te injecteren in de `UmamiClient` Ik maak dan gebruik van deze diensten in de `UmamiClient` Installeren.

```csharp
    public static UmamiClient GetUmamiClient(IServiceCollection? serviceCollection = null,
        HttpContextAccessor? contextAccessor = null)
    {
        serviceCollection ??= SetupServiceCollection();
        SetupUmamiClient(serviceCollection, contextAccessor);
        if (serviceCollection == null) throw new NullReferenceException(nameof(serviceCollection));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<UmamiClient>();
    }
```

Je zult zien dat ik hier een aantal alternatieve optionele parameters heb waarmee ik verschillende opties voor verschillende testtypes kan inspuiten.

### De tests

Dus nu heb ik al deze setup op zijn plaats Ik kan nu beginnen met het schrijven van tests voor de `UmamiClient` methoden.

#### Verzenden

Wat al deze opstelling betekent is dat onze tests eigenlijk vrij eenvoudig kunnen zijn

```csharp
public class UmamiClient_SendTests
{
    [Fact]
    public async Task Send_Wrong_Type()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        await Assert.ThrowsAsync<ArgumentException>(async () => await umamiClient.Send(type: "boop"));
    }

    [Fact]
    public async Task Send_Empty_Success()
    {
        var umamiClient = SetupExtensions.GetUmamiClient();
        var response = await umamiClient.Send();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

Hier zie je de eenvoudigste test geval, alleen ervoor te zorgen dat de `UmamiClient` kan een bericht sturen en een reactie krijgen; belangrijk is dat we ook testen op een uitzondering geval waar de `type` Het is verkeerd. Dit is een vaak over het hoofd gezien deel van het testen, ervoor te zorgen dat de code faalt zoals verwacht.

#### Paginaweergave

Om onze pageview methode te testen kunnen we iets dergelijks doen. In de onderstaande code gebruik ik mijn `EchoHttpHandler` om gewoon terug te denken aan de verzonden reactie en ervoor te zorgen dat het terug stuurt wat ik verwacht.

```csharp
    [Fact]
    public async Task TrackPageView_WithNoUrl()
    {
        var defaultUrl = "/testpath";
        var contextAccessor = SetupExtensions.SetupHttpContextAccessor(path: "/testpath");
        var umamiClient = SetupExtensions.GetUmamiClient(contextAccessor: contextAccessor);
        var response = await umamiClient.TrackPageView();

        var content = await response.Content.ReadFromJsonAsync<EchoedRequest>();
        Assert.NotNull(response);
        Assert.NotNull(content);
        Assert.Equal(content.Payload.Url, defaultUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
```

### HttpContextAccessor

Dit maakt gebruik van de `HttpContextAccessor` om het pad in te stellen `/testpath` en dan controleert dat de `UmamiClient` Stuurt dit correct.

```csharp
    public static HttpContextAccessor SetupHttpContextAccessor(string host = Consts.Host,
        string path = Consts.Path, string ip = Consts.Ip, string userAgent = Consts.UserAgent,
        string referer = Consts.Referer)
    {
        HttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = new PathString(path);
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.Request.Headers.UserAgent = userAgent;
        httpContext.Request.Headers.Referer = referer;

        var context = new HttpContextAccessor { HttpContext = httpContext };
        return context;
    }

```

Dit is belangrijk voor onze Umami client code, aangezien veel van de gegevens verzonden van elke aanvraag daadwerkelijk dynamisch gegenereerd uit de `HttpContext` object. Zodat we helemaal niets kunnen sturen in een `await umamiClient.TrackPageView();` bel en het zal nog steeds de juiste gegevens door het extraheren van de Url uit de `HttpContext`.

Zoals we later zullen zien is het ook belangrijk dat de ontzag verzenden items zoals de `UserAgent` en `IPAddress` omdat deze door de Umami-server worden gebruikt om de gegevens te volgen en gebruikersweergaven te 'tracken' zonder cookies te gebruiken.

Om dit voorspelbaar te hebben definiëren we een aantal Consts in de `Consts` Klas. Zodat we kunnen testen tegen voorspelbare antwoorden en verzoeken.

```csharp
public class Consts
{
    public const string UmamiPath = "https://example.com";
    public const string WebSiteId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
    public const string Host = "example.com";
    public const string Path = "/example";
    public const string Ip = "127.0.0.1";
    public const string UserAgent = "Test User Agent";
    public const string Referer = "Test Referer";
    public const string DefaultUrl = "/testpath";
    public const string DefaultTitle = "Example Page";
    public const string DefaultName = "RSS";
    public const string DefaultType = "event";

    public const string Email = "test@test.com";

    public const string UserId = "11224456";
    
    public const string UserName = "Test User";
    
    public const string SessionId = "B41A9964-FD33-4108-B6EC-9A6B68150763";
}
```

## Verdere tests

Dit is nog maar het begin van onze teststrategie voor Umami.Net, we moeten nog steeds testen de `IHostedService` en testen op de actuele gegevens die Umami genereert (die nergens gedocumenteerd is maar een JWT token bevat met enkele nuttige gegevens.)

```json
{
  "alg": "HS256",
  "typ": "JWT"
}{
  "id": "b9836672-feee-55c5-985a-a5a23d4a23ad",
  "websiteId": "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
  "hostname": "example.com",
  "browser": "chrome",
  "os": "Windows 10",
  "device": "desktop",
  "screen": "1920x1080",
  "language": "en-US",
  "country": "GB",
  "subdivision1": null,
  "subdivision2": null,
  "city": null,
  "createdAt": "2024-09-01T09:26:14.418Z",
  "visitId": "e7a6542f-671a-5573-ab32-45244474da47",
  "iat": 1725182817
}2|Y*: �(N%-ޘ^1>@V
```

Dus we zullen willen testen op dat, simuleren van de token en eventueel terug te keren van de gegevens op elk bezoek (zoals je zult herinneren is dit gemaakt van een `uuid(websiteId,ipaddress, useragent)`).

# Conclusie

Dit is nog maar het begin van het testen van het Umami.Net pakket, er is nog veel meer te doen maar dit is een goede start. Ik voeg er nog meer tests aan toe... en zonder twijfel deze te verbeteren.