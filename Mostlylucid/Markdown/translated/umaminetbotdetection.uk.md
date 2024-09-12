# Umami.Net і Bot Det Detection

# Вступ

Так що я [вивішував ЛІТ](/blog/category/Umami) У минулому стосовно використання Умамі для аналітики в самоутвердженому середовищі і навіть опублікували [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). Однако у меня была проблема, где я хотел отследить те, кто использует мой канал RSS, и в этом положении в том, почему и как я раскрыла его.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 09- 12T14: 50</datetime>

# Проблема

Проблема в тому, що читачі подачі RSS намагаються передати *придатний* Агенти користувача при запитанні на подачу. Це дозволяє **сумісний** Провайдери, які слідкують за кількістю користувачів і типом користувачів, які споживають подачу. Проте, це також означає, що Умамі буде ідентифікувати ці запити як *bot* просьби. Це спірне питання для мене, оскільки воно призводить до просьби, яку ігнорують, а не вистежують.

Агент користувача Fedbin виглядає так:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

Так досить корисно, це передає деякі корисні деталі про ваш ідентифікатор подачі, кількість користувачів і агент користувача. Тим не менш, це також проблема, як це означає, що Умамі буде ігнорувати прохання; насправді він поверне 200 статус, АЛЕ зміст містить `{"beep": "boop"}` Это значит, что это идентифицировано как просьбу бота. Це дратує, оскільки я не можу впоратися з цим за допомогою нормальної обробки помилок (це 200, не означає 403 тощо).

# Розв'язання

Яке ж вирішення цього питання? Я не можу вручну опрацювати всі ці запити і визначити, чи зможе Умамі виявити їх як боту; він використовує IsBot (https://wwww.npmjs.com/package/isbot), щоб визначити, чи є запит ботом, чи ні. Немає еквіваленту C# і це змінений список, тому я навіть не можу використовувати цей список (в майбутньому я можу бути кмітливим і використовувати цей список для визначення, чи є запит ботом, чи ні).
Поэтому мне нужно перехватить просьбу, пока она не доехала до Умами и не изменила агента User на то, что Умами примет для специальной просьбы.

Тепер я додав деякі додаткові параметри моїх методів стеження в Умамі.Net. За допомогою цих пунктів ви можете вказати новий " Типовий користувацький агент " буде надіслано до Умамі замість початкового User Agent. За допомогою цього пункту можна вказати, що агент користувача має бути змінено на певне значення для окремих запитів.

## Методи

На моєму `UmamiBackgroundSender` Я додав наступне:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

Цей параметр існує у всіх методах стеження і просто встановлює параметр `UmamiPayload` об'єкт.

Увімкнено `UmamiClient` Ви можете встановити такі значення:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

У цьому тесті я використовую новий `TrackPageViewAndDecode` метод, який повертає a `UmamiDataResponse` об'єкт. Цей об' єкт містить декодований ключ JWT (який є некоректним, якщо це бот, отже цей об' єкт корисний для перевірки) і стан запиту.

## `PayloadService`

Все оброблено `Payload` Служба, яка відповідає за розгортання об' єкта вантажу. Ось де `UseDefaultUserAgent` все готово.

Типово, я заповнив вантаж з `HttpContext` Так що, зазвичай, ви отримаєте цю множину правильно; пізніше я покажу, де вона відштовхується від Умамі.

```csharp
    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
            Hostname = request?.Host.Host
        };

        return payload;
    }
```

ТОН У мене є кодовий фрагмент під назвою `PopulateFromPayload` де об' єкт- запит отримує дані, встановлені:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```

Ви побачите, що це визначає нового агента користувача у верхній частині файла (який я підтвердив не є *зараз* виявлений як комп' ютер. Потім у методі він визначає чи є UserAgent нульовим (таких не повинно бути, якщо програму не буде викликано з коду без HtpContext) або якщо `UseDefaultUserAgent` все готово. Якщо буде позначено цей пункт, програма встановить значення UserAgent як типовий і додасть початковий параметр UserAgent до об' єкта даних.

Після цього запис буде зареєстровано, щоб ви могли побачити, що буде використано UserAgent.

## Декодувати відповідь.

В Умамі.Net 0. 3. 0 Я додав декілька нових методів " AndDecode," які повертає a `UmamiDataResponse` об'єкт. Цей об' єкт містить декодований ключ JWT.

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }
    
        private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }
```

Ви можете бачити, що це викликає в нормі `TrackPageView` тоді метод викликає метод `DecodeResponse` який перевіряє відповідь `beep` і `boop` рядки (для визначення боту). Якщо він знайде їх, то запише попередження і поверне a `BotDetected` Статус. Якщо їх не знайдуть, декодує ключ JWT і повертає вантаж.

Сам знак JWT - це просто кодований рядок Base64, який містить дані, які зберіг Умамі. Це декодовано і повернуто як `UmamiDataResponse` об'єкт.

Повне джерело для цього знаходиться нижче:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>
Ви можете бачити, що це містить купу корисної інформації про запит, який зберіг Умамі. Якщо ви бажаєте, наприклад, показати різний вміст на основі локалі, мови, переглядача тощо, ви можете це зробити.

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# Включення

Отже, короткий допис охоплює деякі нові функціональності в Умамі.Net 0.4.0, що дозволяє вам вказати типового агента користувача для конкретних запитів. Это подходящее для отслеживания просьбы, которые Амами в противном случае проигнорировал бы.