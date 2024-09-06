# Використання даних Umami для Stats веб- сайта

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 09- 05T23: 45</datetime>

# Вступ

Один з моїх проектів від початку цього блогу є майже одержиме бажання відстежити, скільки користувачів дивиться на мій сайт. Для цього я використовую Умамі і маю [BUNCH дописів](/blog/category/Umami) де ми користуємось і облаштовуємо Умамі. У мене також є пакунок Nuget, який дає можливість відстежувати дані з веб-сайту ядра ASP.NET.

Тепер я додав нову службу, яка дозволяє перевозити дані з Умамі на програму C#. Це проста служба, яка використовує API Umami для отримання даних з вашого екземпляра Umami і використання його на вашому веб-сайті / application.

Як зазвичай, можна знайти всі вихідні коди програми [на GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net) для цього сайту.

[TOC]

# Встановлення

Цей пункт вже знаходиться у пакунку Umami.Net Nuget, встановіть його за допомогою такої команди:

```bash
dotnet add package Umami.Net
```

Тогда тебе нужно устроить службу в твоем `Program.cs` файл:

```csharp
    services.SetupUmamiData(config);
```

This using the `Analytics' element from your `appsetds.json} file:

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee",
    "UmamiScript" : "getinfo",
   "UserName": "admin",
   "Password": ""
 }
```

Ось, будь ласка. `UmamiScript` Це сценарій, який ви використовуєте для стеження на стороні клієнта в Умамі ([ось тут.](/blog/usingumamiforlocalanalytics) як це налаштувати.
The `WebSiteId` Це ідентифікатор сайту, який ви створили у вашому екземплярі "Умамі."
`UmamiPath` - это путь к твоему подразделению Умами.

The `UserName` і `Password` є реєстраційними даними для екземпляра " Умамі " (у цьому випадку я використовую пароль адміністратора).

# Використання

Тепер у вас є `UmamiDataService` у вашій збірці послуг ви можете почати користуватися нею!

## Методи

Всі ці методи взято з визначення API Umami, про які ви можете прочитати у цьому підручнику:
https: // umami. is/ docs/api/ website- Stats

Всі повернені буде загорнуто у рядок `UmamiResults<T>` об' єкт, який має об' єкт `Success` властивість і a `Result` власність. The `Result` Властивість - це об'єкт, повернений з API Umami.

```csharp
public record UmamiResult<T>(HttpStatusCode Status, string Message, T? Data);
```

Всі запити, окрім `ActiveUsers` має об' єкт базового запиту з двома примусовими властивостями. Я додав час зручності до базового предмету, щоб було легше встановити дати початку і закінчення.

```csharp
public class BaseRequest
{
    [QueryStringParameter("startAt", isRequired: true)]
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    [QueryStringParameter("endAt", isRequired: true)]
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
```

У цій службі є такі способи:

### Активні користувачі

Це просто отримує загальну кількість активних користувачів CURRENT на сайті

```csharp
public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
```

### Стани

За допомогою цього пункту можна повернути декілька статистичних даних щодо сайта, зокрема кількість користувачів, перегляд сторінок тощо.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStats(StatsRequest statsRequest)    
```

Ви можете встановити декілька параметрів для фільтрування даних, які буде повернуто з API. Наприклад, використання `url` поверне статистику для певної адреси URL.

<details>
<summary>StatsRequest object</summary>
```csharp
public class StatsRequest : BaseRequest
{
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
Об'єкт JSON Amami повертається наступним чином.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

Це загорнуто всередині мого. `StatsResponseModel` об'єкт.

```csharp
namespace Umami.Net.UmamiData.Models.ResponseObjects;

public class StatsResponseModels
{
    public Pageviews pageviews { get; set; }
    public Visitors visitors { get; set; }
    public Visits visits { get; set; }
    public Bounces bounces { get; set; }
    public Totaltime totaltime { get; set; }


    public class Pageviews
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visitors
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Visits
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Bounces
    {
        public int value { get; set; }
        public int prev { get; set; }
    }

