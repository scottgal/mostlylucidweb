# Seq voor ASP.NET Logging - Traceren met SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Inleiding

In het vorige deel heb ik je laten zien hoe je [self hosting voor Seq met behulp van ASP.NET Core ](/blog/selfhostingseq). Nu we het hebben ingesteld is het tijd om meer van zijn functies te gebruiken om meer volledige logging & traceren mogelijk te maken met behulp van onze nieuwe Seq instantie.

[TOC]

# Traceren

Traceren is als loggen++ het geeft je een extra laag van informatie over wat er gebeurt in uw toepassing. Het is vooral handig als je een gedistribueerd systeem hebt en je een verzoek moet traceren via meerdere diensten.
Op deze site gebruik ik het om problemen snel op te sporen; alleen omdat dit een hobby site is betekent niet dat ik mijn professionele normen opgeef.

## Serilog instellen

Het opzetten van traceren met Serilog is echt vrij eenvoudig met behulp van de [Serilog Traceren](https://github.com/serilog-tracing/serilog-tracing) Pakje. Eerst moet je de pakketten installeren:

Hier voegen we ook de Console gootsteen en de Seq gootsteen toe

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Console is altijd nuttig voor debuggen en Seq is waar we hier voor zijn. Seq beschikt ook over een aantal'verrijkers' die extra informatie aan uw logs kunnen toevoegen.

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

Om deze verrijkingen te gebruiken moet u ze toevoegen aan uw `Serilog` configuratie in uw `appsettings.json` bestand. Je moet ook alle aparte verrijkingen installeren met Nuget.

Het is een van de goede en slechte dingen over Serilog, je eindigt met het installeren van een BUNCH van pakketten; maar het betekent dat je alleen toe te voegen wat je nodig hebt en niet slechts een monolithisch pakket.
Hier is de mijne.

![Serilog Verrijkers](serilogenrichers.png)

Met al deze bomaanslagen krijg ik een vrij goede log output in Seq.

![Serilog Seq-fout](serilogerror.png)

Hier ziet u de foutmelding, de stack trace, de thread-id, het proces-id en de procesnaam. Dit is allemaal nuttige informatie als je probeert om een probleem op te sporen.

Een ding op te merken is dat ik de `  "MinimumLevel": "Warning",` in mijn `appsettings.json` bestand. Dit betekent dat alleen waarschuwingen en hoger worden ingelogd bij Seq. Dit is handig om het geluid in je logs te houden.

Echter in Seq kunt u dit ook per Api Key opgeven; dus u kunt `Information` (of als je echt enthousiast bent `Debug`) logging set here and limit what Seq actually captures by API key.

![Seq Api Key](apikey.png)

Let op: je hebt nog steeds app overhead, je kunt dit ook dynamischer maken zodat je het niveau op de vlieg kunt aanpassen). Zie de [Seq gootsteen ](https://github.com/datalust/serilog-sinks-seq)voor meer details.

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

## Traceren

Nu voegen we Traceren toe, opnieuw gebruik makend van SerilogTracing is het vrij eenvoudig. We hebben dezelfde setup als voorheen, maar we voegen een nieuwe gootsteen toe voor het traceren.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

We voegen ook een extra pakket toe om meer gedetailleerde aspnet kerninformatie in te loggen.

### Instellen in `Program.cs`

Nu kunnen we beginnen met het traceren. In de eerste plaats moeten we de tracing toevoegen aan onze `Program.cs` bestand.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Traceren maakt gebruik van het begrip 'Activiteiten' dat een eenheid van werk vertegenwoordigt. Je kunt een activiteit beginnen, wat werk doen en dan stoppen. Dit is handig voor het volgen van een verzoek via meerdere diensten.

In dit geval voegen we extra traceren toe voor HttpClient verzoeken en AspNetCore verzoeken. We voegen ook een `TraceToSharedLogger` die de activiteit zal loggen naar dezelfde logger als de rest van onze toepassing.

## Traceren in een service gebruiken

Nu hebben we opsporing opgezet kunnen we beginnen met het gebruik ervan in onze applicatie. Hier is een voorbeeld van een dienst die gebruik maakt van traceren.

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

De belangrijkste lijnen zijn hier:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Dit start een nieuwe 'activiteit' die een eenheid van werk is. Het is handig voor het volgen van een verzoek via meerdere diensten.
Zoals we het hebben verpakt in een gebruiksverklaring zal dit voltooien en verwijderen aan het einde van onze methode, maar het is een goede praktijk om het expliciet te voltooien.

```csharp
            activity.Complete();
```

In onze uitzonderingsafhandeling vangen we ook de activiteit maar met een foutniveau en de uitzondering. Dit is handig voor het opsporen van problemen in uw applicatie.

## Sporen gebruiken

Nu hebben we al deze setup die we kunnen gaan gebruiken. Hier is een voorbeeld van een spoor in mijn aanvraag.

![Http Trace](httptrace.png)

Dit toont u de vertaling van een enkele markdown post. U kunt de meerdere stappen voor een enkele post en alle HttpClient verzoeken en timings te zien.

Opmerking Ik gebruik Postgres voor mijn database, in tegenstelling tot SQL server de npgsql stuurprogramma heeft inheemse ondersteuning voor het traceren, zodat u zeer nuttige gegevens uit uw database vragen zoals de SQL uitgevoerd, timings etc. Deze worden opgeslagen als'spans' voor Seq en zien er als volgt uit:

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

Je kunt zien dat dit vrijwel alles omvat wat je moet weten over de query, de SQL uitgevoerd, de verbinding string etc. Dit is allemaal nuttige informatie als je probeert om een probleem op te sporen. In een kleinere app als deze is gewoon interessant, in een gedistribueerde toepassing is het solide goud informatie om problemen op te sporen.

# Conclusie

Ik heb alleen het oppervlak van Tracing gekraakt, het is een klein gebied met gepassioneerde advocaten. Hopelijk heb ik laten zien hoe eenvoudig het is om te gaan met eenvoudige traceren met behulp van Seq & Serilog voor ASP.NET Core toepassingen. Op deze manier kan ik veel van het voordeel van krachtigere tools zoals Application Insights zonder de kosten van Azure (deze dingen kunnen worden besteed wanneer de logs groot zijn).