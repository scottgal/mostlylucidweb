# Перевірка модулів Umami.Net - ведення журналу в ядрі ASP. NET

# Вступ

Я відносний нуб, використовуючи Мока (так, я знаю про суперечки) і я намагався перевірити нову службу, яку додаю до Umami.Net, UmamiData. Це служба, яка дозволяє мені доставляти дані з мого примірника "Умамі" в такі речі, як сортування постів за популярністю тощо...

[TOC]

<!--category-- xUnit, ASP.NET Core -->
<datetime class="hidden">2024- 09- 04T13: 22</datetime>

# Проблема

Я намагався додати простий тест для функції входу до системи, яку мені потрібно використовувати для отримання даних.
Як ви можете бачити, це проста служба, яка передає ім' я користувача і пароль до `/api/auth/login` кінцева точка і результат. Якщо результат буде успішним, ключ зберігатиметься у `_token` поле і встановлює `Authorization` заголовок для `HttpClient` для використання у наступних запитах.

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

Тепер я також хотів перевірити лісозаготівля, щоб переконатися, що він реєструє правильні повідомлення. Я використовую `Microsoft.Extensions.Logging` Простір і я хотіли перевірити, чи правильні журнальні повідомлення написані до лісозаготівля.

У Moq є BUNCH дописів навколо тестування ведення журналу, вони всі мають цю базову форму (з https: // adamstorr.co.uk/blog/ilog-ilogger- with-moq /)

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

ЯК ЖЕ через нещодавні зміни Мока (In.IssameType зараз застаріла) і зміни ядра ASP.NET до FortedLogValues I was a hard tracking this to work.

Я спробував версію та варіанти, але все це завжди було невдалим. Так что... я сдался.

# Розв'язання

Тому, читаючи купу GitHub повідомлень, я натрапив на пост Девіда Фаулера (мій колишнього колеги, а тепер і Володара NET), який показав простий спосіб перевірити лісозаготівель в Ядрі ASPNET.
This using the *новий для мене* `Microsoft.Extensions.Diagnostics.Testing` [пакунок](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.Diagnostics.Testing) у цій програмі передбачено декілька справді корисних додатків для тестування журналу.

Отже, замість всього Мока я щойно додав `Microsoft.Extensions.Diagnostics.Testing` і додав наступні дані до моїх тестів.

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

Ви побачите, що це встановлює мою ServiceCollection, додає новий `FakeLogger<T>` а потім встановлює `UmamiData` служба з іменем користувача і паролем, яку я хочу використовувати (так що я можу перевірити помилку).

## Тести, які використовують підроблювач

Тоді мої випробування можуть стати такими:

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

Там де побачиш, я просто дзвоню `GetServiceProvider` метод отримання мого постачальника послуг, а потім отримання `AuthService` і `ILogger<AuthService>` від постачальника послуг.

Тому що у мене є ось це як `FakeLogger<T>` Тоді я зможу отримати доступ до `FakeLogCollector` і `FakeLogRecord` щоб отримати журнали і перевірити їх.

Тоді я можу просто перевірити, чи в журналі є правильні повідомлення.

# Включення

Отже, ви маєте це, простий спосіб перевірити журнал повідомлень під час тестів з підрозділів без моканської маячні.