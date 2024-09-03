# Enhetstest av Umami.Net - Test av UmamiBackgroundSender

# Inledning

I föregående artikel dryftade vi hur vi skulle pröva `UmamiClient` användning av xUnit och Moq. I den här artikeln ska vi gå igenom hur man testar `UmamiBackgroundSender` Klassen. I detta sammanhang är det viktigt att se till att `UmamiBackgroundSender` är lite annorlunda mot `UmamiClient` som den använder `IHostedService` att hålla igång i bakgrunden och skicka förfrågningar genom `UmamiClient` helt ut ur huvudutförande tråden (så att det inte blockerar avrättning).

Som vanligt kan du se alla källkoden för detta på min GitHub [här](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSenderTests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

Den faktiska strukturen hos `UmamiBackgroundSender` är ganska enkelt. Det är en värdtjänst som skickar förfrågningar till Umami-servern så fort den upptäcker en ny begäran. Grundstrukturen `UmamiBackgroundSender` Klassen visas nedan:

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

Som du kan se är detta bara en klassiker `IHostedService` det läggs till vår tjänstesamling i ASP.NET med hjälp av `services.AddHostedService<UmamiBackgroundSender>()` Metod. Det här drar igång `StartAsync` metod när ansökan börjar.
Den blick inuti den `SendRequest` Det är där magin händer. Här läser vi från kanalen och skickar förfrågan till Umami-servern.

Detta utesluter de faktiska metoderna för att skicka förfrågningarna (visas nedan).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Allt dessa verkligen gör är att paketera begäran upp i `SendBackgroundPayload` Spela in och skicka den till kanalen.

Vårt näste får slinga in `SendRequest` kommer att fortsätta läsa från kanalen tills den är stängd. Det är här vi kommer att fokusera våra testinsatser.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Bakgrundstjänsten har några semantik som gör det möjligt att bara skjuta av meddelandet så snart det anländer.
Men detta väcker ett problem; om vi inte får ett returnerat värde från `Send` Hur ska vi testa det här?

## Provning `UmamiBackgroundSender`

Så frågan är hur vi ska testa denna tjänst femn det finns inget svar på faktiskt testa mot?

Svaret är att injicera `HttpMessageHandler` till den hånade HttpClient vi skickar in i vår UmamiClient. Detta kommer att tillåta oss att fånga upp begäran och kontrollera dess innehåll.

### EchoMockHttpMessageHandler

Du kommer ihåg från förra artikeln vi satte upp en mock HttpMessageHandler. Det här lever inne i världen. `EchoMockHandler` statisk klass:

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

Du kan se här vi använder Mock för att sätta upp en `SendAsync` metod som kommer att returnera ett svar baserat på begäran (i HttpClient alla async förfrågningar görs genom `SendAsync`).

Du ser att vi först sätter upp Mock

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Vi använder sedan magin av `Protected` om inrättande av `SendAsync` Metod. Detta beror på att `SendAsync` är normalt inte tillgänglig i det offentliga API av `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Vi använder sedan bara catch-all `ItExpr.IsAny` att matcha varje begäran och returnera svaret från `responseFunc` Vi passerar in.

## Testmetoder.

Inuti `UmamiBackgroundSender_Tests` Vi har ett gemensamt sätt att definiera alla testmetoder.

### Ställ in

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

När vi väl har definierat detta måste vi hantera våra `IHostedService` Livslängd i testmetoden:

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

Du kan se att vi passerar i handlern till vår `GetServices` Inställningsmetod:

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

Här passerar vi i vår handläggare till våra tjänster för att kroka det i `UmamiClient` Uppställning.

Vi lägger sedan till `UmamiBackgroundSender` till tjänsteinsamlingen och få `IHostedService` från tjänsteleverantören. Lämna sedan tillbaka den här till testklassen för att tillåta den att användas.

#### Värdtjänst Livstid

Nu när vi har alla dessa uppsättningar kan vi helt enkelt `StartAsync` värdtjänsten, använd den och vänta tills den slutar:

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

Detta kommer att starta värdtjänsten, skicka begäran, vänta på svaret sedan stoppa tjänsten.

### Meddelandehanterare

Vi börjar med att sätta upp `EchoMockHandler` och `TaskCompletionSource` vilket kommer att signalera att testet är slutfört. Detta är viktigt för att återställa sammanhanget till den huvudsakliga testtråden så att vi korrekt kan fånga fel och timeouts.

I detta sammanhang är det viktigt att se till att ` async (message, token) => {}` är den funktion vi passerar in i vår mock-handler vi nämnde ovan. Här kan vi kontrollera begäran och returnera ett svar (som i detta fall vi verkligen inte gör något med).

Våra `EchoMockHandler.ResponseHandler` är en hjälpare metod som kommer att återföra begäran kroppen tillbaka till vår metod, detta låter oss verifiera budskapet passerar genom `UmamiClient` till `HttpClient` Rätt.

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

Vi tar sedan detta svar och deserialisera det till en `EchoedRequest` motsätter sig detta. Detta är ett enkelt objekt som representerar begäran vi skickade till servern.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Du ser att detta inkapslar `Type` och `Payload` av begäran. Detta är vad vi kommer att kontrollera mot i vårt test.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Vad som är viktigt här är hur vi hanterar misslyckade tester, eftersom vi inte är i den viktigaste tråden sammanhanget här vi behöver använda `TaskCompletionSource` för att signalera tillbaka till huvudtråden att provningen har misslyckats.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Detta kommer att innebära ett undantag för `TaskCompletionSource` och returnera ett 500 fel till testet.

# Slutsatser

Så det är den första av mina mer detaljerade inlägg, `IHostedService` motiverar detta eftersom det är ganska komplicerat att testa när som här det inte returnerar ett värde till den som ringer.