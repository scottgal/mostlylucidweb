# Pruebas de Unidad Umami.Net - Pruebas de UmamiClient

# Introducción

Ahora tengo el [Paquete Umami.Net](https://www.nuget.org/packages/Umami.Net/) Por supuesto que quiero asegurarme de que todo funciona como se esperaba. Para hacer esto la mejor manera es probar de manera global todos los métodos y clases. Aquí es donde entran las pruebas de unidad.
Nota: Este no es un post de tipo 'enfoque perfecto', es como lo he hecho actualmente. En realidad, no necesito mofarme de la `IHttpMessageHandler` aquí un puede atacar a un DelegatingMessageHandler a un HttpClient normal para hacer esto. Sólo quería mostrar cómo puedes hacerlo con un Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Pruebas por unidad

La prueba de unidad se refiere al proceso de prueba de unidades de código individuales para asegurarse de que funcionan como se esperaba. Esto se hace escribiendo pruebas que llaman a los métodos y clases de una manera controlada y luego comprobar la salida es como se esperaba.

Para un paquete como Umami.Net esto es muy complicado ya que ambos llaman a un cliente remoto `HttpClient` y tiene un `IHostedService` utiliza para hacer el envío de nuevos datos de eventos lo más sin problemas posible.

## Pruebas de UmamiClient

La mayor parte de las pruebas `HttpClient` biblioteca basada está evitando la llamada real 'HttpClient'. Esto se hace mediante la creación de un `HttpClient` que utiliza una `HttpMessageHandler` que devuelve una respuesta conocida. Esto se hace mediante la creación de un `HttpClient` con una `HttpMessageHandler` que devuelve una respuesta conocida; en este caso sólo hago eco de la respuesta de entrada y comprobar que no ha sido destrozado por el `UmamiClient`.

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

Como verás esto establece un `Mock<HttpMessageHandler>` A continuación, pasar a la `UmamiClient`.
En este código engancho esto en nuestro `IServiceCollection` método de configuración. Esto añade todos los servicios requeridos por el `UmamiClient` incluyendo nuestro nuevo `HttpMessageHandler` y luego devuelve el `IServiceCollection` para su uso en las pruebas.

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

Para usar esto e inyectarlo en la `UmamiClient` Entonces utilizo estos servicios en el `UmamiClient` Prepárate.

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

Verán que tengo un montón de parámetros opcionales alternativos que me permiten inyectar diferentes opciones para diferentes tipos de prueba.

### Las pruebas

Así que ahora tengo toda esta configuración en su lugar Ahora puedo empezar a escribir pruebas para el `UmamiClient` métodos.

#### Enviar

Lo que significa todo esto es que nuestras pruebas pueden ser bastante simples.

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

Aquí usted ve el caso de prueba más simple, sólo asegurando que el `UmamiClient` puede enviar un mensaje y obtener una respuesta; lo que es más importante, también se prueba para un caso de excepción donde el `type` Está mal. Esta es una parte a menudo pasada por alto de las pruebas, asegurando que el código falla como se esperaba.

#### Vista de página

Para probar nuestro método pageview podemos hacer algo similar. En el siguiente código utilizo mi `EchoHttpHandler` sólo para reflejar la respuesta enviada y asegurarse de que devuelve lo que espero.

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

Esto utiliza la `HttpContextAccessor` para establecer el camino a `/testpath` y luego comprueba que el `UmamiClient` envía esto correctamente.

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

Esto es importante para nuestro código de cliente Umami ya que gran parte de los datos enviados de cada solicitud se genera de forma dinámica a partir de la `HttpContext` objeto. Así que no podemos enviar nada en absoluto en un `await umamiClient.TrackPageView();` y todavía enviará los datos correctos extrayendo el Url de la `HttpContext`.

Como veremos más adelante, también es importante el asombro de enviar artículos como el `UserAgent` y `IPAddress` ya que estos son utilizados por el servidor Umami para rastrear los datos y las vistas de usuario de 'pista' sin usar cookies.

Con el fin de tener esto predecible definimos un montón de Consts en el `Consts` clase. Así que podemos probar contra respuestas y peticiones predecibles.

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

## Ensayos adicionales

Esto es sólo el comienzo de nuestra estrategia de prueba para Umami.Net, todavía tenemos que probar el `IHostedService` y probar con los datos reales que genera Umami (que no está documentado en ninguna parte pero contiene un token JWT con algunos datos útiles.)

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

Así que vamos a querer probar para eso, simular el token y posiblemente devolver los datos en cada visita (como recordarás esto está hecho de un `uuid(websiteId,ipaddress, useragent)`).

# Conclusión

Esto es sólo el comienzo de la prueba del paquete Umami.Net, hay mucho más que hacer, pero este es un buen comienzo. Añadiré más pruebas a medida que vaya y sin duda mejoraré estas.