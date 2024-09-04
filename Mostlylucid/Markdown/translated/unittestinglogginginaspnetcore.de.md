# Unit Testing Umami.Net - Logging in ASP.NET Core

# Einleitung

Ich bin ein relativer Noob mit Moq (ja ich bin mir der Kontroversen bewusst) und ich habe versucht, einen neuen Service zu testen, den ich umami.Net, UmamiData hinzufügen werde. Dies ist ein Service, der mir erlaubt, Daten aus meiner Umami-Instanz zu ziehen, um in Sachen wie Sortieren von Beiträgen nach Beliebtheit usw. zu verwenden...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# Das Problem

Ich habe versucht, einen einfachen Test für die Login-Funktion hinzuzufügen, die ich verwenden muss, wenn ich Daten ziehe.
Wie Sie sehen können, ist es ein einfacher Service, der einen Benutzernamen und ein Passwort an die `/api/auth/login` Endpoint und bekommt ein Ergebnis. Wenn das Ergebnis erfolgreich ist, speichert es das Token in der `_token` Feld und setzt die `Authorization` header für die `HttpClient` zur Verwendung in zukünftigen Anfragen.

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

Jetzt wollte ich auch gegen den Logger testen, um sicherzustellen, dass er die richtigen Nachrichten protokolliert. Ich benutze die `Microsoft.Extensions.Logging` namespace und ich wollten testen, dass die richtigen Logmeldungen an den Logger geschrieben wurden.

In Moq gibt es eine BUNCH der Beiträge rund um das Testen der Protokollierung haben alle diese grundlegende Form (von https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

WIE auch immer aufgrund der jüngsten Änderungen von Moq (It.IsAnyType ist jetzt veraltet) und ASP.NET Core Änderungen von FormattedLogValues Ich hatte eine harte Zeit, dies zu funktionieren.

Ich habe eine BUNCH von Versionen und Varianten ausprobiert, aber es hat immer versagt. Also... gab ich auf.

# Die Lösung

So las ich eine Reihe von GitHub-Nachrichten, die ich auf einen Beitrag von David Fowler (mein ehemaliger Kollege und jetzt der Herr von.NET) stieß, die einen einfachen Weg, um die Protokollierung in ASP.NET Core zu testen zeigte.
Dabei wird die *neu für mich* `Microsoft.Extensions.Diagnostics.Testing` [Verpackung](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) die einige wirklich nützliche Erweiterungen zum Testen der Protokollierung hat.

Also statt all dem Moq Zeug habe ich gerade die `Microsoft.Extensions.Diagnostics.Testing` Paket und fügte die folgenden zu meinen Tests.

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

Sie werden sehen, dass dies meine ServiceCollection, fügt die neue `FakeLogger<T>` und stellt dann die `UmamiData` Service mit dem Benutzernamen und Passwort, das ich verwenden möchte (damit ich Fehler testen kann).

## Die Tests mit FakeLogger

Dann können meine Tests werden:

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

Wo Sie sehen werden, ich rufe einfach die `GetServiceProvider` Methode, um meinen Service-Provider zu bekommen, dann erhalten Sie die `AuthService` und `ILogger<AuthService>` vom Dienstleister.

Weil ich diese als `FakeLogger<T>` Ich kann dann auf die `FakeLogCollector` und `FakeLogRecord` um die Protokolle zu bekommen und sie zu überprüfen.

Dann kann ich einfach die Protokolle auf die richtigen Nachrichten überprüfen.

# Schlussfolgerung

So haben Sie es, eine einfache Möglichkeit, Protokollnachrichten in Unit Tests ohne den Moq-Unsinn zu testen.