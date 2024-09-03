# Essais unitaires Umami.Net - Tests UmamiBackgroundSender

# Présentation

Dans l'article précédent, nous avons discuté de la façon de tester `UmamiClient` utilisant xUnit et Moq. Dans cet article, nous discuterons de la façon de tester `UmamiBackgroundSender` En cours. Les `UmamiBackgroundSender` est un peu différent de `UmamiClient` comme il l'utilise `IHostedService` pour rester en cours d'exécution en arrière-plan et envoyer des demandes à travers `UmamiClient` complètement hors du thread principal d'exécution (pour qu'il ne bloque pas l'exécution).

Comme d'habitude, vous pouvez voir tout le code source pour cela sur mon GitHub [Ici.](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

La structure actuelle `UmamiBackgroundSender` est assez simple. C'est un service hébergé qui envoie des requêtes au serveur Umami dès qu'il détecte une nouvelle requête. La structure de base `UmamiBackgroundSender` la classe est indiquée ci-dessous:

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

Comme vous pouvez le voir, ce n'est qu'un classique `IHostedService` il est ajouté à notre collection de services dans ASP.NET en utilisant le `services.AddHostedService<UmamiBackgroundSender>()` méthode. C'est le coup d'envoi. `StartAsync` méthode lorsque l'application commence.
Le regard à l'intérieur du `SendRequest` méthode est l'endroit où la magie se produit. C'est là que nous lisons depuis le canal et envoyons la requête au serveur Umami.

Cela exclut les méthodes réelles d'envoi des demandes (voir ci-dessous).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Tout ce qu'ils font vraiment, c'est le paquetage de la demande vers le haut dans le `SendBackgroundPayload` Enregistrez-le et envoyez-le à la chaîne.

Nos imbriqués reçoivent une boucle en `SendRequest` continuera à lire depuis le canal jusqu'à ce qu'il soit fermé. C'est là que nous concentrerons nos efforts d'essai.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

Le service d'arrière-plan a quelques sémantiques qui lui permettent d'éteindre le message dès qu'il arrive.
Cependant, cela soulève un problème; si nous n'obtenons pas une valeur retournée de la `Send` Comment testons-nous que ça fait quelque chose?

## Essais `UmamiBackgroundSender`

Alors la question est de savoir comment tester ce service cinqn il n'y a pas de réponse au test réel contre?

La réponse est d'injecter `HttpMessageHandler` à l'HttpClient que nous envoyons dans notre UmamiClient. Cela nous permettra d'intercepter la demande et de vérifier son contenu.

### EchoMockHttpMessageHandler

Vous vous souviendrez de l'article précédent, nous avons créé une maquette de HttpMessageHandler. Cela vit à l'intérieur du `EchoMockHandler` classe statique:

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

Vous pouvez voir ici que nous utilisons Mock pour mettre en place un `SendAsync` méthode qui retournera une réponse en fonction de la demande (dans HttpClient toutes les demandes d'async sont faites par `SendAsync`).

On a d'abord installé le Mock.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Nous utilisons alors la magie de `Protected` pour mettre en place le `SendAsync` méthode. C'est parce que `SendAsync` n'est normalement pas accessible dans l'API publique de `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Ensuite, on utilise le catch-all. `ItExpr.IsAny` pour correspondre à n'importe quelle demande et retourner la réponse de la `responseFunc` Nous passons à l'intérieur.

## Méthodes d'essai.

À l'intérieur du `UmamiBackgroundSender_Tests` Nous avons un moyen commun de définir toutes les méthodes de test.

### Configuration

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

Une fois cela défini, nous devons gérer notre `IHostedService` durée de vie dans la méthode d'essai:

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

Vous pouvez voir que nous passons dans le gestionnaire à notre `GetServices` méthode de configuration & #160;:

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

Ici, nous passons dans notre gestionnaire à nos services pour l'accrocher dans le `UmamiClient` l'installation.

Nous ajoutons ensuite le `UmamiBackgroundSender` à la collecte de service et obtenir le `IHostedService` de la part du fournisseur de services. Ensuite, retournez ceci à la classe de test pour permettre son utilisation.

#### Durée de vie des services hébergés

Maintenant que nous avons tout cela mis en place, nous pouvons simplement `StartAsync` le Service hébergé, utilisez-le puis attendez jusqu'à ce qu'il s'arrête:

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

Cela va démarrer le service hébergé, envoyer la demande, attendre la réponse puis arrêter le service.

### Handler du message

Nous commençons d'abord par mettre en place la `EchoMockHandler` et les `TaskCompletionSource` qui signalera que l'essai est terminé. Ceci est important pour retourner le contexte au thread de test principal afin que nous puissions capturer correctement les échecs et les temps d'attente.

Les ` async (message, token) => {}` est la fonction que nous transmettons à notre manipulateur fictif que nous avons mentionné ci-dessus. Ici, nous pouvons vérifier la demande et retourner une réponse (qui dans ce cas nous ne faisons vraiment rien avec).

Notre `EchoMockHandler.ResponseHandler` est une méthode d'aide qui retournera le corps de la requête à notre méthode, cela nous permet de vérifier que le message passe à travers le `UmamiClient` à l'Organisation des Nations Unies pour l'alimentation et l'agriculture (FAO) `HttpClient` correctement.

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

Nous saisissons alors cette réponse et la désérialisons en une `EchoedRequest` objet. C'est un objet simple qui représente la requête que nous avons envoyée au serveur.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Vous voyez cela encapsule le `Type` et `Payload` de la demande. C'est ce que nous allons vérifier dans notre test.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Ce qui est critique ici, c'est la façon dont nous traitons les tests en échec, car nous ne sommes pas dans le contexte principal du thread ici nous devons utiliser `TaskCompletionSource` pour signaler au fil principal que l'essai a échoué.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Cela va fixer l'exception sur le `TaskCompletionSource` et retourner une erreur de 500 au test.

# En conclusion

Donc c'est le premier de mes posts plus détaillés, `IHostedService` justifie cela car il est assez complexe de tester quand comme ici il ne retourne pas une valeur à l'appelant.