# Перевірка модулів Umami.Net - тестування UmamiBackgroundSender

# Вступ

У попередній статті ми обговорили, як перевірити `UmamiClient` за допомогою xUnit і Moq. У цій статті ми обговоримо, як перевірити себе. `UmamiBackgroundSender` Клас. The `UmamiBackgroundSender` трохи відрізняється від `UmamiClient` як використовується `IHostedService` продовжити роботу у фоновому режимі і надіслати запити через `UmamiClient` повністю поза основною ниткою (так що вона не блокує виконання).

Як завжди, ви можете бачити всі початкові коди для цього на моєму GitHub [тут](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net.Test/UmamiBackgroundSender_Tests.cs).

[TOC]

<!--category-- xUnit, Umami, IHostedService, Moq -->
<datetime class="hidden">2024- 09- 03T09: 00</datetime>

## `UmamiBackgroundSender`

Поточна структура `UmamiBackgroundSender` досить просто. Це служба, яка надсилає запити на сервер Умамі, як тільки виявить новий запит. Основна структура `UmamiBackgroundSender` клас показано нижче:

```csharp
public class UmamiBackgroundSender(IServiceScopeFactory scopeFactory, ILogger<UmamiBackgroundSender> logger) : IHostedService
{

    private  Channel<SendBackgroundPayload> _channel = Channel.CreateUnbounded<SendBackgroundPayload>();

    private Task _sendTask = Task.CompletedTask;
    
        public Task StartAsync(CancellationToken cancellationToken)
    {

        _sendTask = SendRequest(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }
    
            public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("UmamiBackgroundSender is stopping.");

            // Signal cancellation and complete the channel
            await _cancellationTokenSource.CancelAsync();
            _channel.Writer.Complete();
            try
            {
                // Wait for the background task to complete processing any remaining items
                await Task.WhenAny(_sendTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("StopAsync operation was canceled.");
            }
        }
        
                private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload, type:payload.EventType);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }

    private record SendBackgroundPayload(string EventType, UmamiPayload Payload);
    
    }

```

Як бачите, це просто класика. `IHostedService` вона додається до нашої колекції послуг в ASP.NET, використовуючи `services.AddHostedService<UmamiBackgroundSender>()` метод. Це скидає `StartAsync` метод запуску програми.
Погляд всередині `SendRequest` метод - це місце, де відбувається диво. Тут ми читаємо з каналу і надсилаємо запит на сервер Умамі.

За допомогою цього пункту можна виключити справжні методи надсилання запитів (показано нижче).

```csharp
public async Task TrackPageView(string url, string title, UmamiPayload? payload =null, UmamiEventData? eventData = null)

public async Task Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)   

        public async Task IdentifySession(string sessionId, UmamiEventData? eventData = null)

public async Task Track(string eventName, UmamiEventData? eventData = null)

public async Task Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Все, що вони дійсно роблять це пакет запит вгору до `SendBackgroundPayload` Запишіть і відішліть його на канал.

У нашому гнізді петля отримується в `SendRequest` продовжуватиме читати з каналу, доки його не буде закрито. Саме тут ми сконцентруємо наші випробування.

```csharp
  while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
            }
        }    

```

У фоновому сервісі є певні семантики, за допомогою яких повідомлення можна вивільнити одразу ж після прибуття.
Тим не менш, це викликає проблему; якщо ми не отримуємо повернути значення з `Send` Як ми можемо перевірити, що це насправді робить щось?

## Перевірка `UmamiBackgroundSender`

Отже, питання в тому, як ми тестуємо цю службу п'ять тисяч і немає відповіді на випробування?

Відповідь - ввести `HttpMessageHandler` до глузливого HttpClient, якого ми відправляємо у наш Умамілієнт. Це дозволить нам перехопити запит і перевірити його зміст.

### EchoMockHttpMessageHandler

Ви пам'ятаєте з попередньої статті, ми створили глузливий HtpMessageHandler. Це живе всередині `EchoMockHandler` Статичний клас:

```csharp
public static class EchoMockHandler
{
    public static HttpMessageHandler Create(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFunc)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                responseFunc(request, cancellationToken).Result);

        return mockHandler.Object;
    }
```

Тут видно, що ми використовуємо Mock, щоб встановити `SendAsync` метод, який поверне відповідь на основі запиту (у HtpClient всі запити буде виконано через `SendAsync`).

Ви бачите, що ми вперше сконструювали Mock

```csharp
     var mockHandler = new Mock<HttpMessageHandler>();
