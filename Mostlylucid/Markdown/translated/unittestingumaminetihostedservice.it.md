# Unit Testing Umami.Net - Testing UmamiBackgroundSender

# Introduzione

Nell'articolo precedente, abbiamo discusso come testare la `UmamiClient` usando xUnit e Moq. In questo articolo, discuteremo come testare il `UmamiBackgroundSender` classe. La `UmamiBackgroundSender` è un po 'diverso da `UmamiClient` come usa `IHostedService` per rimanere in esecuzione in background e inviare richieste attraverso `UmamiClient` completamente fuori dalla thread di esecuzione principale (in modo che non blocchi l'esecuzione).

Come al solito puoi vedere tutto il codice sorgente per questo sul mio GitHub [qui](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

La struttura effettiva di `UmamiBackgroundSender` E' molto semplice. Si tratta di un servizio ospitato che invia richieste al server Umami non appena rileva una nuova richiesta. La struttura di base `UmamiBackgroundSender` la classe è mostrata di seguito:

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

Come potete vedere questo è solo un classico `IHostedService` è aggiunto alla nostra collezione di servizi in ASP.NET utilizzando il `services.AddHostedService<UmamiBackgroundSender>()` metodo. Questo dà il via al `StartAsync` metodo all'inizio dell'applicazione.
Lo sguardo all'interno del `SendRequest` metodo è dove la magia accade. Qui è dove leggiamo dal canale e inviamo la richiesta al server Umami.

Ciò esclude i metodi effettivi per inviare le richieste (indicate di seguito).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Tutto ciò che fa veramente è impacchettare la richiesta fino al `SendBackgroundPayload` registrare e inviarlo al canale.

Il nostro nidificato ricevere loop in `SendRequest` continuerà a leggere dal canale fino a quando non sarà chiuso. E' qui che concentreremo i nostri sforzi di sperimentazione.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Il servizio di background ha alcune semantiche che gli permettono di spegnere il messaggio appena arriva.
Tuttavia questo solleva un problema; se non otteniamo un valore restituito dal `Send` Come facciamo a testare che questo stia davvero facendo qualcosa?

## Prova `UmamiBackgroundSender`

Quindi la domanda è come facciamo a testare questo servizio cinquen non c'è risposta a test effettivamente contro?

La risposta è quella di iniettare `HttpMessageHandler` al client HttpClient che mandiamo nel nostro UmamiClient. Questo ci permetterà di intercettare la richiesta e controllare il suo contenuto.

### EchoMockHttpMessageHandler

Ricorderete dall'articolo precedente che abbiamo creato un finto HttpMessageHandler. Questo vive all'interno del `EchoMockHandler` classe statica:

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

Potete vedere qui usiamo Mock per impostare un `SendAsync` metodo che restituirà una risposta basata sulla richiesta (in HttpClient tutte le richieste async vengono effettuate attraverso `SendAsync`).

Vedete, abbiamo preparato il Mock per la prima volta.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Poi usiamo la magia di `Protected` per impostare il `SendAsync` metodo. Questo è perché `SendAsync` non è normalmente accessibile nelle API pubbliche di `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Poi usiamo solo il catch-all `ItExpr.IsAny` per corrispondere a qualsiasi richiesta e restituire la risposta dal `responseFunc` Passiamo dentro.

## Metodi di prova.

All'interno della `UmamiBackgroundSender_Tests` classe abbiamo un modo comune per definire tutti i metodi di prova.

### Configurazione

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

Una volta che abbiamo questo definito abbiamo bisogno di gestire il nostro `IHostedService` durata nel metodo di prova:

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

Potete vedere che passiamo nel gestore al nostro `GetServices` metodo di setup:

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

Qui passiamo nel nostro gestore ai nostri servizi per agganciarlo nel `UmamiClient` Prepararsi.

A questo punto aggiungiamo l'emendamento n. `UmamiBackgroundSender` alla raccolta di servizi e ottenere il `IHostedService` dal fornitore del servizio. Poi restituisci questo alla classe test per consentirne l'uso.

#### Hosted Service Lifetime

Ora che abbiamo tutte queste cose, possiamo semplicemente... `StartAsync` il Servizio Hosted, utilizzarlo quindi attendere che si fermi:

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

Questo avvierà il servizio ospitato, inviare la richiesta, attendere la risposta poi interrompere il servizio.

### Gestore messaggi

Iniziamo con l'istituzione del `EchoMockHandler` e della `TaskCompletionSource` che segnalerà che il test è completo. Questo è importante per riportare il contesto al thread di test principale in modo da poter catturare correttamente i guasti e i timeout.

La ` async (message, token) => {}` è la funzione che passiamo nel nostro simulatore che abbiamo menzionato sopra. Qui possiamo verificare la richiesta e restituire una risposta (che in questo caso in realtà non facciamo nulla con).

La nostra `EchoMockHandler.ResponseHandler` è un metodo helper che restituirà il corpo della richiesta al nostro metodo, questo ci permette di verificare che il messaggio sta passando attraverso il `UmamiClient` alla `HttpClient` Giusto.

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

Poi afferrare questa risposta e deserializzarlo in un `EchoedRequest` Oggetto. Questo è un semplice oggetto che rappresenta la richiesta che abbiamo inviato al server.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Vedete questo incapsula il `Type` e `Payload` della richiesta. Questo è ciò contro cui controlleremo nel nostro test.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Ciò che è critico qui è come gestiamo i test falliti, come non siamo nel contesto principale thread qui abbiamo bisogno di utilizzare `TaskCompletionSource` per segnalare al thread principale che il test non è riuscito.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Questo metterà l'eccezione sulla `TaskCompletionSource` e restituire un errore di 500 alla prova.

# In conclusione

Questo è il primo dei miei post più dettagliati, `IHostedService` garantisce questo perché è piuttosto complesso da testare quando come qui non restituisce un valore al chiamante.