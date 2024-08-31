# Seq для ведення журналу ASP. NET - тема з серилогастрагією

<datetime class="hidden">2024- 08- 31T11: 20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Вступ

В попередній частині я показав вам, як налаштувати [Самопідтримка Seq за допомогою ядра ASP.NET ](/blog/selfhostingseq). Тепер, коли ми вже встановили час, щоб використати більше можливостей, щоб дати змогу проводити ведення лісозаготівель за допомогою нашого нового екземпляра Seq.

[TOC]

# Трасування

Трасування - це як ведення лісозаготівель, це дає вам додатковий шар інформації про те, що відбувається у вашій програмі. Це особливо корисно, коли у вас розподілена система і вам потрібно відстежити запит через декілька служб.
На цьому сайті я використовую його, щоб швидко відстежити проблеми; тільки тому, що це сайт хобі не означає, що я відмовляюся від професійних стандартів.

## Налаштування серілога

Налаштування слідкування за серілогом дуже просте за допомогою [Серіологічне трасування](https://github.com/serilog-tracing/serilog-tracing) пакунок. Спочатку вам слід встановити пакунки:

Тут ми також додаємо Консольну раковину і сикку.

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Консоль завжди корисна для зневаджування, а Сек - для нас. Крім того, в сеці є декілька " багатіїв," які можуть додати додаткову інформацію до ваших журналів.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

Щоб збагачувати людей, то треба додати їх до вашого дому. `Serilog` налаштування у вашій системі `appsettings.json` файл. Крім того, вам слід встановити всі накопичувачі за допомогою Nuget.

Це одна з добрих і поганих речей у серілогі, ви встановлюєте BUNCH пакунків; але це означає, що ви додаєте лише те, що вам потрібно, а не лише один моноліт.
Ось мій.

![Серіологічні багачі](serilogenrichers.png)

З усіма цими бомбами я отримую досить гарну інформацію в Сеці.

![Помилка у серилоговому сикці](serilogerror.png)

Тут ви бачите повідомлення про помилку, траєкторію стека, ідентифікатор гілки, ідентифікатор процесу і назву процесу. Це все корисна інформація, коли ви намагаєтеся відшукати проблему.

Одна річ, яку треба відмітити, це те, що я встановив `  "MinimumLevel": "Warning",` в моєму `appsettings.json` файл. Це означає, що лише попередження, вище, буде зареєстровано до Seq. За допомогою цього пункту можна зменшити рівень шуму у ваших журналах.

Але у Seq ви також можете вказати цей параметр за ключем Api; отже, ви можете вказати `Information` (або якщо ви дійсно захоплені `Debug`) Тут встановлено журналювання і обмеження того, що Сек насправді захоплює ключ API.

![Ключ Seq Api](apikey.png)

Зауваження: ви все ще маєте над головою програму, ви також можете зробити її динамічнішою, щоб ви могли налаштувати рівень на льоту). Видите [Сектонеsudan. kgm ](https://github.com/datalust/serilog-sinks-seq)щоб дізнатися більше.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## Трасування

Тепер ми додаємо Трайсінг, знову ж таки, використовуючи серіолог-кратизацію, це доволі просто. У нас така ж система, як і раніше, але ми додаємо нову раковину для слідкування.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Крім того, ми додаємо додатковий пакунок, щоб записувати докладнішу інформацію про ядро Aspnet.

### Налаштувати в `Program.cs`

Тепер ми можемо почати використовувати сліди. Спочатку нам потрібно додати слід `Program.cs` файл.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Трайс використовує концепцію "активності," яка представляє одиницю роботи. Ви можете розпочати якусь діяльність, трохи попрацювати, а потім припинити її. Це корисно для стеження за запитом через декілька служб.

У нашому випадку ми додаємо додаткове слідкування до запитів HttpClient і запитів AspNetCore. Ми також додаємо `TraceToSharedLogger` який вестиме журнал дій до того самого інструменту входу до системи, що і до решти нашої програми.

## Використовуй доручене служіння

Тепер ми маємо відстеження, ми можемо почати використовувати його в нашому застосуванні. Ось приклад служби, яка використовує слідкування.

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

Важливими лініями тут є:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Це розпочинає нову "активність," яка є одиницею роботи. Корисно відстежувати запит через декілька служб.
Так як ми загорнули його в використання інструкції це буде завершувати і позбуватися в кінці нашого методу, але це гарна практика, щоб явно завершити його.

```csharp
            activity.Complete();
```

У нашому винятковому процесі роботи ми також завершили роботу, але на рівні помилок та винятку. Це корисно для стеження за проблемами у вашій програмі.

## Використання слідів

Тепер у нас є вся ця конфігурація, яку ми можемо почати використовувати. Ось приклад сліду в моїй заяві.

![Http trace](httptrace.png)

Тут показано переклад одного допису. Ви можете побачити декілька кроків для одного допису і всіх запитів HtpClient і часових відліків.

Зауважте, що я використовую Postgres для моєї бази даних, на відміну від сервера SQL, драйвер npgsql має вбудовану підтримку для стеження, отже, ви можете отримувати дуже корисні дані від запитів до вашої бази даних, зокрема виконаної SQL, часові дані тощо. Вони врятовані як "пани" Секу і визирають:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

Ви можете бачити, що це включає майже все, що вам потрібно знати про запит, виконання SQL, рядок з' єднання тощо. Це все корисна інформація, коли ви намагаєтеся відшукати проблему. У меншій програмі, як це, це просто цікаво, у розподіленій програмі це тверда золота інформація для пошуку проблем.

# Включення

Я тільки почухав поверхню Траверсу тут, це трохи територія з пристрасними захисниками. Сподіваюся, я показував, наскільки просто слідувати за допомогою Secq і Serilog для програм ядра ASP. NET. Таким чином я можу отримати більшу користь від таких потужніших інструментів, як " Проникливі енциклопедії," не коштуючи Азуру (це може бути марною справою, коли колоди великі).