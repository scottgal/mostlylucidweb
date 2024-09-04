# Unità Test Umami.Net - Registrazione in ASP.NET Core

# Introduzione

Sono un noob relativo che usa Moq (sì sono a conoscenza delle controversie) e stavo cercando di testare un nuovo servizio che sto aggiungendo a Umami.Net, UmamiData. Questo è un servizio questo mi permette di estrarre i dati dalla mia istanza Umami da utilizzare in roba come ordinare i messaggi per popolarità ecc...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# Il problema

Stavo cercando di aggiungere un semplice test per la funzione di login che ho bisogno di utilizzare quando si tirano i dati.
Come potete vedere è un servizio semplice che passa un nome utente e la password al `/api/auth/login` Endpoint e ottiene un risultato. Se il risultato è di successo memorizza il gettone nel `_token` campo e imposta il `Authorization` intestazione per il `HttpClient` da utilizzare nelle future richieste.

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

Ora volevo anche testare contro il logger per assicurarmi che stesse registrando i messaggi corretti. Sto usando il `Microsoft.Extensions.Logging` namespace e volevo verificare che i messaggi di log corretti fossero scritti al logger.

In Moq c'è un BUNCH di post su testing logging che hanno tutti questo modulo di base (da https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

Tuttavia, a causa delle recenti modifiche di Moq (It.IsAnyType è ormai obsoleto) e delle modifiche di ASP.NET Core a FormattedLogValues stavo avendo difficoltà a farlo funzionare.

Ho provato un BUNCH di versioni e varianti ma ha sempre fallito. Quindi... mi sono arreso.

# La soluzione

Così leggendo un mucchio di messaggi GitHub mi sono imbattuto in un post di David Fowler (il mio ex collega e ora il Signore di.NET) che ha mostrato un modo semplice per testare la registrazione in ASP.NET Core.
In questo modo si utilizza il *nuovo a me* `Microsoft.Extensions.Diagnostics.Testing` [pacchetto](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) che ha alcune estensioni davvero utili per testare la registrazione.

Così invece di tutte le cose Moq ho appena aggiunto il `Microsoft.Extensions.Diagnostics.Testing` imballo e aggiunto quanto segue ai miei test.

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

Vedrete che questo imposta il mio ServiceCollection, aggiunge il nuovo `FakeLogger<T>` e poi impostare il `UmamiData` servizio con il nome utente e la password che voglio utilizzare (in modo da poter testare il guasto).

## I test utilizzando FakeLogger

Allora i miei test possono diventare:

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

Dove vedrai, ti chiamo semplicemente... `GetServiceProvider` metodo per ottenere il mio fornitore di servizi, poi ottenere il `AuthService` e `ILogger<AuthService>` dal fornitore del servizio.

Perche' questi li ho preparati come... `FakeLogger<T>` Posso quindi accedere al `FakeLogCollector` e `FakeLogRecord` Per prendere i registri e controllarli.

Allora posso semplicemente controllare i registri per i messaggi corretti.

# In conclusione

Ecco qui, un modo semplice per testare i messaggi di registro nei test di unità senza le sciocchezze Moq.