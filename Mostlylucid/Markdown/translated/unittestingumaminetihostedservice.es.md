# Pruebas de Unidad Umami.Net - Pruebas de UmamiAntecedentesSender

# Introducción

En el artículo anterior, discutimos cómo probar el `UmamiClient` usando xUnit y Moq. En este artículo, vamos a discutir cómo probar el `UmamiBackgroundSender` clase. Los `UmamiBackgroundSender` es un poco diferente a `UmamiClient` como se utiliza `IHostedService` para seguir corriendo en segundo plano y enviar peticiones a través de `UmamiClient` completamente fuera del hilo de ejecución principal (por lo que no bloquea la ejecución).

Como de costumbre se puede ver todo el código fuente para esto en mi GitHub [aquí](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024-09-03T09:00</datetime>

## `UmamiBackgroundSender`

La estructura real de `UmamiBackgroundSender` es bastante simple. Es un servicio alojado que envía peticiones al servidor Umami tan pronto como detecta una nueva solicitud. Estructura básica `UmamiBackgroundSender` la clase se muestra a continuación:

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

Como se puede ver esto es sólo un clásico `IHostedService` se añade a nuestra colección de servicios en ASP.NET utilizando el `services.AddHostedService<UmamiBackgroundSender>()` método. Esto da comienzo a la `StartAsync` método cuando se inicia la aplicación.
La mirada dentro de la `SendRequest` método es donde sucede la magia. Aquí es donde leemos desde el canal y enviamos la solicitud al servidor Umami.

Esto excluye los métodos reales para enviar las solicitudes (que se muestran a continuación).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Todo esto realmente hace es empaquetar la petición hasta en el `SendBackgroundPayload` grabar y enviarlo al canal.

Nuestro anidado recibe lazo en `SendRequest` seguirá leyendo desde el canal hasta que esté cerrado. Aquí es donde enfocaremos nuestros esfuerzos de prueba.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

El servicio de fondo tiene algunas semánticas que le permiten simplemente disparar el mensaje tan pronto como llega.
Sin embargo esto plantea un problema; si no obtenemos un valor devuelto de la `Send` ¿Cómo probamos que esto realmente está haciendo algo?

## Ensayos `UmamiBackgroundSender`

Entonces la pregunta es ¿cómo probamos este servicio cincon no hay respuesta a la prueba en realidad contra?

La respuesta es inyectar un `HttpMessageHandler` al cliente Http que enviamos a nuestro cliente Umami. Esto nos permitirá interceptar la solicitud y comprobar su contenido.

### EchoMockHttpMensajeHandler

Recordarás del artículo anterior que preparamos una maqueta de HttpMessageHandler. Esto vive dentro del `EchoMockHandler` clase estática:

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

Puedes ver aquí que usamos Mock para configurar un `SendAsync` método que devolverá una respuesta basada en la solicitud (en HttpClient todas las solicitudes de sincronización se realizan a través de `SendAsync`).

Verás, primero preparamos el Mock.

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Entonces usamos la magia de `Protected` para establecer el sistema de `SendAsync` método. Esto es porque `SendAsync` normalmente no es accesible en la API pública de `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Entonces sólo usamos el cuadro de todos. `ItExpr.IsAny` para que coincida con cualquier petición y devolver la respuesta de la `responseFunc` Pasamos.

## Métodos de prueba.

Dentro de la `UmamiBackgroundSender_Tests` clase tenemos una manera común de definir todos los métodos de prueba.

### Configuración

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

Una vez que hayamos definido esto tenemos que manejar nuestro `IHostedService` vida útil en el método de ensayo:

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

Usted puede ver que pasamos en el controlador a nuestro `GetServices` método de configuración:

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

Aquí pasamos en nuestro manejador a nuestros servicios para engancharlo en el `UmamiClient` Prepárate.

A continuación, añadimos el `UmamiBackgroundSender` a la colección de servicios y obtener el `IHostedService` del prestador de servicios. Luego devuelve esto a la clase de prueba para permitir su uso.

#### Servicio alojado por toda la vida

Ahora que tenemos todo esto establecido podemos simplemente `StartAsync` el Servicio Hosted, utilícelo y luego espere hasta que se detenga:

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

Esto iniciará el servicio alojado, enviará la solicitud, esperará a la respuesta y luego detendrá el servicio.

### Manipulador de mensajes

En primer lugar, comenzamos por la creación de la `EchoMockHandler` y el `TaskCompletionSource` que indicará que la prueba está completa. Esto es importante para devolver el contexto al hilo de prueba principal para que podamos capturar correctamente los fallos y los tiempos de espera.

Los ` async (message, token) => {}` es la función que pasamos a nuestro manipulador simulado que mencionamos anteriormente. Aquí podemos comprobar la solicitud y devolver una respuesta (que en este caso realmente no hacemos nada con).

Nuestro `EchoMockHandler.ResponseHandler` es un método de ayuda que devolverá el cuerpo de solicitud a nuestro método, esto nos permite verificar que el mensaje está pasando a través de la `UmamiClient` a las Naciones Unidas `HttpClient` correctamente.

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

Luego tomamos esta respuesta y la deserializamos en un `EchoedRequest` objeto. Este es un objeto simple que representa la petición que enviamos al servidor.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Ves que esto encapsula la `Type` y `Payload` de la solicitud. Esto es lo que vamos a comprobar en nuestra prueba.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Lo que es crítico aquí es cómo manejamos las pruebas que fallan, ya que no estamos en el contexto principal del hilo aquí que necesitamos usar `TaskCompletionSource` para indicar de nuevo al hilo principal que la prueba ha fallado.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

Esto establecerá la excepción en el `TaskCompletionSource` y devolver un error de 500 a la prueba.

# Conclusión

Así que ese es el primero de mis posts más detallados, `IHostedService` garantiza esto ya que es bastante complejo para probar cuando como aquí no devuelve un valor a la persona que llama.