# Додавання клієнта стеження за Amami

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 08- 27T02: 00</datetime>

# Вступ

В [Попередній допис](/blog/addingumamitrackingclient.md) Я нарисовал, как клиент наблюдения для Умами в "С #" может сработать.
Ну, наконец-то, у меня была возможность проверить и улучшить его операцию. `IHostedService`).

[TOC]

# Kirks of the API Amami

API стеження за Амамі - це як дуже самовпевнене, так і дуже непросте. Отже, мені довелося оновити код клієнта, щоб працювати з такими кодами:

1. API очікує на рядок " true " User- Agent. Отже, мені довелося оновити клієнтську програму для того, щоб скористатися справжнім рядком User- Agent (або, щоб точніше сказати, я захопив рядок справжнього User- Agent з переглядача і використав його).
2. API очікує, що це ввід JSON у дуже специфічному форматі; порожні рядки не дозволені. Так что мне пришлось обновить клиента, чтобы справиться с этим.
3. The [Клієнт API вузлів](https://github.com/umami-software/node) має дещо дивну площу поверхні. Не відразу зрозуміло, чого очікує API. Тому мені довелося зробити невеличку спробу і помилку, щоб це спрацювало.

## Клієнт API вузлів

Загалом, клієнт API вузла є нижче, він дуже гнучкий, але насправді не дуже добре документований.

```javascript
export interface UmamiOptions {
  hostUrl?: string;
  websiteId?: string;
  sessionId?: string;
  userAgent?: string;
}

export interface UmamiPayload {
  website: string;
  session?: string;
  hostname?: string;
  language?: string;
  referrer?: string;
  screen?: string;
  title?: string;
  url?: string;
  name?: string;
  data?: {
    [key: string]: string | number | Date;
  };
}

export interface UmamiEventData {
  [key: string]: string | number | Date;
}

export class Umami {
  options: UmamiOptions;
  properties: object;

  constructor(options: UmamiOptions = {}) {
    this.options = options;
    this.properties = {};
  }

  init(options: UmamiOptions) {
    this.options = { ...this.options, ...options };
  }

  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }

  track(event: object | string, eventData?: UmamiEventData) {
    const type = typeof event;
    const { websiteId } = this.options;

    switch (type) {
      case 'string':
        return this.send({
          website: websiteId,
          name: event as string,
          data: eventData,
        });
      case 'object':
        return this.send({ website: websiteId, ...(event as UmamiPayload) });
    }

    return Promise.reject('Invalid payload.');
  }

  identify(properties: object = {}) {
    this.properties = { ...this.properties, ...properties };
    const { websiteId, sessionId } = this.options;

    return this.send(
      { website: websiteId, session: sessionId, data: { ...this.properties } },
      'identify',
    );
  }

  reset() {
    this.properties = {};
  }
}

const umami = new Umami();

export default umami;
```

Як ви бачите, вона викриває такі методи:

1. `init` - Встановить варианты.
2. `send` - Прислати вантаж.
3. `track` - Щоб відстежити подію.
4. `identify` - Ідентифікувати користувача.
5. `reset` - Перезапустити властивості.

Суть цього - `send` метод, який надсилає вантаж до API.

```javascript
  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }
```

# Клієнт C#

Для початку я скопіював клієнти API вузла `UmamiOptions` і `UmamiPayload` Класи (я більше не пройду повз них, вони великі).

Отже, тепер моя `Send` метод виглядає так:

```csharp
     public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
            var websiteId = settings.WebsiteId;
             payload = PopulateFromPayload(websiteId, payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

```

Тут є дві критичні частини:

1. The `PopulateFromPayload` метод, який заповнить вантаж ідентифікатором веб- сайта і даними про подію.
2. JSON Серіалізація вантажу, вона повинна виключити нульові значення.

## The `PopulateFromPayload` Метод

```csharp
        public static UmamiPayload PopulateFromPayload(string webSite, UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(webSite, data: data);
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            return newPayload;          
        }
        
        private static UmamiPayload GetPayload(string websiteId, string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
            Website = websiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

```

Ви можете бачити, що ми завжди гарантуємо `websiteId` є множиною і ми встановлюємо інші значення, лише якщо вони не є нульовими. Це дає нам гнучкість за рахунок трохи дієслова.

## Налаштування HttpClient

Як ми вже згадували раніше, нам потрібно надати дещо реальний рядок User-Agent для API. Це робиться в `HttpClient` Заряджай.

```csharp
              services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

```

## Служба тла

Це ще один. `IHostedService`, є купа статей про те, як встановити їх, щоб я не пішов сюди (спробуйте пошукову панель!).

Єдиний болезаспокійливий момент - це використання ін'єкції. `HttpClient` в `UmamiClient` Клас. Через копіювання клієнта і служби, яку я використовував `IServiceScopeFactory` Ввели в конструктора HooredService, а потім забрали його для кожного запрошення.

```csharp
    

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
                    await client.Send(payload.Payload);

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
   
```

### Користування службами, які обслуговують

Тепер, коли у нас є послуга, ми можемо значно покращити продуктивність, надіславши події на задній план.

Я використав це в кількох різних місцях, в моїх `Program.cs` Я решил поекспериментировать с отслеживанием просьбы RSS, используя "Межную программу," она просто выявляет любую традицию, которая заканчивается в "RSS," и отправляет экземплярную мерацию.

```csharp
app.Use( async (context, next) =>
{
var path = context.Request.Path.Value;
if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
{
var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
// Send the event in the background
await rss.SendBackground(new UmamiPayload(){Url  = path, Name = "RSS Feed"});
}
await next();
});
```

Я також перелічив більше даних від моїх `TranslateAPI` Кінцева точка.
Це дає мені змогу бачити, скільки часу проходять переклади. Зауважте, що жоден з цих перекладів не блокує головну нитку АБО слідкування за окремими користувачами.

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# Включення

API Umami є трохи дивним, але це чудовий спосіб відстежувати події самостійно. Сподіваюся, я матиму шанс ще більше прибрати і дістати пакет "Нугета Умамі."
Крім того, [Попередня стаття](/blog/addingascsharpclientforumamiapi)  Я хочу забрати дані з Умамі, щоб надати такі особливості, як популярне сортування.