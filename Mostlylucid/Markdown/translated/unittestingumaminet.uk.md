# Перевірка модулів Umami.Net - перевірка UmamiClient

# Вступ

Тепер у мене є [Пакунок amamami.Net](https://www.nuget.org/packages/Umami.Net/) Я, звичайно, хочу запевнити, що все працює, як очікувалося. Щоб зробити це, то найкраще було б трохи докладно випробувати всі способи й класи. Саме тут з'являється тест на одиниці.
Зауваження: це не "досконалий підхід" типу повідомлення, це просто те, як я це зробив на даний момент. В дійсності мені не потрібно глузувати `IHttpMessageHandler` Тут ви можете напасти на DelagingMessageHandler, щоб зробити це звичайним HttpClient. Я лише хотів показати, як це можна зробити за допомогою мока.

[TOC]

<!--category-- xUnit, Umami -->
<datetime class="hidden">2024- 09- 01T17: 22</datetime>

# Перевірка одиниць

Одиничне тестування стосується процесу перевірки окремих одиниць коду, щоб переконатися, що вони працюють, як очікувалося. Це можна зробити, написавши тести, що викликають методи і класи у контрольований спосіб, а потім перевіряється вихід, як і очікувалося.

Для пакунка на зразок Umami.Net це так складно, як те, що обидва називають віддаленим клієнтом через `HttpClient` і має `IHostedService` вона робить надсилання нових даних подій якомога безперешкодно.

## Перевірка UmamiClient

Основна частина тестування `HttpClient` базована бібліотека уникає виклику " HttpClient." Це робиться шляхом створення a `HttpClient` який використовує a `HttpMessageHandler` що повертає відому відповідь. Це робиться шляхом створення a `HttpClient` з a `HttpMessageHandler` що повертає відому відповідь. У цьому випадку я просто відлую вхідні відповіді і перевіряю, що це не було спотворено `UmamiClient`.

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

Як ви побачите, це встановлюється `Mock<HttpMessageHandler>` Потім я входжу в `UmamiClient`.
У цьому коді я підключаю це до нашої `IServiceCollection` метод налаштування. Це додає всі служби, потрібні для `UmamiClient` включаючи наш новий `HttpMessageHandler` а потім повертає `IServiceCollection` для використання в тестах.

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

Щоб використати це і ввести його в `UmamiClient` Потім я використовую ці послуги в `UmamiClient` Заряджай.

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

Ви побачите, що я маю низку альтернативних параметрів, які дають мені змогу ввести різні параметри для різних типів тестів.

### Тести

Тепер я маю всі ці налаштування на місці, я можу почати писати тести для `UmamiClient` методи.

#### Надіслати

Все це означає, що наші тести можуть бути досить простими

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

Тут ви бачите найпростіший тест, просто гарантуючи, що `UmamiClient` може надіслати повідомлення і отримати відповідь; важливо, що ми також тестуємо для винятку випадок, де `type` неправильно. Це часто не звертає уваги на частину тестування, забезпечуючи, що код зазнає невдачі, як і очікувалося.

#### Перегляд сторінок

Щоб перевірити наш спосіб перегляду сторінок, ми можемо зробити щось подібне. У коді нижче я використовую мій `EchoHttpHandler` щоб відобразити відіслану відповідь і переконатися, що вона відсилає назад те, чого я очікую.

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

This using the `HttpContextAccessor` встановити шлях до `/testpath` а потім перевіряє, що `UmamiClient` правильно надсилає це.

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

Це важливо для коду нашого клієнта Умамі, оскільки більшість даних, надісланих з кожного запиту, фактично, динамічно створюється з `HttpContext` об'єкт. Отже, ми не можемо відправити нічого в `await umamiClient.TrackPageView();` виклик, і він все ще надсилатиме правильні дані витягуванням Url з `HttpContext`.

Як ми побачимо пізніше, це також важливо, щоб отримати такі речі, як `UserAgent` і `IPAddress` оскільки ці сервери використовуються для стеження за даними і переглядами " доріжки " без кук.

Для того, щоб мати це передбачуване ми визначаємо купу конст в `Consts` Клас. Таким чином, ми можемо протестувати проти передбачуваних відповідей та запитів.

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

## Подальше тестування

Это только начало нашей стратегии проверки для Умами. Мы все еще должны проверить `IHostedService` і тест проти фактичної генерації даних Умамі (який ніде не документується, але містить ключ JWT з деякими корисними даними).

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

Отже, ми хочемо перевірити це, імітувати ключ і, можливо, повернути дані кожного візиту (так як ви пам'ятаєте, це зроблено з a `uuid(websiteId,ipaddress, useragent)`).

# Включення

Це тільки початок тестування пакунка Umami.Net, є ще багато роботи, але це хороший початок. Я додам більше тестів і, без сумніву, покращуватиму їх.