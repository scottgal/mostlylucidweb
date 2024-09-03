# Unit Testing Umami.Net - Testing UmamiHintergrundSender

# Einleitung

Im vorigen Artikel haben wir diskutiert, wie man die `UmamiClient` Verwendung von xUnit und Moq. In diesem Artikel werden wir diskutieren, wie man die `UmamiBackgroundSender` Unterricht. Das `UmamiBackgroundSender` ist ein bisschen anders als `UmamiClient` wie es verwendet `IHostedService` um im Hintergrund zu bleiben und Anfragen durch zu senden `UmamiClient` komplett aus dem Hauptausführungsgewinde heraus (es blockiert also nicht die Ausführung).

Wie immer kannst du den Quellcode dazu auf meinem GitHub sehen. [Hierher](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSenderTests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

Die tatsächliche Struktur der `UmamiBackgroundSender` ist ganz einfach. Es ist ein gehosteter Dienst, der Anfragen an den Umami-Server sendet, sobald er eine neue Anfrage erkennt. Die Grundstruktur `UmamiBackgroundSender` Die Klasse ist unten dargestellt:

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

Wie Sie sehen können, ist dies nur ein Klassiker `IHostedService` es wird zu unserer Service-Sammlung in ASP.NET mit dem hinzugefügt `services.AddHostedService<UmamiBackgroundSender>()` verfahren. Dies beginnt mit der `StartAsync` Methode, wenn die Anwendung beginnt.
Der Blick in den `SendRequest` Methode ist, wo die Magie geschieht. Hier lesen wir vom Kanal aus und senden die Anfrage an den Umami-Server.

Dies schließt die eigentlichen Methoden zum Senden der Anfragen aus (siehe unten).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Alles, was diese wirklich tun, ist die Anfrage in die `SendBackgroundPayload` Nehmen Sie es auf und senden Sie es an den Kanal.

Unsere verschachtelten empfangen Schleife in `SendRequest` wird vom Kanal lesen, bis es geschlossen ist. Hier werden wir unsere Testbemühungen fokussieren.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Der Hintergrunddienst hat einige Semantik, die es erlauben, einfach die Nachricht abzufeuern, sobald sie kommt.
Dies wirft jedoch ein Problem auf; wenn wir nicht einen zurückgegebenen Wert von der `Send` Wie testen wir, dass dies tatsächlich etwas tut?

## Prüfung `UmamiBackgroundSender`

Die Frage ist also, wie wir diesen Dienst 5n testen, da gibt es keine Antwort, gegen den wir eigentlich testen?

Die Antwort lautet: `HttpMessageHandler` an den verspotteten HttpClient, den wir in unseren UmamiClient schicken. Dadurch können wir die Anfrage abfangen und den Inhalt überprüfen.

### EchoMockHttpMessageHandler

Sie werden sich an den vorherigen Artikel erinnern, den wir einen HttpMessageHandler eingerichtet haben. Das Leben in der `EchoMockHandler` statische Klasse:

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

Sie können hier sehen, wir verwenden Mock, um eine `SendAsync` Methode, die eine Antwort basierend auf der Anfrage zurückgibt (in HttpClient werden alle async-Anfragen durchgeführt durch `SendAsync`).

Sehen Sie, wir haben zuerst den Mock eingerichtet.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Wir benutzen dann die Magie von `Protected` zur Einrichtung der `SendAsync` verfahren. Das ist, weil `SendAsync` ist normalerweise nicht zugänglich in der öffentlichen API von `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Wir benutzen dann einfach den Fang-all `ItExpr.IsAny` um jede Anfrage zu erfüllen und die Antwort von der `responseFunc` Wir gehen rein.

## Prüfverfahren.

Im Inneren des `UmamiBackgroundSender_Tests` Klasse haben wir einen gemeinsamen Weg, um alle Testmethoden zu definieren.

### Einrichtung

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

Wenn wir dies einmal definiert haben, müssen wir unsere `IHostedService` Lebensdauer der Prüfmethode:

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

Sie können sehen, dass wir im Handler zu unserem `GetServices` Einrichtungsmethode:

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

Hier gehen wir in unserem Handler zu unseren Dienstleistungen, um es in der `UmamiClient` Einrichtung.

Wir fügen dann die `UmamiBackgroundSender` zur Service-Sammlung und erhalten Sie die `IHostedService` vom Dienstleister. Dann geben Sie dies in die Testklasse zurück, um die Verwendung zu erlauben.

#### Gehostete Dienste lebenslänglich

Nun, da wir alle diese Einrichtungen haben, können wir einfach `StartAsync` der Hosted Service, verwenden Sie es dann warten, bis es stoppt:

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

Dies startet den gehosteten Service, sendet die Anfrage, wartet auf die Antwort und stoppt den Service.

### Message Handler

Wir beginnen zuerst mit der Einrichtung der `EchoMockHandler` und der `TaskCompletionSource` die das Signal geben, dass der Test abgeschlossen ist. Dies ist wichtig, um den Kontext zum Haupttest Thread zurückzugeben, damit wir Fehler und Timeouts korrekt erfassen können.

Das ` async (message, token) => {}` ist die Funktion, die wir in unseren mock handler übergeben, die wir oben erwähnt. Hier können wir die Anfrage überprüfen und eine Antwort zurückgeben (was wir in diesem Fall wirklich nicht tun).

Unsere `EchoMockHandler.ResponseHandler` ist ein Helfer-Methode, die die Anfrage Körper zurück zu unserer Methode, so können wir überprüfen, die Nachricht wird durch die `UmamiClient` zu dem `HttpClient` Richtig.

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

Wir greifen dann diese Antwort und deserialisieren sie in eine `EchoedRequest` Gegenstand. Dies ist ein einfaches Objekt, das die Anfrage darstellt, die wir an den Server gesendet haben.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Sie sehen, dass dies verkapselt die `Type` und `Payload` des Antrags. Darauf werden wir in unserem Test achten.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Was hier kritisch ist, ist, wie wir mit Fehlertests umgehen, da wir nicht im Haupt-Thread-Kontext hier sind, müssen wir verwenden `TaskCompletionSource` zurück zum Hauptgewinde zu signalisieren, dass der Test fehlgeschlagen ist.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Dies setzt die Ausnahme auf die `TaskCompletionSource` und einen 500-Fehler an den Test zurückgeben.

# Schlussfolgerung

Also das ist der erste meiner eher detaillierteren Beiträge, `IHostedService` Das rechtfertigt, da es ziemlich komplex ist, zu testen, wenn es wie hier keinen Wert an den Anrufer zurückgibt.