    public class Totaltime
    {
        public int value { get; set; }
        public int prev { get; set; }
    }
}
```

### Метрики

Метрики в Умамі надають вам кількість поглядів на специфічні властивості.

#### Події

Ось один приклад:

"Events" в Умамі це специфічні речі, які ви можете відстежити на сайті. Під час стеження за подіями за допомогою Umami.Net ви можете вказати декілька властивостей, за якими слідкує назва події. Наприклад, тут я слідкую `Search` запити з адресою URL і виразом пошуку.

```csharp
       await  umamiBackgroundSender.Track( "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Для отримання даних щодо цієї події вам слід скористатися " @ info: whatsthis `Metrics` метод:

```csharp
public async Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
```

Як і з іншими методами це приймає `MetricsRequest` об' єкт (з примусовим об' єктом) `BaseRequest` Властивості) і декілька необов' язкових властивостей для фільтрування даних.

<details>
<summary>MetricsRequest object</summary>
```csharp
public class MetricsRequest : BaseRequest
{
    [QueryStringParameter("type", isRequired: true)]
    public MetricType Type { get; set; } // Metrics type

    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    
    [QueryStringParameter("query")]
    public string? Query { get; set; } // Name of query
    
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
    
    [QueryStringParameter("language")]
    public string? Language { get; set; } // Name of language
    
    [QueryStringParameter("event")]
    public string? Event { get; set; } // Name of event
    
    [QueryStringParameter("limit")]
    public int? Limit { get; set; } = 500; // Number of events returned (default: 500)
}
```

</details>
Тут ви можете бачити, що ви можете вказати декілька властивостей у елементі запиту, щоб вказати вихідні дані, які ви бажаєте повернути.

Ви також можете встановити `Limit` властивість для обмеження кількості повернених результатів.

Наприклад, щоб отримати цю подію за попередній день, про який я згадував вище, ви можете скористатись таким проханням:

```csharp
var metricsRequest = new MetricsRequest
{
    StartAtDate = DateTime.Now.AddDays(-1),
    EndAtDate = DateTime.Now,
    Type = MetricType.@event,
    Event = "searchEvent"
};
```

Об' єкт JSON, повернений з API, такий:

```json
[
  { "x": "searchEvent", "y": 46 }
]
```

І знову я загорну це у свій `MetricsResponseModels` об'єкт.

```csharp
public class MetricsResponseModels
{
    public string x { get; set; }
    public int y { get; set; }
}
```

Де x - назва події, а y - кількість разів, які було викликано.

#### Перегляди сторінок

Одним з найкорисніших показників є кількість сторінок. Це кількість разів, коли на сайті було зображено сторінку. Нижче показана перевірка, яку я використовую, щоб отримати кількість переглядів сторінок за останні 30 днів. Ви помітите `Type` параметр встановлюється як `MetricType.url` Але це також типове значення, отже вам не потрібно його встановлювати.

```csharp
  [Fact]
    public async Task Metrics_StartEnd()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        
        var metrics = await websiteDataService.GetMetrics(new MetricsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-30),
            EndAtDate = DateTime.Now,
            Type = MetricType.url,
            Limit = 500
        });
        Assert.NotNull(metrics);
        Assert.Equal( HttpStatusCode.OK, metrics.Status);

    }
```

Це повертає a `MetricsResponse` об' єкт, який має структуру JSON:

```json
[
  {
    "x": "/",
    "y": 1
  },
  {
    "x": "/blog",
    "y": 1
  },
  {
    "x": "/blog/usingumamidataforwebsitestats",
    "y": 1
  }
]
```

Де `x` є адресою URL і `y` є кількість разів на яку дивились.

### PageViews

За допомогою цього пункту можна повернути кількість переглядів сторінок для певної адреси URL.

Знову ж таки, це тест, який я використовую для цього методу:

```csharp
    [Fact]
    public async Task PageViews_StartEnd_Day_Url()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();
    
        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest()
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.day,
            Url = "/blog"
        });
        Assert.NotNull(pageViews);
        Assert.Equal( HttpStatusCode.OK, pageViews.Status);

    }
```

Це повертає a `PageViewsResponse` об' єкт, який має структуру JSON:

