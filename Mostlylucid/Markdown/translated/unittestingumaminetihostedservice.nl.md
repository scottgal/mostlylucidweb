# Unit Testing Umami.Net - UmamiBackgroundSender testen

# Inleiding

In het vorige artikel bespraken we hoe we de `UmamiClient` gebruik van xUnit en Moq. In dit artikel bespreken we hoe we de `UmamiBackgroundSender` Klas. De `UmamiBackgroundSender` is een beetje anders dan `UmamiClient` zoals het gebruikt `IHostedService` om op de achtergrond te blijven draaien en verzoeken door te sturen `UmamiClient` volledig uit de belangrijkste executive thread (dus het blokkeert de uitvoering niet).

Zoals gewoonlijk kunt u alle broncode voor dit zien op mijn GitHub [Hier.](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSenderTests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

De werkelijke structuur van `UmamiBackgroundSender` Het is heel eenvoudig. Het is een gehoste dienst die verzoeken stuurt naar de Umami server zodra het een nieuw verzoek detecteert. De basisstructuur `UmamiBackgroundSender` De klasse is hieronder weergegeven:

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

Zoals je kunt zien is dit gewoon een klassieker. `IHostedService` het is toegevoegd aan onze service collectie in ASP.NET met behulp van de `services.AddHostedService<UmamiBackgroundSender>()` methode. Dit schopt de `StartAsync` methode wanneer de toepassing begint.
De blik in de `SendRequest` methode is waar de magie gebeurt. Hier lezen we van het kanaal en sturen we het verzoek naar de Umami server.

Dit sluit de werkelijke methoden voor het versturen van de verzoeken (hieronder weergegeven) uit.

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

De Voorzitter. - Het woord is aan de Fractie van de Europese Volkspartij (Christen-democratische Fractie). `SendBackgroundPayload` Neem op en stuur het naar het kanaal.

Onze geneste ontvangen loop in `SendRequest` zal blijven lezen vanaf het kanaal totdat het gesloten is. Hier zullen we onze testinspanningen concentreren.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

De achtergronddienst heeft een aantal semantiek die het mogelijk maken om gewoon af te schieten van het bericht zodra het aankomt.
Maar dit roept een probleem op; als we niet krijgen een geretourneerde waarde van de `Send` Hoe testen we of dit echt iets doet?

## Testen `UmamiBackgroundSender`

Dus dan is de vraag hoe testen we deze dienst vijfen is er geen reactie om daadwerkelijk te testen tegen?

Het antwoord is het injecteren van een `HttpMessageHandler` op de bespotte HttpClient die we naar onze UmamiClient sturen. Dit zal ons in staat stellen om het verzoek te onderscheppen en de inhoud ervan te controleren.

### EchoMockHttpMessageHandler

Je zult je herinneren uit het vorige artikel dat we een nep HttpMessageHandler hebben opgezet. Dit leeft in de `EchoMockHandler` statische klasse:

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

Je kunt hier zien dat we Mock gebruiken om een `SendAsync` methode die een antwoord geeft op basis van het verzoek (in HttpClient worden alle async verzoeken gedaan via `SendAsync`).

We zetten de Mock voor het eerst op.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Wij gebruiken dan de magie van `Protected` voor het opzetten van de `SendAsync` methode. Dit komt omdat... `SendAsync` is normaal gesproken niet toegankelijk in de openbare API van `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

We gebruiken dan gewoon de catch-all `ItExpr.IsAny` om aan elk verzoek te voldoen en het antwoord van de `responseFunc` We passeren.

## Testmethoden.

Binnenin de `UmamiBackgroundSender_Tests` klasse hebben we een gemeenschappelijke manier om alle testmethoden te definiëren.

### Instellen

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

Zodra we dit gedefinieerd hebben moeten we onze `IHostedService` de levensduur van de testmethode:

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

U kunt zien dat we in de handler naar onze `GetServices` setup methode:

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

Hier geven we in onze handler aan onze diensten om het te haken in de `UmamiClient` Installeren.

Vervolgens voegen we het volgende toe: `UmamiBackgroundSender` naar de service collectie en krijg de `IHostedService` van de dienstverlener. Breng dit dan terug naar de testklas om het gebruik ervan mogelijk te maken.

#### Hosted Service Lifetime

Nu we al deze dingen klaar hebben, kunnen we simpelweg... `StartAsync` de Hosted Service, gebruik het dan wachten tot het stopt:

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

Dit start de gehoste service, stuur het verzoek, wacht op het antwoord en stop de service.

### Message Handler

De Voorzitter. - Het woord is aan de Fractie van de Europese Volkspartij (Christen-democratische Fractie). `EchoMockHandler` en de `TaskCompletionSource` die zal aangeven dat de test is voltooid. Dit is belangrijk om de context terug te brengen naar de belangrijkste testdraad, zodat we fouten en time-outs correct kunnen vastleggen.

De ` async (message, token) => {}` Is de functie die we doorgeven in onze schijnverzorger die we hierboven noemden. Hierin kunnen we het verzoek checken en een reactie teruggeven (die we in dit geval echt niet doen).

Onze `EchoMockHandler.ResponseHandler` is een helper methode die het verzoek lichaam terug naar onze methode, dit laat ons controleren of het bericht door de `UmamiClient` aan de `HttpClient` Juist.

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

We grijpen dan deze reactie en deserializeren het in een `EchoedRequest` object. Dit is een eenvoudig object dat het verzoek vertegenwoordigt dat we naar de server hebben gestuurd.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

U ziet dit inkapselt de `Type` en `Payload` van het verzoek. Dit is wat we zullen controleren in onze test.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Wat hier van cruciaal belang is, is hoe we met falende tests omgaan, omdat we niet in de hoofdcontext zitten die we moeten gebruiken. `TaskCompletionSource` om terug te geven aan de hoofddraad dat de test is mislukt.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Dit zal de uitzondering op de `TaskCompletionSource` en breng een 500 fout terug naar de test.

# Conclusie

Dus dat is de eerste van mijn meer gedetailleerde berichten, `IHostedService` Het is nogal complex om te testen als het hier geen waarde teruggeeft aan de beller.