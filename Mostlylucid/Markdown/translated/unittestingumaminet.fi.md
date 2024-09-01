# Yksikkötestaus Umami.Net - Testaus UmamiClient

# Johdanto

Nyt minulla on [Umami.Net-paketti](https://www.nuget.org/packages/Umami.Net/) Ulkona haluan tietysti varmistaa, että kaikki toimii odotetusti. Tämä onnistuu parhaiten testaamalla hieman kattavasti kaikkia menetelmiä ja luokkia. Yksikön testaus on tässä mukana.
Huomautus: Tämä ei ole "täydellinen lähestymistapa" -tyyppinen viesti, vaan se, miten olen sen tällä hetkellä tehnyt. Todellisuudessa minun ei todellakaan tarvitse mollata `IHttpMessageHandler` Täällä voit hyökätä DelegatingMessageHandlerin kimppuun normaaliin HttpClientiin tekemään näin. Halusin vain näyttää, miten se onnistuu Mockilla.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024–09–01T17:22</datetime>

# Yksikkötestaus

Yksikkötestauksella tarkoitetaan yksittäisten koodiyksiköiden testausprosessia sen varmistamiseksi, että ne toimivat odotetusti. Tämä tehdään kirjoittamalla testejä, joissa kutsutaan menetelmiä ja luokkia hallitusti ja sitten tarkistetaan, että ulostulo on odotetun mukainen.

Umamin kaltaiselle paketille.Net tämä on niin hankalaa, koska molemmat kutsuvat etäasiakasta `HttpClient` ja hänellä on `IHostedService` sillä tehdään uusien tapahtumatietojen lähettämisestä mahdollisimman saumatonta.

## UmamiClientin testaus

Suurin osa testauksesta `HttpClient` peruskirjasto välttelee varsinaista HttpClient-puhelua. Tämä tapahtuu luomalla `HttpClient` jossa käytetään `HttpMessageHandler` se palauttaa tunnetun vastauksen. Tämä tapahtuu luomalla `HttpClient` a:n kanssa `HttpMessageHandler` Tämä palauttaa tunnetun vastauksen. Tässä tapauksessa vain toistan vastauksen ja tarkistan, että vastaus ei ole raadeltu. `UmamiClient`.

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

Kuten näette, tämä on `Mock<HttpMessageHandler>` Sitten siirryn `UmamiClient`.
Tässä koodissa kytken tämän meidän `IServiceCollection` Setup method. Tämä lisää kaikki palvelut, joita `UmamiClient` Myös meidän uusi `HttpMessageHandler` ja sitten palauttaa `IServiceCollection` käytettäväksi testeissä.

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

Käytä tätä ja ruiskuta se ihon alle. `UmamiClient` Sen jälkeen käytän näitä palveluita `UmamiClient` Lavastus.

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

Huomaat, että minulla on tässä valikoima vaihtoehtoisia valinnaisia parametreja, joiden avulla voin injektoida erilaisia vaihtoehtoja eri testityypeille.

### Testit

Joten nyt minulla on kaikki tämä asetelma, voin nyt alkaa kirjoittaa testejä `UmamiClient` metodit.

#### Lähetä

Kaikki tämä tarkoittaa sitä, että kokeet voivat olla aika yksinkertaisia.

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

Tässä näet yksinkertaisimman testitapauksen, vain varmistaen, että `UmamiClient` Voimme lähettää viestin ja saada vastauksen. Tärkeää on myös se, että testaamme poikkeustapausta, jossa `type` on väärin. Tämä on usein sivuutettu osa testausta, jolla varmistetaan, että koodi epäonnistuu odotetusti.

#### Sivunäkymä

Testataksemme sivukatselumenetelmäämme voimme tehdä jotain samankaltaista. Alla olevassa koodissa käytän `EchoHttpHandler` Reflektoimaan lähetettyä vastausta ja varmistamaan, että se lähettää takaisin sen, mitä odotan.

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

### HttpContextAccessori

Tässä käytetään `HttpContextAccessor` Aseta polku `/testpath` ja sen jälkeen tarkistaa, että `UmamiClient` Lähetä tämä oikein.

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

Tämä on tärkeää Umami-asiakaskoodillemme, koska suuri osa jokaisesta pyynnöstä lähetetyistä tiedoista on itse asiassa dynaamisesti tuotettu `HttpContext` Esine. Jotta emme voi lähettää mitään. `await umamiClient.TrackPageView();` Soita ja se lähettää edelleen oikeat tiedot poimimalla Url `HttpContext`.

Kuten näemme myöhemmin, on myös tärkeää, että kunnioitus lähettää kohteita kuten `UserAgent` sekä `IPAddress` Koska niitä käyttää Umami-palvelin tietojen seuraamiseen ja "jäljitä" käyttäjien näkökulmia ilman evästeiden käyttöä.

Jotta tämä olisi ennustettavissa, määrittelemme joukko Consts in `Consts` Luokka. Voimme siis testata ennakoitavia vastauksia ja pyyntöjä.

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

## Lisätestit

Tämä on vasta alkua testistrategiallemme Umamille.Net, meidän täytyy vielä testata `IHostedService` ja testaa Umamin tuottamaa dataa (jota ei ole dokumentoitu missään, mutta joka sisältää JWT-tokentin, jossa on joitakin hyödyllisiä tietoja).

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

Joten haluamme testata sitä varten, simuloida kuponkia ja mahdollisesti palauttaa jokaisen vierailun tiedot (kuten muistat, tämä on tehty `uuid(websiteId,ipaddress, useragent)`).

# Johtopäätöksenä

Tämä on vasta alkua Umamin testaamiselle.Net-pakettia on vielä paljon tehtävää, mutta tämä on hyvä alku. Lisään lisää testejä mennessäni ja epäilemättä parantelen näitä.