```json
[
  {
    "date": "2024-09-06 00:00",
    "value": 1
  }
]
```

Де `date` є дата і `value` є кількістю переглядів сторінок, це значення повторюється для кожного дня у вказаному діапазоні (або годинах, місяця тощо). залежно від `Unit` властивість).

Як і з іншими методами це приймає `PageViewsRequest` об' єкт (з примусовим об' єктом) `BaseRequest` Властивості) і декілька необов' язкових властивостей для фільтрування даних.

<details>
<summary>PageViewsRequest object</summary>
```csharp
public class PageViewsRequest : BaseRequest
{
    // Required properties

    [QueryStringParameter("unit", isRequired: true)]
    public Unit Unit { get; set; } = Unit.day; // Time unit (year | month | hour | day)
    
    [QueryStringParameter("timezone")]
    [TimeZoneValidator]
    public string Timezone { get; set; }

    // Optional properties
    [QueryStringParameter("url")]
    public string? Url { get; set; } // Name of URL
    [QueryStringParameter("referrer")]
    public string? Referrer { get; set; } // Name of referrer
    [QueryStringParameter("title")]
    public string? Title { get; set; } // Name of page title
    [QueryStringParameter("host")]
    public string? Host { get; set; } // Name of hostname
    [QueryStringParameter("os")]
    public string? Os { get; set; } // Name of operating system
    [QueryStringParameter("browser")]
    public string? Browser { get; set; } // Name of browser
    [QueryStringParameter("device")]
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    [QueryStringParameter("country")]
    public string? Country { get; set; } // Name of country
    [QueryStringParameter("region")]
    public string? Region { get; set; } // Name of region/state/province
    [QueryStringParameter("city")]
    public string? City { get; set; } // Name of city
}
```

</details>
Так само, як і у випадку з іншими методами, ви можете встановити декілька властивостей для фільтрування даних, які буде повернуто з API, наприклад, ви можете встановити значення
`Country` властивість для отримання кількості переглядів сторінок з певної країни.

# Користування службою

На цьому сайті є код, який дозволяє мені використовувати цю службу, щоб отримати кількість переглядів, які мають кожна сторінка блогу. У коді нижче я беру початкову і кінцеву дату і префікс (яка є `/blog` у моєму випадку) і отримати кількість переглядів кожної сторінки в блозі.

Тогда я зарезервирую эти данные на час, так что мне не нужно продолжать в API Амами.

```csharp
public class UmamiDataSortService(
    UmamiDataService dataService,
    IMemoryCache cache)
{
    public async Task<List<MetricsResponseModels>?> GetMetrics(DateTime startAt, DateTime endAt, string prefix="" )
    {
        using var activity = Log.Logger.StartActivity("GetMetricsWithPrefix");
        try
        {
            var cacheKey = $"Metrics_{startAt}_{endAt}_{prefix}";
            if (cache.TryGetValue(cacheKey, out List<MetricsResponseModels>? metrics))
            {
                activity?.AddProperty("CacheHit", true);
                return metrics;
            }
            activity?.AddProperty("CacheHit", false);
            var metricsRequest = new MetricsRequest()
            {
                StartAtDate = startAt,
                EndAtDate = endAt,
                Type = MetricType.url,
                Limit = 500
            };
            var metricRequest = await dataService.GetMetrics(metricsRequest);

            if(metricRequest.Status != HttpStatusCode.OK)
            {
                return null;
            }
            var filteredMetrics = metricRequest.Data.Where(x => x.x.StartsWith(prefix)).ToList();
            cache.Set(cacheKey, filteredMetrics, TimeSpan.FromHours(1));
            activity?.AddProperty("MetricsCount", filteredMetrics?.Count()?? 0);
            activity?.Complete();
            return filteredMetrics;
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
         
            return null;
        }
    }

```

# Включення

Це проста служба, яка дозволяє добувати дані з Умамі і використовувати їх у своїй програмі. Я використовую це, щоб отримати кількість переглядів для кожної сторінки блогу і показати їх на сторінці. Але це дуже корисно для просто отримати БУНКЮ даних про те, хто використовує ваш сайт і як він використовується.