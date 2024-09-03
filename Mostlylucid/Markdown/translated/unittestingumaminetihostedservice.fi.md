# Unit Testaa Umami.Net - Testaa UmamiBackgroundSender

# Johdanto

Edellisessä kirjoituksessa keskustelimme siitä, miten testattaisiin `UmamiClient` xUnitin ja Moqin käyttö. Tässä artikkelissa käsittelemme, miten testata `UmamiBackgroundSender` Luokka. Erytropoietiini `UmamiBackgroundSender` on vähän erilainen `UmamiClient` kuten se käyttää `IHostedService` pysyäkseen taustalla ja lähettääkseen pyyntöjä `UmamiClient` Täysin pois tärkeimmästä teloitusketjusta (joten se ei estä teloitusta).

Kuten tavallista, näet tämän lähdekoodin GitHubistani. [täällä](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSenderTests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

Varsinainen rakenne: `UmamiBackgroundSender` se on aika yksinkertaista. Se on isännöity palvelu, joka lähettää pyyntöjä Umami-palvelimelle heti, kun se havaitsee uuden pyynnön. Perusrakenne `UmamiBackgroundSender` luokka on esitetty alla:

```csharp
public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory, ILogger<UmamiBackgroundSender> logger) : IHostedService
{

    private  Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

    private Task _sendTask = Task.CompletedTask;
    
        public Task StartAsync(CancellationToken cancellationToken)
    {

        _sendTask = SendRequest(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }
    
            public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("UmamiBackgroundSender is stopping.");

            // Signal cancellation and complete the channel
            await _cancellationTokenSource.CancelAsync();
            _channel.Writer.Complete();
            try
            {
                // Wait for the background task to complete processing any remaining items
                await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("StopAsync operation was canceled.");
            }
        }
        
                private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload, type:payload.EventType);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
    
    }

```

Kuten näette, tämä on vain klassikko `IHostedService` Se on lisätty palvelukokoelmaamme ASP.netissä `services.AddHostedService<UmamiBackgroundSender>()` menetelmä. Tämä käynnistyy `StartAsync` menetelmä, kun sovellus alkaa.
Katse sisällä `SendRequest` metodi on se, missä taika tapahtuu. Täällä luemme kanavalta ja lähetämme pyynnön Umami-palvelimelle.

Tämä sulkee pois pyyntöjen varsinaiset lähetystavat (esitetty alla).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Kaikki nämä todella tekevät on paketoida pyyntö jopa `SendBackgroundPayload` kirjaa ja lähetä se kanavalle.

Pesimämme vastaanottaa silmukan `SendRequest` jatkaa lukemista kanavalta, kunnes se on suljettu. Tähän me keskitämme testaustyömme.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Taustapalvelussa on semantiikkaa, jonka avulla se voi vain laukaista viestin heti sen saavuttua.
Tämä kuitenkin aiheuttaa ongelmia, jos emme saa palautettua arvoa `Send` miten testaamme, että tämä oikeasti tekee mitään?

## Testit `UmamiBackgroundSender`

Joten kysymys kuuluukin, miten testaamme tätä palvelua viisikolla ei ole mitään vastausta siihen, mitä vastaan oikeasti testataan?

Vastaus on, että pistät ruiskeen. `HttpMessageHandler` Pilkatulle HttpClientille lähetämme UmamiClientin. Näin voimme siepata pyynnön ja tarkistaa sen sisällön.

### EchoMockHttpMessageHandler

Muistat varmaan edellisestä artikkelista, jonka järjestimme valekappaleen HttpMessageHandler. Tämä elää sisällä `EchoMockHandler` staattinen luokka:

```csharp
public static class EchoMockHandler
{
    public static HttpMessageHandler Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFunc)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                responseFunc(request, cancellationToken).Result);

        return mockHandler.Object;
    }
```

Tässä näette, että käytämme Mockia perustaaksemme `SendAsync` Menetelmä, joka palauttaa pyyntöön perustuvan vastauksen (HttpClientissä kaikki async-pyynnöt tehdään `SendAsync`).

Ensin lavastimme Mockin.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Sitten käytämme taikaa `Protected` Euroopan parlamentin ja neuvoston asetus (EU) N:o 1380/2013, annettu 11 päivänä joulukuuta 2013, Euroopan aluekehitysrahaston (EAKR) perustamisesta (EUVL L 347, 20.12.2013, s. 1). `SendAsync` menetelmä. Tämä johtuu siitä, että `SendAsync` Ei ole normaalisti saatavilla julkisen API `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Sitten käytämme vain camp-all `ItExpr.IsAny` Vastaamaan kaikkiin pyyntöihin ja palauttamaan vastauksen `responseFunc` ohitamme sisääntulon.

## Testimenetelmät.

Sisällä `UmamiBackgroundSender_Tests` Luokalla meillä on yhteinen tapa määritellä kaikki testimenetelmät.

### Asetukset

```csharp
[Fact]
    public async Task Track_Page_View()
    {
        var page = "https://background.com";
        var title = "Background Example Page";
        var tcs = new TaskCompletionSource<bool>();
        // Arrange
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Result.Content.ReadFromJsonAsync<EchoedRequest>(token);
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(page, jsonContent.Payload.Url);
                Assert.Equal(title, jsonContent.Payload.Title);
                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

Kun tämä on määritelty, meidän on hoidettava `IHostedService` Elinikä testimenetelmässä:

```csharp
       var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

Huomaat, että siirrymme käsittelijän kautta `GetServices` Setup-menetelmä:

```csharp
    private (UmamiBackgroundSender, IHostedService) GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        services.AddScoped<UmamiBackgroundSender>();
       

        services.AddScoped<IHostedService, UmamiBackgroundSender>(provider =>
            provider.GetRequiredService<UmamiBackgroundSender>());
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var backgroundSender = serviceProvider.GetRequiredService<UmamiBackgroundSender>();
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        return (backgroundSender, hostedService);
    }
```

Tässä siirrymme käsittelijämme palveluksiimme kytkeäksemme sen `UmamiClient` Lavastus.

Sitten lisäämme: `UmamiBackgroundSender` palvelukeräykseen ja saat `IHostedService` Palveluntuottajalta. Palauta tämä testiluokalle, jotta sitä voidaan käyttää.

#### Isännöitsijän elinikäistä palvelusta

Nyt kun meillä on kaikki nämä, voimme yksinkertaisesti `StartAsync` Hosted Service, käytä sitä ja odota, kunnes se loppuu:

```csharp
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
```

Tämä käynnistää isännöidyn palvelun, lähettää pyynnön, odottaa vastausta ja lopettaa palvelun.

### Viestinjakaja

Aloitamme luomalla `EchoMockHandler` ja `TaskCompletionSource` joka viestittää testin olevan valmis. Tämä on tärkeää, jotta asiayhteys saadaan palautettua päätestiketjuun, jotta voimme oikein vangita epäonnistumiset ja aikalisät.

Erytropoietiini ` async (message, token) => {}` on funktio, jonka välitämme edellä mainitulle pilkkakäsittelijällemme. Täällä voimme tarkistaa pyynnön ja palauttaa vastauksen (jolla emme tässä tapauksessa todellakaan tee mitään).

Meidän `EchoMockHandler.ResponseHandler` on auttajamenetelmä, joka palauttaa pyyntöelimen metodiimme, jonka avulla voimme varmistaa, että viesti kulkee `UmamiClient` Euroopan unionin toiminnasta tehtyyn sopimukseen ja Euroopan unionin toiminnasta tehtyyn sopimukseen liitetyssä pöytäkirjassa N:o 2 olevan 1 ja 2 kohdan mukaisesti. `HttpClient` oikein.

```csharp
    public static async Task<HttpResponseMessage> ResponseHandler(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Read the request content
        var requestBody = request.Content?.ReadAsStringAsync(cancellationToken).Result;
        // Create a response that echoes the request body
        var responseContent = requestBody ?? "No request body";
        // Return the response
        return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
```

Sen jälkeen tartumme tähän vastaukseen ja poistamme sen käytöstä `EchoedRequest` Esine. Tämä on yksinkertainen esine, joka edustaa pyyntöä, jonka lähetimme palvelimelle.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Tämä kiteyttää `Type` sekä `Payload` Pyynnöstä. Tätä vastaan tarkistamme testissämme.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Ratkaisevaa tässä on se, miten käsittelemme epäonnistuneita testejä, koska emme ole tässä yhteydessä keskeisessä asemassa. `TaskCompletionSource` viestittää takaisin päälangalle, että testi on epäonnistunut.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Tämä tekee poikkeuksen `TaskCompletionSource` ja palauta kokeeseen 500 virhettä.

# Johtopäätöksenä

Se on ensimmäinen hieman yksityiskohtaisempi tehtäväni. `IHostedService` Tämä on perusteltua, koska se on melko monimutkainen testata, kun kuten täällä se ei palauta arvoa soittajalle.