```

Потім ми використовуємо магію `Protected` для налаштування `SendAsync` метод. Це тому, що `SendAsync` не є, зазвичай, доступним у громадському API `HttpMessageHandler`.

```csharp
public abstract class HttpMessageHandler : IDisposable
    {
        protected HttpMessageHandler()
        {
        }
        protected internal abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
```

Тогда мы используем все похищение. `ItExpr.IsAny` пасує до будь- якого запиту і повертає відповідь від `responseFunc` ми входимо.

## Методи перевірки.

Всередині `UmamiBackgroundSender_Tests` Клас у нас поширений спосіб визначення всіх методів перевірки.

### Налаштування

```csharp
[Fact]
    public async Task Track_Page_View()
    {
        var page = "https://background.com";
        var title = "Background Example Page";
        var tcs = new TaskCompletionSource<bool>();
        // Arrange
        var handler = EchoMockHandler.Create(async (message, token) =>
        {
            try
            {
                var responseContent = EchoMockHandler.ResponseHandler(message, token);
                var jsonContent = await responseContent.Result.Content.ReadFromJsonAsync<EchoedRequest>(token);
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                Assert.Contains("api/send", message.RequestUri.ToString());
                Assert.NotNull(jsonContent);
                Assert.Equal(page, jsonContent.Payload.Url);
                Assert.Equal(title, jsonContent.Payload.Title);
                // Signal completion
                tcs.SetResult(true);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        });

        var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

Як тільки ми маємо це визначення, ми маємо керувати нашим `IHostedService` Тривалість у методі перевірки:

```csharp
       var (backgroundSender, hostedService) = GetServices(handler);
        var cancellationToken = new CancellationToken();
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
    }
```

Ви можете бачити, що ми переходимо до нашого куратора `GetServices` Метод налаштування:

```csharp
    private (UmamiBackgroundSender, IHostedService) GetServices(HttpMessageHandler handler)
    {
        var services = SetupExtensions.SetupServiceCollection(handler: handler);
        services.AddScoped<UmamiBackgroundSender>();
       

        services.AddScoped<IHostedService, UmamiBackgroundSender>(provider =>
            provider.GetRequiredService<UmamiBackgroundSender>());
        SetupExtensions.SetupUmamiClient(services);
        var serviceProvider = services.BuildServiceProvider();
        var backgroundSender = serviceProvider.GetRequiredService<UmamiBackgroundSender>();
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        return (backgroundSender, hostedService);
    }
```

Тут ми передаємо нашому куратору послуги, щоб зв'язати його в `UmamiClient` Заряджай.

Потім ми додаємо `UmamiBackgroundSender` до збірки служб і отримання `IHostedService` від постачальника послуг. Потім поверніть це до класу тесту, щоб дозволити йому використовувати його.

#### Життєдайне служіння

Тепер, коли ми маємо все це, ми можемо просто `StartAsync` Служба вузла, скористайтеся нею, а потім зачекайте доки вона припиниться:

```csharp
        await hostedService.StartAsync(cancellationToken);
        await backgroundSender.TrackPageView(page, title);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000, cancellationToken));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("The background task did not complete in time.");
        }
        
        await tcs.Task;
        await backgroundSender.StopAsync(CancellationToken.None);
```

Це запустить службу, відішле прохання і чекатиме на відповідь, а потім зупинить службу.

### Обробник повідомлень

Спочатку ми створюємо `EchoMockHandler` і `TaskCompletionSource` що дасть сигнал тесту завершено. Це важливо, щоб повернути контекст до основної тестової гілки, щоб ми могли правильно сприймати помилки і час очікування.

The ` async (message, token) => {}` це функція, яку ми передаємо нашому глузливому куратору, про яку ми згадували вище. Тут ми можемо перевірити запит і повернути відповідь (як у даному випадку ми насправді нічого не робимо).

Наш `EchoMockHandler.ResponseHandler` є допоміжним методом, який поверне тіло запиту назад до нашого методу, це дає нам змогу перевірити, що повідомлення передається через `UmamiClient` до `HttpClient` Правильно.

```csharp
    public static async Task<HttpResponseMessage> ResponseHandler(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Read the request content
        var requestBody = request.Content?.ReadAsStringAsync(cancellationToken).Result;
        // Create a response that echoes the request body
        var responseContent = requestBody ?? "No request body";
        // Return the response
        return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        });
    }
```

Потім ми беремо цю реакцію і знижуємо її в `EchoedRequest` об'єкт. Це простий об' єкт, який відповідає запиту, який ми надіслали на сервер.

```csharp
public class EchoedRequest
{
    public string Type { get; set; }
    public UmamiPayload Payload { get; set; }
}
```

Ви бачите, що це перекреслює `Type` і `Payload` прохання. Це те, проти чого ми будемо перевіряти у нашому тесті.

```csharp
      Assert.Contains("api/send", message.RequestUri.ToString());
      Assert.NotNull(jsonContent);
      Assert.Equal(page, jsonContent.Payload.Url);
      Assert.Equal(title, jsonContent.Payload.Title);
```

Важливим тут є те, як ми справляємося з помилками, тому що ми не в контексті основної нитки тут ми повинні використовувати `TaskCompletionSource` щоб повернути дані до основної гілки, які не вдалося перевірити.

```csharp
     catch (Exception e)
            {
                
                tcs.SetException(e);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
```

За допомогою цього пункту можна встановити виняток `TaskCompletionSource` і повернути до тесту 500 помилок.

# Включення

Це перший з моїх більш детальних дописів. `IHostedService` Запропонує це, як це досить складно перевірити, коли, як тут, він не повертає значення тому, хто дзвонить.