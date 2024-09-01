# Unità Test Umami.Net - Testing UmamiClient

# Introduzione

Ora ho la... [Pacchetto Umami.Net](https://www.nuget.org/packages/Umami.Net/) Là fuori naturalmente voglio assicurarsi che tutto funzioni come previsto. Per fare questo il modo migliore è quello di testare un po 'completamente tutti i metodi e le classi. E' qui che entrano in gioco i test dell'unita'.
Nota: Questo non è un 'approccio perfetto' tipo post, è solo come ho fatto attualmente. In realtà non ho davvero bisogno di prendere in giro il `IHttpMessageHandler` qui a si può attaccare un delegatoMessageHandler a un normale HttpClient per fare questo. Volevo solo mostrarti come puoi farlo con un Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Prova dell'unità

La prova di unità si riferisce al processo di prova di singole unità di codice per garantire che funzionino come previsto. Questo viene fatto scrivendo test che chiamano i metodi e le classi in modo controllato e poi controllando l'output è come previsto.

Per un pacchetto come Umami.Net questo è soewhat difficile come entrambi chiama un client remoto sopra `HttpClient` e ha un `IHostedService` utilizza per rendere l'invio di nuovi dati di evento il più semplice possibile.

## Prova UmamiClient

La maggior parte dei test `HttpClient` libreria basata sta evitando la chiamata 'HttpClient' reale. Questo è fatto creando un `HttpClient` che utilizza un `HttpMessageHandler` che restituisce una risposta nota. Questo è fatto creando un `HttpClient` con `HttpMessageHandler` che restituisce una risposta nota; in questo caso ho appena riecheggiare la risposta di input e controllare che non è stato mangled dal `UmamiClient`.

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

Come vedrai, questo sistema... `Mock<HttpMessageHandler>` Poi passo nel `UmamiClient`.
In questo codice metto questo nel nostro `IServiceCollection` metodo di setup. Questo aggiunge tutti i servizi richiesti dal `UmamiClient` incluso il nostro nuovo `HttpMessageHandler` e poi restituisce il `IServiceCollection` per l'uso nelle prove.

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

Per usare questo medicinale ed iniettarlo nel `UmamiClient` Ho poi utilizzato questi servizi nel `UmamiClient` Prepararsi.

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

Vedrete che qui ho un sacco di parametri alternativi opzionali che mi permettono di iniettare diverse opzioni per diversi tipi di test.

### Le prove

Quindi ora ho tutto questo setup sul posto che ora posso iniziare a scrivere test per il `UmamiClient` metodi.

#### Invia

Ciò che tutto questo setup significa è che i nostri test possono essere abbastanza semplici

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

Qui si vede il caso di prova più semplice, solo assicurando che il `UmamiClient` può inviare un messaggio e ottenere una risposta; soprattutto testiamo anche per un caso di eccezione in cui il `type` e' sbagliato. Si tratta di una parte spesso trascurata dei test, assicurando che il codice fallisca come previsto.

#### Vista pagina

Per testare il nostro metodo pageview possiamo fare qualcosa di simile. Nel codice qui sotto uso il mio `EchoHttpHandler` per riflettere solo indietro la risposta inviata e assicurarsi che rimandi indietro ciò che mi aspetto.

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

In questo modo si utilizza il `HttpContextAccessor` per impostare il percorso a `/testpath` e poi controlla che il `UmamiClient` invia questo correttamente.

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

Questo è importante per il nostro codice client Umami come gran parte dei dati inviati da ogni richiesta è effettivamente generato dinamicamente dal `HttpContext` Oggetto. Quindi non possiamo mandare nulla in un `await umamiClient.TrackPageView();` chiamata e invierà ancora i dati corretti estraendo l'Url dal `HttpContext`.

Come vedremo più tardi è anche importante il awe inviare articoli come il `UserAgent` e `IPAddress` come questi sono utilizzati dal server Umami per tracciare i dati e 'traccia' le viste degli utenti senza utilizzare i cookie.

Per avere questo prevedibile definiamo un gruppo di Conste nel `Consts` classe. Quindi possiamo testare risposte e richieste prevedibili.

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

## Ulteriori prove

Questo è solo l'inizio della nostra strategia di test per Umami.Net, dobbiamo ancora testare il `IHostedService` e test contro i dati reali generati da Umami (che non è documentato da nessuna parte ma contiene un token JWT con alcuni dati utili).

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

Quindi vorremmo testare per questo, simulare il token ed eventualmente restituire i dati di ogni visita (come ricorderete questo è fatto da un `uuid(websiteId,ipaddress, useragent)`).

# In conclusione

Questo è solo l'inizio del test del pacchetto Umami.Net, c'è molto di più da fare, ma questo è un buon inizio. Aggiungero' altri test man mano che andro' e senza dubbio migliorero' questi.