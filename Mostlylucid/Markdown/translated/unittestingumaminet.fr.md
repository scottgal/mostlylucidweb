# Essais unitaires Umami.Net - Essais UmamiClient

# Présentation

Maintenant j'ai le [Paquet Umami.Net](https://www.nuget.org/packages/Umami.Net/) Je veux bien sûr m'assurer que tout fonctionne comme prévu. Pour ce faire, la meilleure façon est de tester un peu complètement toutes les méthodes et les classes. C'est là que les tests unitaires entrent en jeu.
Note : Ce n'est pas un post de type 'approche parfaite', c'est comme ça que je l'ai fait actuellement. En réalité, je n'ai pas vraiment besoin de Mock the `IHttpMessageHandler` ici a vous pouvez attaquer un DelegatingMessageHandler à un HttpClient normal pour le faire. Je voulais juste montrer comment tu peux le faire avec un Mock.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024-09-01T17:22</datetime>

# Essai à l'unité

Les tests unitaires font référence au processus d'essai des unités de code individuelles pour s'assurer qu'elles fonctionnent comme prévu. Cela se fait en écrivant des tests qui appellent les méthodes et les classes d'une manière contrôlée et puis en vérifiant la sortie est comme prévu.

Pour un paquet comme Umami.Net c'est tellement difficile car il appelle tous les deux un client distant sur `HttpClient` et a un `IHostedService` il utilise pour rendre l'envoi de nouvelles données d'événement aussi transparente que possible.

## Essai UmamiClient

La majeure partie des essais `HttpClient` based library évite l'appel réel 'HttpClient'. Cela se fait par la création d'un `HttpClient` qui utilise un `HttpMessageHandler` qui renvoie une réponse connue. Cela se fait par la création d'un `HttpClient` avec une `HttpMessageHandler` qui renvoie une réponse connue; dans ce cas, je fais juste écho à la réponse d'entrée et de vérifier qui n'a pas été massacré par le `UmamiClient`.

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

Comme vous le verrez, cela met en place un `Mock<HttpMessageHandler>` Je passe ensuite dans le `UmamiClient`.
Dans ce code, j'accroche ça à notre `IServiceCollection` méthode de configuration. Cela ajoute tous les services requis par le `UmamiClient` y compris notre nouvelle `HttpMessageHandler` et retourne ensuite le `IServiceCollection` pour une utilisation dans les essais.

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

Pour l'utiliser et l'injecter dans le `UmamiClient` J'utilise ensuite ces services dans `UmamiClient` l'installation.

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

Vous verrez que j'ai un tas de paramètres optionnels alternatifs ici me permettant d'injecter différentes options pour différents types de tests.

### Les essais

Donc maintenant j'ai toute cette configuration en place, je peux maintenant commencer à écrire des tests pour le `UmamiClient` les méthodes de travail.

#### Envoyer

Tout ce que cette configuration signifie, c'est que nos tests peuvent en fait être assez simples

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

Ici vous voyez le cas de test le plus simple, juste en s'assurant que le `UmamiClient` peut envoyer un message et obtenir une réponse; surtout, nous testons également pour un cas d'exception où le `type` C'est faux. Il s'agit d'une partie souvent négligée des tests, s'assurant que le code échoue comme prévu.

#### Affichage de la page

Pour tester notre méthode de vision de page, nous pouvons faire quelque chose de similaire. Dans le code ci-dessous, j'utilise mon `EchoHttpHandler` de revenir sur la réponse envoyée et de s'assurer qu'elle renvoie ce que j'attends.

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

Il s'agit de `HttpContextAccessor` pour définir le chemin à `/testpath` et vérifie ensuite que les `UmamiClient` l'envoie correctement.

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

Ceci est important pour notre code client Umami car une grande partie des données envoyées de chaque demande est en fait générée dynamiquement à partir de la `HttpContext` objet. Donc nous ne pouvons rien envoyer du tout dans un `await umamiClient.TrackPageView();` appel et il enverra toujours les données correctes en extrayant l'Url de la `HttpContext`.

Comme nous le verrons plus tard, il est également important que l'émerveillement envoie des éléments comme le `UserAgent` et `IPAddress` comme ils sont utilisés par le serveur Umami pour suivre les données et « suivre » les vues des utilisateurs sans utiliser de cookies.

Pour que cela soit prévisible, nous définissons un groupe de Consts dans le `Consts` En cours. Nous pouvons donc tester les réponses et les demandes prévisibles.

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

## Essais supplémentaires

Ce n'est que le début de notre stratégie de test pour Umami.Net, nous devons encore tester le `IHostedService` et test contre les données réelles Umami génère (qui n'est documenté nulle part mais contient un jeton JWT avec quelques données utiles.)

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

Donc, nous allons vouloir tester pour cela, simuler le jeton et éventuellement renvoyer les données sur chaque visite (comme vous vous souviendrez que ceci est fait à partir d'un `uuid(websiteId,ipaddress, useragent)`).

# En conclusion

Ce n'est que le début du test du paquet Umami.Net, il y a beaucoup plus à faire, mais c'est un bon début. J'ajouterai d'autres tests à mesure que je m'en vais et je les améliorerai sans aucun doute.