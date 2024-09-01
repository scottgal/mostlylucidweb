# Unit Testing Umami.Net - Testing UmamiClient

# Einleitung

Jetzt habe ich die [Umami.Net Paket](https://www.nuget.org/packages/Umami.Net/) Da draußen möchte ich natürlich dafür sorgen, dass alles wie erwartet funktioniert. Um dies zu tun, ist der beste Weg, alle Methoden und Klassen etwas umfassend zu testen. Hier kommt der Unit Test ins Spiel.
Hinweis: Dies ist kein 'perfekter Ansatz' Typ Post, es ist gerade, wie ich es gerade getan habe. In Wirklichkeit brauche ich nicht wirklich, um die `IHttpMessageHandler` Hier kann man einen DelegatingMessageHandler an einen normalen HttpClient angreifen, um dies zu tun. Ich wollte nur zeigen, wie man es mit einem Mock machen kann.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Einheitenprüfung

Einheitenprüfung bezieht sich auf den Prozess der Prüfung einzelner Codeeinheiten, um sicherzustellen, dass sie wie erwartet funktionieren. Dies geschieht durch das Schreiben von Tests, die die Methoden und Klassen auf kontrollierte Weise aufrufen und dann die Ausgabe überprüfen, ist wie erwartet.

Für ein Paket wie Umami.Net ist dies soewas wie schwierig, da es beide Anrufe ein entfernter Client über `HttpClient` und hat eine `IHostedService` es verwendet, um das Senden neuer Ereignisdaten so nahtlos wie möglich zu machen.

## Test von UmamiClient

Der Hauptteil der Prüfung `HttpClient` based library vermeidet den eigentlichen 'HttpClient'-Aufruf. Dies geschieht durch die Schaffung eines `HttpClient` , die eine `HttpMessageHandler` das eine bekannte Antwort zurückgibt. Dies geschieht durch die Schaffung eines `HttpClient` mit einem `HttpMessageHandler` Das liefert eine bekannte Antwort; in diesem Fall ich nur Echo zurück die Eingabe-Antwort und überprüfen, die nicht durch die `UmamiClient`.

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

Wie Sie sehen werden, stellt dies eine `Mock<HttpMessageHandler>` Ich gehe dann in die `UmamiClient`.
In diesem Code stecke ich das in unsere `IServiceCollection` Einrichtungsmethode. Dies fügt alle Dienstleistungen, die durch die `UmamiClient` einschließlich unserer neuen `HttpMessageHandler` und gibt dann die `IServiceCollection` zur Verwendung in den Tests.

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

Um dies zu verwenden und injizieren Sie es in die `UmamiClient` Ich nutze diese Dienste dann in der `UmamiClient` Einrichtung.

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

Sie werden sehen, ich habe eine Reihe von alternativen optionalen Parameter hier ermöglicht es mir, verschiedene Optionen für verschiedene Testtypen injizieren.

### Die Prüfungen

Also jetzt habe ich all diese Einrichtung an Ort und Stelle kann ich jetzt mit dem Schreiben von Tests für die `UmamiClient` Methoden.

#### Senden

Was all dieses Setup bedeutet, ist, dass unsere Tests wirklich ziemlich einfach sein können

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

Hier sehen Sie den einfachsten Testfall, nur um sicherzustellen, dass die `UmamiClient` können eine Nachricht senden und eine Antwort erhalten; wichtig ist, dass wir auch für einen Ausnahmefall testen, in dem die `type` ist falsch. Dies ist ein oft übersehener Teil des Testens, um sicherzustellen, dass der Code wie erwartet fehlschlägt.

#### Seitenansicht

Um unsere Pageview-Methode zu testen, können wir etwas Ähnliches tun. Im Code unten verwende ich meine `EchoHttpHandler` um nur die gesendete Antwort zurück zu reflektieren und sicherzustellen, dass es zurücksendet, was ich erwarte.

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

Dabei wird die `HttpContextAccessor` zur Einstellung des Pfades auf `/testpath` und dann überprüft, dass die `UmamiClient` sendet dies korrekt.

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

Dies ist für unseren Umami-Client-Code wichtig, da viele der von jeder Anfrage gesendeten Daten tatsächlich dynamisch aus dem `HttpContext` Gegenstand. So können wir überhaupt nichts in einem schicken `await umamiClient.TrackPageView();` Anruf und es wird immer noch die richtigen Daten durch Extrahieren der Url aus dem senden `HttpContext`.

Wie wir später sehen, ist es auch wichtig, die Ehrfurcht senden Gegenstände wie die `UserAgent` und `IPAddress` da diese vom Umami-Server verwendet werden, um die Daten und 'Track'-Benutzeransichten ohne Verwendung von Cookies zu verfolgen.

Um dies vorhersagbar zu haben, definieren wir eine Reihe von Consts in der `Consts` Unterricht. So können wir gegen vorhersehbare Antworten und Anfragen testen.

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

## Weitere Prüfungen

Dies ist nur der Anfang unserer Teststrategie für Umami.Net, müssen wir immer noch die `IHostedService` und Test gegen die eigentlichen Daten, die Umami generiert (die nirgendwo dokumentiert ist, aber ein JWT-Token mit nützlichen Daten enthält).

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

Also wollen wir darauf testen, das Token simulieren und eventuell die Daten bei jedem Besuch zurückgeben (da Sie sich erinnern werden, dass dies aus einem `uuid(websiteId,ipaddress, useragent)`).

# Schlussfolgerung

Dies ist nur der Anfang des Testens des Umami.Net Pakets, es gibt viel mehr zu tun, aber das ist ein guter Anfang. Ich werde weitere Tests hinzufügen, während ich gehe und zweifellos diese verbessern.