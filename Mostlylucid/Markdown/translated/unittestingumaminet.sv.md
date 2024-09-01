# Enhetstest av Umami.Net - Test av UmamiClient

# Inledning

Nu har jag [Ummami.Net-paket](https://www.nuget.org/packages/Umami.Net/) Där ute vill jag naturligtvis se till att allt fungerar som förväntat. För att göra detta är det bästa sättet att något omfattande testa alla metoder och klasser. Det är här enhetstester kommer in.
Observera: Detta är inte en "perfekt metod" typ inlägg, det är bara hur jag för närvarande har gjort det. I själva verket behöver jag inte verkligen för att Mock `IHttpMessageHandler` Här en du kan attackera en DelegatingMessageHandler till en normal HttpClient för att göra detta. Jag ville bara visa hur du kan göra det med en Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">Förbehåll IIIA-PT-38</datetime>

# Enhetstest

Enhetstester avser processen för att testa enskilda kodenheter för att säkerställa att de fungerar som förväntat. Detta görs genom att skriva tester som kallar metoderna och klasserna på ett kontrollerat sätt och sedan kontrollera utdata är som förväntat.

För ett paket som Umami.Net detta är soewhat knepigt som det båda kallar en fjärrklient över `HttpClient` och har en `IHostedService` Det används för att göra sändandet av nya händelsedata så sömlöst som möjligt.

## Pröva UmamiClient

Huvuddelen av testerna `HttpClient` baserat bibliotek undviker det faktiska "HttpClient"-samtalet. Detta görs genom att skapa en `HttpClient` som använder en `HttpMessageHandler` som ger ett känt svar. Detta görs genom att skapa en `HttpClient` med en `HttpMessageHandler` som returnerar ett känt svar; i detta fall jag bara ekor tillbaka inmatningssvaret och kontrollera som inte har manglats av `UmamiClient`.

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

Som du kommer att se detta sätter upp en `Mock<HttpMessageHandler>` Jag passerar sedan in i `UmamiClient`.
I den här koden kopplar jag in den här i vår `IServiceCollection` Uppställningsmetod. Detta lägger till alla tjänster som krävs av `UmamiClient` inklusive vår nya `HttpMessageHandler` och sedan returnerar `IServiceCollection` för användning i testerna.

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

För att använda detta och injicera det i `UmamiClient` Jag använder sedan dessa tjänster i `UmamiClient` Uppställning.

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

Du ser att jag har ett gäng alternativa parametrar här som tillåter mig att injicera olika alternativ för olika typer av tester.

### Testerna

Så nu har jag all denna installation på plats jag kan nu börja skriva tester för `UmamiClient` Metoder.

#### Skicka

Vad allt detta inställning innebär är att våra tester faktiskt kan vara ganska enkel

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

Här ser du den enklaste test fall, bara se till att `UmamiClient` kan skicka ett meddelande och få ett svar; viktigt är att vi också testar för ett undantag fall där `type` Det är fel. Detta är en ofta förbisedd del av testning, se till att koden misslyckas som förväntat.

#### Sidvy

För att testa vår pageview metod kan vi göra något liknande. I koden nedan använder jag min `EchoHttpHandler` Att bara reflektera tillbaka det skickade svaret och se till att det skickar tillbaka det jag förväntar mig.

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

Detta använder sig av `HttpContextAccessor` för att ställa in sökvägen till `/testpath` och sedan kontrollera att `UmamiClient` skickar detta på rätt sätt.

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

Detta är viktigt för vår Umami klient kod eftersom mycket av de data som skickas från varje begäran faktiskt dynamiskt genereras från `HttpContext` motsätter sig detta. Så vi kan inte skicka något alls i en `await umamiClient.TrackPageView();` ring och det kommer fortfarande att skicka rätt data genom att extrahera Url från `HttpContext`.

Som vi kommer att se senare är det också viktigt awe skicka objekt som `UserAgent` och `IPAddress` eftersom dessa används av Umami-servern för att spåra data och'spåra' användarvyer utan att använda cookies.

För att ha denna förutsägbara vi definierar ett gäng av Constants i `Consts` Klassen. Så vi kan testa mot förutsägbara svar och förfrågningar.

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

## Ytterligare tester

Detta är bara början på vår teststrategi för Umami.Net, vi måste fortfarande testa `IHostedService` och testa mot de faktiska data Umami genererar (som inte dokumenteras någonstans men innehåller en JWT token med några användbara data.)

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

Så vi kommer att vilja testa för det, simulera token och eventuellt returnera data vid varje besök (som du kommer ihåg detta är gjord av en `uuid(websiteId,ipaddress, useragent)`).

# Slutsatser

Detta är bara början på att testa Umami.Net paketet, det finns mycket mer att göra men detta är en bra början. Jag kommer att lägga till fler tester när jag går och utan tvekan förbättra dessa.