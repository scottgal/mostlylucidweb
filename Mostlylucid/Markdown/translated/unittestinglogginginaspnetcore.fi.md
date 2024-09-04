# Unit Testing Umami.Net - Kirjautuminen ASP.NET Core

# Johdanto

Olen Moqia käyttävä suhteellinen noob (kyllä, olen tietoinen kiistoista) ja yritin testata uutta palvelua, jota lisään Umamiin.Net, UmamiData. Tämä on palvelu, jonka avulla voin vetää Umami-instanssin dataa käyttööni muun muassa valikointitehtävissä suosiolla jne....

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024–09–04T13:22</datetime>

# Ongelma

Yritin lisätä yksinkertaisen testin kirjautumistoiminnolle, jota tarvitsen dataa vetäessäni.
Kuten näet, se on yksinkertainen palvelu, joka välittää käyttäjätunnuksen ja salasanan `/api/auth/login` päätepiste ja saadaan tulos. Jos tulos on onnistunut, se tallentaa kupongin `_token` Kenttä ja asettaa `Authorization` Otsikko `HttpClient` käytettäväksi tulevissa pyynnöissä.

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

Nyt halusin myös testata metsuria varmistaakseni, että se kirjautuu oikeisiin viesteihin. Käytän... `Microsoft.Extensions.Logging` namespace ja minä halusimme testata, että metsurille kirjoitetaan oikeat lokiviestit.

Moqissa on BUNCH-virkoja, joissa testataan puunkorjuuta. Kaikilla on tämä peruslomake (https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/).

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

Moqin viimeaikaisten muutosten (It.IsAnyType on nyt vanhentunut) ja ASP.NET Coren FormattedLogValuesin muutosten vuoksi minun oli vaikea saada tätä toimimaan.

Kokeilin BUNCH-versioita ja -versioita, mutta se epäonnistui aina. Joten... Luovutin.

# Ratkaisu

Joten lukiessani joukkoa GitHub-viestejä törmäsin David Fowlerin (entinen kollegani ja nyt Lord of.NET) viestiin, joka näytti yksinkertaisen tavan testata kirjautumista ASP.NET Coressa.
Tässä käytetään *minulle uutta* `Microsoft.Extensions.Diagnostics.Testing` [paketti](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) Jolla on todella hyödyllisiä laajennuksia puunkorjuun testaamiseen.

Moq-juttujen sijaan lisäsin... `Microsoft.Extensions.Diagnostics.Testing` paketoi ja lisäsi testiini seuraavat:

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

Huomaat, että tämä perustaa ServiceCollectionin, lisää uusi `FakeLogger<T>` ja sitten perustaa `UmamiData` Palvelu käyttäjätunnuksella ja salasanalla, jota haluan käyttää (jotta voin testata epäonnistumista).

## Testauksia väärennöksillä

Sitten kokeistani voi tulla:

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

Missä näet, minä vain soitan `GetServiceProvider` Metodi saada minun palveluntarjoaja, sitten saada `AuthService` sekä `ILogger<AuthService>` Palveluntuottajalta.

Koska minulla on nämä valmiina `FakeLogger<T>` Sitten voin käyttää `FakeLogCollector` sekä `FakeLogRecord` Hakemaan lokit ja tarkastamaan ne.

Sitten voin vain tarkistaa lokit oikeiden viestien varalta.

# Johtopäätöksenä

Siinä se on, yksinkertainen tapa testata lokiviestejä Unit Testsissä ilman moq-hölynpölyä.