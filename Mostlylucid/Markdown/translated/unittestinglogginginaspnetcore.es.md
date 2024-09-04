# Pruebas de Unidades Umami.Net - Iniciar sesión en ASP.NET Core

# Introducción

Soy un noob relativo usando Moq (sí, soy consciente de las controversias) y estaba tratando de probar un nuevo servicio que estoy añadiendo a Umami.Net, UmamiData. Este es un servicio que me permite extraer datos de mi instancia de Umami para usar en cosas como ordenar mensajes por popularidad, etc...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024-09-04T13:22</datetime>

# El problema

Estaba tratando de añadir una simple prueba para la función de inicio de sesión que necesito utilizar al extraer datos.
Como se puede ver es un servicio simple que pasa un nombre de usuario y contraseña a la `/api/auth/login` endpoint y obtiene un resultado. Si el resultado es exitoso almacena el token en el `_token` campo y establece el `Authorization` cabecera de la `HttpClient` utilizar en futuras solicitudes.

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

Ahora también quería probar contra el registrador para asegurarme de que estaba registrando los mensajes correctos. Estoy usando el `Microsoft.Extensions.Logging` espacio de nombres y quería probar que los mensajes de registro correctos estaban siendo escritos al registrador.

En Moq hay un BUNCH de publicaciones alrededor de la prueba de registro que todos tienen esta forma básica (de https://adamstorr.co.uk/blog/mocking-ilogger-with-moq/)

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

NO obstante, debido a los cambios recientes de Moq (It.IsAnyType ahora es obsoleto) y a los cambios del núcleo de ASP.NET a FormattedLogValues, me estaba costando conseguir que esto funcionara.

Probé un BUNCH de versiones y variantes, pero siempre falló. Así que... me di por vencido.

# La solución

Así que leyendo un montón de mensajes de GitHub me encontré con un post de David Fowler (mi antiguo colega y ahora el Señor de.NET) que mostró una manera sencilla de probar el registro en ASP.NET Core.
Esto utiliza la *nuevo para mí* `Microsoft.Extensions.Diagnostics.Testing` [paquete](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) que tiene algunas extensiones realmente útiles para probar el registro.

Así que en lugar de todas las cosas Moq acabo de añadir el `Microsoft.Extensions.Diagnostics.Testing` y añadió lo siguiente a mis pruebas.

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

Verás que esto establece mi ServiceCollection, agrega el nuevo `FakeLogger<T>` y luego establece el `UmamiData` servicio con el nombre de usuario y la contraseña que quiero usar (para que pueda probar el fallo).

## Las pruebas que utilizan el FakeLogger

Entonces mis pruebas pueden ser:

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

Donde verás, simplemente llamo a la `GetServiceProvider` método para obtener mi proveedor de servicios, a continuación, obtener el `AuthService` y `ILogger<AuthService>` del prestador de servicios.

Porque tengo estos establecidos como `FakeLogger<T>` Entonces puedo acceder a la `FakeLogCollector` y `FakeLogRecord` para conseguir los registros y comprobarlos.

Entonces puedo simplemente comprobar los registros para los mensajes correctos.

# Conclusión

Así que ahí lo tienes, una forma simple de probar los mensajes de registro en pruebas de unidad sin la tontería Moq.