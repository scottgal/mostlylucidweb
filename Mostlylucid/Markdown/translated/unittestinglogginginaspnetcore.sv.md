# Enhetstestning Umami.Net - Loggning i ASP.NET Core

# Inledning

Jag är en släkting noob använder Moq (ja jag är medveten om kontroverser) och jag försökte testa en ny tjänst jag lägger till Umami.Net, UmamiData. Detta är en tjänst som gör att jag kan dra data från min Umami instans att använda i saker som sortering inlägg av popularitet etc...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">Förbehåll IIIA-PT-38</datetime>

# Problemet

Jag försökte lägga till ett enkelt test för inloggningsfunktionen som jag behöver använda när jag drar data.
Som du kan se är det en enkel tjänst som passerar ett användarnamn och lösenord till `/api/auth/login` ändpunkt och får ett resultat. Om resultatet är framgångsrikt lagrar den token i `_token` fält och ställer in `Authorization` sidhuvud för `HttpClient` att använda i framtida förfrågningar.

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

Nu ville jag också testa mot loggern för att se till att den loggade rätt meddelanden. Jag använder `Microsoft.Extensions.Logging` Namnrymden och jag ville testa att rätt loggmeddelanden skrevs till loggaren.

I Moq finns det en BUNCH av inlägg runt att testa loggning de alla har denna grundläggande form (från https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

Hur som helst på grund av Moqs senaste förändringar (IsAnyType är nu föråldrad) och ASP.NET Core förändringar i FormatedLogValues Jag hade svårt att få detta att fungera.

Jag provade en BUNCH av versioner och varianter men det misslyckades alltid. Så...jag gav upp.

# Lösningen

Så läser ett gäng GitHub meddelanden Jag kom över ett inlägg av David Fowler (min tidigare kollega och nu Lord of.NET) som visade ett enkelt sätt att testa loggning i ASP.NET Core.
Detta använder sig av *nytt för mig* `Microsoft.Extensions.Diagnostics.Testing` [förpackning](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) som har några riktigt användbara tillägg för att testa loggning.

Så istället för alla Moq grejer jag bara lagt till `Microsoft.Extensions.Diagnostics.Testing` paketera och lägga till följande i mina tester.

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

Du kommer att se att detta sätter upp min ServiceCollection, lägger till den nya `FakeLogger<T>` och sedan sätta upp `UmamiData` tjänst med användarnamn och lösenord jag vill använda (så att jag kan testa fel).

## Testerna med hjälp av falska logger

Då kan mina tester bli:

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

Där du kommer att se Jag helt enkelt ringa `GetServiceProvider` metod för att få min tjänsteleverantör, sedan få `AuthService` och `ILogger<AuthService>` från tjänsteleverantören.

Eftersom jag har dessa som `FakeLogger<T>` Jag kan sedan komma åt `FakeLogCollector` och `FakeLogRecord` för att hämta loggarna och kolla dem.

Då kan jag helt enkelt kolla loggarna efter rätt meddelanden.

# Slutsatser

Så där har du det, ett enkelt sätt att testa loggar meddelanden i Unit Tests utan Moq nonsens.