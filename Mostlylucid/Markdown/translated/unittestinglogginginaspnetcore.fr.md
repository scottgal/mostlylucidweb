# Essai d'unité Umami.Net - Logging dans ASP.NET Core

# Présentation

Je suis un noob relatif utilisant Moq (oui je suis au courant des controverses) et j'essayais de tester un nouveau service que j'ajoute à Umami.Net, UmamiData. C'est un service qui me permet de tirer des données de mon instance Umami à utiliser dans des choses comme le tri des messages par popularité etc...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# Le problème

J'essayais d'ajouter un test simple pour la fonction de connexion que j'ai besoin d'utiliser pour tirer des données.
Comme vous pouvez le voir, c'est un service simple qui passe un nom d'utilisateur et un mot de passe à la `/api/auth/login` et obtient un résultat. Si le résultat est réussi, il stocke le jeton dans le `_token` champ et définit le `Authorization` en-tête pour l'en-tête `HttpClient` à utiliser dans les demandes futures.

```csharp
public class AuthService(HttpClient httpClient, UmamiDataSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token = string.Empty;
    public HttpClient HttpClient => httpClient;

    public async Task<bool> LoginAsync()
    {
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                logger.LogError("Login failed");
                return false;
            }

            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
}
```

Maintenant, je voulais aussi tester contre l'enregistreur pour m'assurer qu'il enregistre les messages corrects. Je m'en sers. `Microsoft.Extensions.Logging` namespace et je voulais tester que les bons messages de journal étaient écrits à l'enregistreur.

Dans Moq il y a un BUNCH de messages autour de tester l'enregistrement, ils ont tous cette forme de base (à partir de https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

```csharp
public static Mock<ILogger<T>> VerifyDebugWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
{
    Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;
    
    logger.Verify(
        x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => state(v, t)),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

    return logger;
}
```

HOWEVER en raison des changements récents de Moq (It.IsAnyType est maintenant obsolète) et les changements ASP.NET Core à FormatedLogValues J'avais du mal à faire fonctionner ça.

J'ai essayé un BUNCH de versions et de variantes mais il a toujours échoué. Donc... j'ai abandonné.

# La solution

Donc, en lisant un tas de messages GitHub, je suis tombé sur un post de David Fowler (mon ancien collègue et maintenant le Seigneur du.NET) qui a montré une façon simple de tester la connexion dans ASP.NET Core.
Il s'agit de *nouveau pour moi* `Microsoft.Extensions.Diagnostics.Testing` [colis](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) qui a quelques extensions vraiment utiles pour tester l'enregistrement.

Donc au lieu de tous les trucs Moq, j'ai juste ajouté le `Microsoft.Extensions.Diagnostics.Testing` et a ajouté ce qui suit à mes tests.

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

Vous verrez que cela met en place mon ServiceCollection, ajoute la nouvelle `FakeLogger<T>` et ensuite mettre en place le `UmamiData` service avec le nom d'utilisateur et le mot de passe que je veux utiliser (pour que je puisse tester l'échec).

## Les tests utilisant FakeLogger

Alors mes tests peuvent devenir :

```csharp
    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var authService = serviceProvider.GetRequiredService<AuthService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var result = await authService.LoginAsync();
        var fakeLogger = (FakeLogger<AuthService>)authLogger;
        FakeLogCollector collector = fakeLogger.Collector; // Collector allows you to access the captured logs
         IReadOnlyList<FakeLogRecord> logs = collector.GetSnapshot();
         Assert.Contains("Login successful", logs.Select(x => x.Message));
        Assert.True(result);
    }
```

Où vous verrez que j'appelle simplement le `GetServiceProvider` méthode pour obtenir mon fournisseur de services, puis obtenir le `AuthService` et `ILogger<AuthService>` de la part du fournisseur de services.

Parce que je les ai mis en place comme `FakeLogger<T>` Je peux alors accéder au `FakeLogCollector` et `FakeLogRecord` d'obtenir les journaux et de les vérifier.

Ensuite, je peux simplement vérifier les journaux pour les messages corrects.

# En conclusion

Donc là, vous l'avez, une façon simple de tester les messages de journal dans les tests d'unité sans les absurdités Moq.