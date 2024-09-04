# Unit Testing Umami.Net - Loggen in ASP.NET Core

# Inleiding

Ik ben een relatieve noob met behulp van Moq (ja ik ben me bewust van de controverses) en ik probeerde een nieuwe service te testen die ik toevoeg aan Umami.Net, UmamiData. Dit is een dienst die me toelaat om gegevens uit mijn Umami instantie te halen om te gebruiken in dingen zoals het sorteren van berichten door populariteit etc...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# Het probleem

Ik probeerde een eenvoudige test toe te voegen voor de login functie die ik moet gebruiken bij het trekken van gegevens.
Zoals u kunt zien is het een eenvoudige dienst die een gebruikersnaam en wachtwoord aan de `/api/auth/login` eindpunt en krijgt een resultaat. Als het resultaat succesvol is het slaat de token in de `_token` veld en stelt de `Authorization` kop voor de `HttpClient` te gebruiken in toekomstige verzoeken.

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

Nu wilde ik ook testen tegen de logger om er zeker van te zijn dat het de juiste berichten inlogde. Ik gebruik de `Microsoft.Extensions.Logging` namespace en ik wilden testen of de juiste logberichten naar de logger werden geschreven.

In Moq is er een BUNCH van berichten rond het testen van logging hebben ze allemaal dit basisformulier (van https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

Hoe dan ook als gevolg van Moq's recente veranderingen (It.IsAnyType is nu verouderd) en ASP.NET Core's wijzigingen in GeformatteerdeLogValues Ik had het moeilijk om dit te laten werken.

Ik probeerde een BUNCH van versies en varianten, maar het is altijd mislukt. Dus... gaf ik het op.

# De oplossing

Dus het lezen van een aantal GitHub berichten kwam ik over een post van David Fowler (mijn voormalige collega en nu de Lord of.NET) die een eenvoudige manier om te testen logging in ASP.NET Core toonde.
Dit maakt gebruik van de *nieuw voor mij* `Microsoft.Extensions.Diagnostics.Testing` [pakket](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) die heeft een aantal echt nuttige extensies voor het testen van logging.

Dus in plaats van al het Moq-gedoe heb ik net de `Microsoft.Extensions.Diagnostics.Testing` pakket en voegde het volgende toe aan mijn tests.

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

U zult zien dat dit zet mijn ServiceCollection, voegt de nieuwe `FakeLogger<T>` en zet dan de `UmamiData` service met de gebruikersnaam en wachtwoord die ik wil gebruiken (zodat ik een storing kan testen).

## De tests met behulp van FakeLogger

Dan kunnen mijn tests worden:

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

Waar je zult zien... bel ik gewoon... `GetServiceProvider` methode om mijn service provider te krijgen, dan krijgen de `AuthService` en `ILogger<AuthService>` van de dienstverlener.

Want ik heb deze opgezet als `FakeLogger<T>` Ik kan dan toegang krijgen tot de `FakeLogCollector` en `FakeLogRecord` Om de logs te halen en te controleren.

Dan kan ik gewoon de logs controleren op de juiste berichten.

# Conclusie

Dus daar heb je het, een eenvoudige manier om logberichten te testen in Unit Tests zonder de Moq onzin.