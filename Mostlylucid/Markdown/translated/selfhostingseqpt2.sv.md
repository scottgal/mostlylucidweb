# Seq för ASP.NET Loggning - Spårning med SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Inledning

I förra delen visade jag dig hur du skulle ställa in [själv hosting för Seq med hjälp av ASP.NET Core ](/blog/selfhostingseq)....................................... Nu när vi har det konfigurerat är det dags att använda fler av dess funktioner för att möjliggöra mer fullständig loggning och spårning med hjälp av vår nya Seq instans.

[TOC]

# Spårning

Spårning är som loggning++ det ger dig ett extra lager av information om vad som händer i din ansökan. Det är särskilt användbart när du har ett distribuerat system och du behöver för att spåra en begäran genom flera tjänster.
På denna webbplats använder jag det för att spåra upp frågor snabbt; bara för att detta är en hobby webbplats betyder inte att jag ger upp mina professionella standarder.

## Ställa in Serilog

Att sätta upp spårning med Serilog är verkligen ganska enkelt med hjälp av [Serilogspårning](https://github.com/serilog-tracing/serilog-tracing) paket. Först måste du installera paketen:

Här lägger vi också till konsolsänkan och Seq-sänkan

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Konsolen är alltid användbar för felsökning och Seq är vad vi är här för. Seq har också ett gäng "berikare" som kan lägga till extra information till dina loggar.

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

För att använda dessa berikare måste du lägga till dem till din `Serilog` inställning i din `appsettings.json` En akt. Du måste också installera alla separat berikare med Nuget.

Det är en av de bra och dåliga sakerna med Serilog, du hamnar installera en BUNCH av paket; men det betyder att du bara lägga till vad du behöver och inte bara ett monolitiskt paket.
Här är min.

![Serilogberikare](serilogenrichers.png)

Med alla dessa bombinerade får jag en ganska bra loggutgång i Seq.

![Fel vid Serilog Seq](serilogerror.png)

Här ser du felmeddelandet, stackspåret, tråd-ID, process-ID och processnamnet. Allt detta är användbar information när du försöker hitta ett problem.

En sak att notera är att jag har satt `  "MinimumLevel": "Warning",` i min `appsettings.json` En akt. Detta innebär att endast varningar och ovan kommer att loggas till Seq. Detta är användbart för att hålla ljudet nere i dina loggar.

Men i Seq kan du också ange detta per Api Key; så att du kan ha `Information` (eller om du verkligen är entusiastisk `Debug`) loggning här och begränsa vad Seq faktiskt fångar med API-nyckel.

![Seq Api- nyckel](apikey.png)

Observera: du har fortfarande app overhead, du kan också göra detta mer dynamiskt så att du kan justera nivån i farten). Se tabellen nedan. [Seq-sänka ](https://github.com/datalust/serilog-sinks-seq)för mer information.

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

## Spårning

Nu lägger vi till Tracing, igen använder SerilogTracing det är ganska enkelt. Vi har samma inställning som tidigare men vi lägger till en ny diskho för spårning.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Vi lägger också till ett extra paket för att logga mer detaljerad Aspnet kärninformation.

### Ställ in `Program.cs`

Nu kan vi börja använda spårningen. Först måste vi lägga till spårningen till vår `Program.cs` En akt.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Spårningen använder begreppet "verksamhet" som utgör en arbetsenhet. Du kan starta en aktivitet, göra lite arbete och sedan stoppa den. Detta är användbart för att spåra en begäran via flera tjänster.

I detta fall lägger vi till extra spårning för HttpClient-förfrågningar och AspNetCore-förfrågningar. Vi lägger också till en `TraceToSharedLogger` som kommer att logga aktiviteten till samma logger som resten av vår applikation.

## Att använda spårning i en tjänst

Nu har vi satt upp spårning som vi kan börja använda i vår ansökan. Här är ett exempel på en tjänst som använder spårning.

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

De viktigaste linjerna här är:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Detta startar en ny "aktivitet" som är en arbetsenhet. Det är användbart för att spåra en begäran genom flera tjänster.
Som vi har det insvept i en använda uttalande detta kommer att slutföra och kassera i slutet av vår metod men det är bra praxis att uttryckligen slutföra det.

```csharp
            activity.Complete();
```

I vårt undantag hantering fångst vi också slutföra verksamheten men med en felnivå och undantaget. Detta är användbart för att spåra problem i din ansökan.

## Använda spår

Nu har vi alla dessa inställningar som vi kan börja använda den. Här är ett exempel på ett spår i min ansökan.

![Spår av Http](httptrace.png)

Detta visar dig översättningen av en enda markdown inlägg. Du kan se flera steg för ett enda inlägg och alla HttpClient önskemål och tidpunkter.

Observera att jag använder Postgres för min databas, till skillnad från SQL-servern har npgsql-drivrutinen inbyggt stöd för spårning så att du kan få mycket användbar data från dina databasfrågor som SQL exekverade, tidpunkter etc. Dessa sparas som "pans" till Seq och ser ut som följande:

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

Du kan se detta inkluderar i stort sett allt du behöver veta om frågan, SQL som utförs, anslutningssträngen etc. Allt detta är användbar information när du försöker hitta ett problem. I en mindre app som denna är detta bara intressant, i ett distribuerat program är det solid guld information för att spåra problem.

# Slutsatser

Jag har bara skrapat på ytan av Tracing här, det är lite område med passionerade förespråkare. Förhoppningsvis har jag visat hur enkelt det är att komma igång med enkel spårning med Seq & Serilog för ASP.NET Core-applikationer. På så sätt kan jag få mycket av fördelarna med kraftfullare verktyg som Application Insights utan kostnaden för Azure (dessa saker kan bli slöa när loggarna är stora).