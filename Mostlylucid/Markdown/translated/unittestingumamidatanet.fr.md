# Essai unitaire Umami.Net - Tester les données Umami sans utiliser Moq

# Présentation

Dans la partie précédente de cette série où j'ai testé[ Méthodes de suivi Umami.Net ](/blog/unittestingumaminet)

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T20:30</datetime>
[TOC]

## Le problème

Dans la partie précédente, j'ai utilisé Moq pour me donner un `Mock<HttpMessageHandler>` et de retourner le gestionnaire utilisé dans `UmamiClient`, c'est un modèle commun lors des tests de code qui utilise `HttpClient`C'est ce que j'ai dit. Dans ce post, je vais vous montrer comment tester le nouveau `UmamiDataService` sans utiliser Moq.

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

## Pourquoi utiliser Moq?

Moq est une puissante bibliothèque de maquette qui vous permet de créer des objets de maquette pour les interfaces et les classes. Il est largement utilisé dans les tests unitaires pour isoler le code en cours d'essai de ses dépendances. Cependant, dans certains cas, l'utilisation de Moq peut être lourde, voire impossible. Par exemple, lorsque le code d'essai utilise des méthodes statiques ou lorsque le code soumis à l'essai est étroitement couplé à ses dépendances.

L'exemple que j'ai donné ci-dessus donne beaucoup de flexibilité dans les tests `UmamiClient` classe, mais il a aussi quelques inconvénients. C'est du code UGLY et je fais beaucoup de trucs dont je n'ai pas vraiment besoin. Ainsi, lors de l'essai `UmamiDataService` J'ai décidé d'essayer une autre approche.

# Tester UmamiDataService

Les `UmamiDataService` est un futur ajout à la bibliothèque Umami.Net qui vous permettra de récupérer des données d'Umami pour des choses comme voir combien de vues une page avait, ce qui s'est passé d'un certain type, filtré par une tonne de paramètres liek country, ville, OS, taille d'écran, etc. C'est un très puissant mais en ce moment le [Umami API ne fonctionne que via JavaScript](https://umami.is/docs/api/website-stats)C'est ce que j'ai dit. Donc, voulant jouer avec ces données, j'ai passé par l'effort de créer un client C# pour elle.

Les `UmamiDataService` classe est divisé en classes partielles multple (les méthodes sont SUPER longue) par exemple voici le `PageViews` méthode.

Vous pouvez voir que MUCH du code construit la QueryString à partir de la classe passée dans PageViewsRequest (il y a d'autres façons de le faire, mais ceci, par exemple en utilisant Attributs ou travaux de réflexion ici).

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

Comme vous pouvez le voir, il suffit de construire une chaîne de requête. authentifie l'appel (voir [dernier article](/blog/unittestinglogginginaspnetcore) pour quelques détails sur cela) et ensuite fait l'appel à l'API Umami. Alors, comment testons-nous ça?

## Tester le UmamiDataService

Contrairement à UmamiClient, j'ai décidé de tester `UmamiDataService` sans utiliser Moq. Au lieu de cela, j'ai créé un simple `DelegatingHandler` classe qui me permet d'interroger la demande puis de retourner une réponse. C'est une approche beaucoup plus simple que d'utiliser Moq et me permet de tester la `UmamiDataService` sans avoir à se moquer de `HttpClient`.

Dans le code ci-dessous vous pouvez voir que j'étends simplement `DelegatingHandler` et outrepasser la `SendAsync` méthode. Cette méthode me permet d'inspecter la demande et de retourner une réponse en fonction de la demande.

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

## Configuration

Pour mettre en place le nouveau `UmamiDataService` d'utiliser ce gestionnaire est aussi simple.

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

Tu verras que je viens de mettre en place le `ServiceCollection`, ajouter le `FakeLogger<T>` (à nouveau voir le [dernier article pour plus de détails sur ce](/blog/unittestinglogginginaspnetcore) et ensuite mettre en place le `UmamiData` service avec le nom d'utilisateur et le mot de passe que je veux utiliser (pour que je puisse tester l'échec).

J'appelle ensuite `services.SetupUmamiData(username, password);` qui est une méthode d'extension que j'ai créé pour mettre en place le `UmamiDataService` avec `UmamiDataDelegatingHandler` et les `AuthService`;

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

Vous pouvez voir que c'est là que je m'accroche au `UmamiDataDelegatingHandler` et les `AuthService` à l'Organisation des Nations Unies pour l'alimentation et l'agriculture (FAO) `UmamiDataService`C'est ce que j'ai dit. La façon dont cela est structuré est que le `AuthService` 'sont' les `HttpClient` et les `UmamiDataService` utilise les `AuthService` pour faire les appels à l'API Umami avec le `bearer` en jeton et en jeton `BaseAddress` C'est déjà prêt.

## Les essais

Vraiment, ça rend vraiment le test vraiment simple. C'est juste un peu verbeux car je voulais aussi tester l'enregistrement. Tout ce qu'il fait c'est poster dans mon `DelegatingHandler` et je simule une réponse basée sur la demande.

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

### Simulation de la réponse

Pour simuler la réponse pour cette méthode, vous vous souviendrez que j'ai cette ligne dans la `UmamiDataDelegatingHandler`:

```csharp
  if (absPath.StartsWith($"/api/websites/{Consts.WebSiteId}/pageviews"))
                {
                    var pageViews = GetParams<PageViewsRequest> (request);
                  
                    return ReturnPageViewsMessage(pageViews);
                }
```

Tout ce que cela fait, c'est tirer des informations de la requête et construire une réponse "réaliste" (basée sur Live Tests que j'ai compilé, encore une fois très peu de docs sur ce). Vous verrez que je teste le nombre de jours entre la date de début et la date de fin, puis je retourne une réponse avec le même nombre de jours.

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

# En conclusion

Donc c'est vraiment assez simple de tester un `HttpClient` demander sans utiliser Moq et je pense que c'est beaucoup plus propre de cette façon. Vous perdez une partie de la sophistication rendue possible à Moq mais pour des tests simples comme celui-ci, je pense que c'est un bon compromis.