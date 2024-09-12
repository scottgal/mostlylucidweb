# Pruebas de unidad Umami.Net - Pruebas de datos de Umami sin usar Moq

# Introducción

En la parte anterior de esta serie donde probé[ Métodos de seguimiento de Umami.Net ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## El problema

En la parte anterior usé Moq para darme un `Mock<HttpMessageHandler>` y devolver el manejador utilizado en `UmamiClient`, este es un patrón común cuando se prueba el código que utiliza `HttpClient`. En este post te mostraré cómo probar el nuevo `UmamiDataService` sin utilizar Moq.

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

## ¿Por qué usar Moq?

Moq es una poderosa biblioteca de burlas que le permite crear objetos simulados para interfaces y clases. Es ampliamente utilizado en pruebas de unidades para aislar el código bajo prueba de sus dependencias. Sin embargo, hay algunos casos en que el uso de Moq puede ser engorroso o incluso imposible. Por ejemplo, cuando se prueba el código que utiliza métodos estáticos o cuando el código bajo prueba está estrechamente acoplado a sus dependencias.

El ejemplo que he dado anteriormente da una gran flexibilidad en la prueba de la `UmamiClient` clase, pero también tiene algunos inconvenientes. Es código UGLY y hace un montón de cosas que realmente no necesito. Así que cuando se prueba `UmamiDataService` Decidí probar un enfoque diferente.

# Probando UmamiDataService

Los `UmamiDataService` es una adición futura a la biblioteca Umami.Net que le permitirá obtener datos de Umami para cosas como ver cuántas vistas tenía una página, qué eventos ocurrieron de cierto tipo, filtrado por una tonelada de parámetros liek país, ciudad, sistema operativo, tamaño de la pantalla, etc. Este es un muy poderoso pero ahora mismo el [La API de Umami sólo funciona a través de JavaScript](https://umami.is/docs/api/website-stats). Así que queriendo jugar con esos datos pasé por el esfuerzo de crear un cliente C# para ello.

Los `UmamiDataService` clase se divide en clases parciales multple (los métodos son SUPER long) por ejemplo aquí está el `PageViews` método.

Puede ver que MUCHO del código está construyendo el QueryString desde la clase passed en PageViewsRequest (hay otras maneras de hacer esto, pero esto, por ejemplo usando Atributos o trabajos de reflexión aquí).

<details>
<summary>GetPageViews</summary>
```csharp
    public async Task<UmamiResult<PageViewsResponseModel>> GetPageViews(PageViewsRequest pageViewsRequest)
    {
        if (await authService.LoginAsync() == false)
            return new UmamiResult<PageViewsResponseModel>(HttpStatusCode.Unauthorized, "Failed to login", null);
        // Start building the query string
        var queryParams = new List<string>
        {
            $"startAt={pageViewsRequest.StartAt}",
            $"endAt={pageViewsRequest.EndAt}",
            $"unit={pageViewsRequest.Unit.ToLowerString()}"
        };

        // Add optional parameters if they are not null
        if (!string.IsNullOrEmpty(pageViewsRequest.Timezone)) queryParams.Add($"timezone={pageViewsRequest.Timezone}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Url)) queryParams.Add($"url={pageViewsRequest.Url}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Referrer)) queryParams.Add($"referrer={pageViewsRequest.Referrer}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Title)) queryParams.Add($"title={pageViewsRequest.Title}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Host)) queryParams.Add($"host={pageViewsRequest.Host}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Os)) queryParams.Add($"os={pageViewsRequest.Os}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Browser)) queryParams.Add($"browser={pageViewsRequest.Browser}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Device)) queryParams.Add($"device={pageViewsRequest.Device}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Country)) queryParams.Add($"country={pageViewsRequest.Country}");
        if (!string.IsNullOrEmpty(pageViewsRequest.Region)) queryParams.Add($"region={pageViewsRequest.Region}");
        if (!string.IsNullOrEmpty(pageViewsRequest.City)) queryParams.Add($"city={pageViewsRequest.City}");

        // Combine the query parameters into a query string
        var queryString = string.Join("&", queryParams);

        // Make the HTTP request
        var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/pageviews?{queryString}");

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Successfully got page views");
            var content = await response.Content.ReadFromJsonAsync<PageViewsResponseModel>();
            return new UmamiResult<PageViewsResponseModel>(response.StatusCode, response.ReasonPhrase ?? "Success",
                content ?? new PageViewsResponseModel());
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.LoginAsync();
            return await GetPageViews(pageViewsRequest);
        }

        logger.LogError("Failed to get page views");
        return new UmamiResult<PageViewsResponseModel>(response.StatusCode,
            response.ReasonPhrase ?? "Failed to get page views", null);
    }
```

</details>
Como se puede ver esto realmente sólo construye una cadena de consulta. autentifica la llamada (consulte la [último artículo](/blog/unittestinglogginginaspnetcore) para algunos detalles sobre esto) y luego hace la llamada a la API de Umami. Entonces, ¿cómo probamos esto?

## Probando el UmamiDataService

En contraste con la prueba de UmamiClient, decidí probar el `UmamiDataService` sin utilizar Moq. En su lugar, creé un simple `DelegatingHandler` clase que me permite interrogar la petición y luego devolver una respuesta. Este es un enfoque mucho más simple que el uso de Moq y me permite probar el `UmamiDataService` sin tener que burlarse de la `HttpClient`.

En el código de abajo puedes ver que simplemente extiendo `DelegatingHandler` y anular el `SendAsync` método. Este método me permite inspeccionar la solicitud y devolver una respuesta basada en la solicitud.

```csharp
public class UmamiDataDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var absPath = request.RequestUri.AbsolutePath;
        switch (absPath)
        {
            case "/api/auth/login":
                var authContent = await request.Content.ReadFromJsonAsync<AuthRequest>(cancellationToken);
                if (authContent?.username == "username" && authContent?.password == "password")
                    return ReturnAuthenticatedMessage();
                else if (authContent?.username == "bad")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
            default:

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }

                if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/metrics"))
                {
                    var metricsRequest = GetParams<MetricsRequest>(request);
                    return ReturnMetrics(metricsRequest);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
 }
```

## Configuración

Para configurar el nuevo `UmamiDataService` usar este manejador es similarmente simple.

```csharp
    public IServiceProvider GetServiceProvider (string username="username", string password="password")
    {
        var services = new ServiceCollection();
        var mockLogger = new FakeLogger<UmamiDataService>();
        var authLogger = new FakeLogger<AuthService>();
        services.AddScoped<ILogger<UmamiDataService>>(_ => mockLogger);
        services.AddScoped<ILogger<AuthService>>(_ => authLogger);
        services.SetupUmamiData(username, password);
        return  services.BuildServiceProvider();
        
    }
```

Ya verás que acabo de preparar el `ServiceCollection`, añádase el `FakeLogger<T>` (ver de nuevo el [último artículo para más detalles sobre esto](/blog/unittestinglogginginaspnetcore) y luego establecer el `UmamiData` servicio con el nombre de usuario y la contraseña que quiero usar (para que pueda probar el fallo).

A continuación, llamo a `services.SetupUmamiData(username, password);` que es un método de extensión que creé para configurar el `UmamiDataService` con la `UmamiDataDelegatingHandler` y el `AuthService`;

```csharp
    public static void SetupUmamiData(this IServiceCollection services, string username="username", string password="password")
    {
        var umamiSettings = new UmamiDataSettings()
        {
            UmamiPath = Consts.UmamiPath,
            Username = username,
            Password = password,
            WebsiteId = Consts.WebSiteId
        };
        services.AddSingleton(umamiSettings);
        services.AddHttpClient<AuthService>((provider,client) =>
        {
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
            

        }).AddHttpMessageHandler<UmamiDataDelegatingHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));  //Set lifetime to five minutes

        services.AddScoped<UmamiDataDelegatingHandler>();
        services.AddScoped<UmamiDataService>();
    }
```

Puedes ver que aquí es donde me engancho en el `UmamiDataDelegatingHandler` y el `AuthService` a las Naciones Unidas `UmamiDataService`. La forma en que esto está estructurado es que el `AuthService` "Posee" el `HttpClient` y el `UmamiDataService` utiliza la `AuthService` para hacer las llamadas a la API de Umami con el `bearer` token y `BaseAddress` ya está listo.

## Las pruebas

Realmente esto hace que las pruebas realmente tan simple. Es sólo un poco verboso, ya que también quería probar el registro también. Todo lo que está haciendo es publicar a través de mi `DelegatingHandler` y simulo una respuesta basada en la solicitud.

```csharp
public class UmamiData_PageViewsRequest_Test : UmamiDataBase
{
    private readonly DateTime StartDate = DateTime.ParseExact("2021-10-01", "yyyy-MM-dd", null);
    private readonly DateTime EndDate = DateTime.ParseExact("2021-10-07", "yyyy-MM-dd", null);
    
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var umamiDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var umamiDataLogger = serviceProvider.GetRequiredService<ILogger<UmamiDataService>>();
        var result = await umamiDataService.GetPageViews(StartDate, EndDate);
        var fakeAuthLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeAuthLogger.Collector; 
        IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
        Assert.Contains("Login successful", logs.Select(x => x.Message));
        
        var fakeUmamiDataLogger = (FakeLogger<UmamiDataService>)umamiDataLogger;
        FakeLogCollector umamiDataCollector = fakeUmamiDataLogger.Collector;
        IReadOnlyList<FakeLogRecord> umamiDataLogs = umamiDataCollector.GetSnapshot();
        Assert.Contains("Successfully got page views", umamiDataLogs.Select(x => x.Message));
        
        Assert.NotNull(result);
    }
}
```

### Simulación de la respuesta

Para simular la respuesta para este método recordaré que tengo esta línea en el `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Todo lo que esto hace es extraer información de la cadena de consulta y construye una respuesta'realista' (basada en pruebas en vivo que he compilado, de nuevo muy pocos documentos sobre esto). Verá que pruebo el número de días entre la fecha de inicio y fin y luego devolver una respuesta con el mismo número de días.

```csharp
    private static HttpResponseMessage ReturnPageViewsMessage(PageViewsRequest request)
    {
        var startAt = request.StartAt;
        var endAt = request.EndAt;
        var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startAt).DateTime;
        var endDate = DateTimeOffset.FromUnixTimeMilliseconds(endAt).DateTime;
        var days = (endDate - startDate).Days;

        var pageViewsList = new List<PageViewsResponseModel.Pageviews>();
        var sessionsList = new List<PageViewsResponseModel.Sessions>();
        for(int i=0; i<days; i++)
        {
            
            pageViewsList.Add(new PageViewsResponseModel.Pageviews()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*4
            });
            sessionsList.Add(new PageViewsResponseModel.Sessions()
            {
                x = startDate.AddDays(i).ToString("yyyy-MM-dd"),
                y = i*8
            });
        }
        var pageViewResponse = new PageViewsResponseModel()
        {
            pageviews = pageViewsList.ToArray(),
            sessions = sessionsList.ToArray()
        };
        var json = JsonSerializer.Serialize(pageViewResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
```

# Conclusión

Así que eso es realmente es bastante simple para probar un `HttpClient` solicitud sin usar Moq y creo que es mucho más limpio de esta manera. Pierdes algo de la sofisticación que es posible en Moq pero para pruebas simples como esta, creo que es una buena compensación.