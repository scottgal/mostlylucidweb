# Seq für ASP.NET Logging - Tracing mit SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Einleitung

Im vorherigen Teil habe ich Ihnen gezeigt, wie man [Selbst-Hosting für Seq mit ASP.NET Core ](/blog/selfhostingseq)......................................................................................................... Jetzt, da wir es eingerichtet haben, ist es an der Zeit, mehr seiner Funktionen zu verwenden, um eine vollständigere Protokollierung und Nachverfolgung mit unserer neuen Seq-Instanz zu ermöglichen.

[TOC]

# Aufspüren

Tracing ist wie logging++ es gibt Ihnen eine zusätzliche Ebene von Informationen über das, was in Ihrer Anwendung geschieht. Es ist besonders nützlich, wenn Sie ein verteiltes System haben und Sie eine Anfrage durch mehrere Dienste verfolgen müssen.
In dieser Seite bin ich mit ihm, um Probleme schnell aufzuspüren; nur weil dies ein Hobby-Website bedeutet nicht, dass ich meine professionellen Standards aufgeben.

## Einrichtung von Serilog

Das Einrichten von Tracing mit Serilog ist wirklich ziemlich einfach mit dem [Serilog Tracing](https://github.com/serilog-tracing/serilog-tracing) Paket. Zuerst müssen Sie die Pakete installieren:

Hier fügen wir auch die Konsole Spüle und die Seq Spüle hinzu

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Konsole ist immer nützlich zum Debuggen und Seq ist das, wofür wir hier sind. Seq verfügt auch über eine Reihe von 'Enrichers', die zusätzliche Informationen zu Ihren Protokollen hinzufügen können.

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

Um diese Anreicherer zu verwenden, müssen Sie sie zu Ihrem hinzufügen `Serilog` Konfiguration in Ihrem `appsettings.json` .............................................................................................................................. Sie müssen auch alle separaten Anreicherer mit Nuget installieren.

Es ist eines der guten und schlechten Dinge über Serilog, Sie beenden die Installation einer BUNCH Pakete; aber es bedeutet, dass Sie nur hinzufügen, was Sie brauchen und nicht nur ein monolithisches Paket.
Hier ist meins.

![Serilog Enrichers](serilogenrichers.png)

Mit all diesen Bomben bekomme ich eine ziemlich gute Log-Ausgabe in Seq.

![Serilog Seq Fehler](serilogerror.png)

Hier sehen Sie die Fehlermeldung, die Stack-Trace, die Thread-ID, die Prozess-ID und den Prozessnamen. Dies ist alles nützliche Informationen, wenn Sie versuchen, ein Problem aufzuspüren.

Eine Sache zu beachten ist, dass ich die `  "MinimumLevel": "Warning",` in meinem `appsettings.json` .............................................................................................................................. Das bedeutet, dass nur Warnungen und darüber bei Seq protokolliert werden. Dies ist nützlich, um das Geräusch unten in Ihren Protokollen zu halten.

In Seq können Sie dies jedoch auch per Api Key angeben; so können Sie `Information` (oder wenn Sie wirklich begeistert sind `Debug`) Protokollieren Sie hier und begrenzen Sie, was Seq tatsächlich durch API-Schlüssel erfasst.

![Seq-Api-Schlüssel](apikey.png)

Hinweis: Sie haben immer noch App Overhead, Sie können dies auch dynamischer machen, so dass Sie die Ebene auf der Flucht anpassen können). Siehe [Seq-Spülbecken ](https://github.com/datalust/serilog-sinks-seq)für weitere Details.

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

## Aufspüren

Jetzt fügen wir Tracing hinzu, wieder mit SerilogTracing ist es ziemlich einfach. Wir haben das gleiche Setup wie vorher, aber wir fügen ein neues Waschbecken für die Nachverfolgung hinzu.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Wir fügen auch ein zusätzliches Paket hinzu, um detailliertere aspnet-Kerninformationen zu protokollieren.

### Einrichtung `Program.cs`

Jetzt können wir mit der Nachverfolgung anfangen. Zunächst müssen wir die Rückverfolgung zu unserem `Program.cs` ..............................................................................................................................

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Tracing verwendet das Konzept der 'Aktivitäten', die eine Einheit der Arbeit darstellen. Sie können eine Aktivität starten, etwas Arbeit machen und sie dann stoppen. Dies ist nützlich für die Verfolgung einer Anfrage durch mehrere Dienste.

In diesem Fall fügen wir zusätzliche Tracings für HttpClient-Anfragen und AspNetCore-Anfragen hinzu. Wir fügen auch eine `TraceToSharedLogger` die die Aktivität auf den gleichen Logger wie der Rest unserer Anwendung protokollieren wird.

## Tracing in einem Dienst nutzen

Jetzt haben wir Rückverfolgung eingerichtet, die wir mit der Verwendung in unserer Anwendung beginnen können. Hier ist ein Beispiel für einen Dienst, der Tracing verwendet.

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

Die wichtigsten Linien sind hier:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Dies beginnt mit einer neuen 'Aktivität', die eine Arbeitseinheit ist. Es ist nützlich, um eine Anfrage durch mehrere Dienste zu verfolgen.
Da wir es in eine Verwendung Aussage eingewickelt haben, wird dies am Ende unserer Methode vollständig und verfügen, aber es ist gute Praxis, sie explizit zu vervollständigen.

```csharp
            activity.Complete();
```

In unserer Ausnahme Handhabung fangen wir auch die Aktivität, aber mit einer Fehlerstufe und die Ausnahme. Dies ist nützlich für das Aufspüren von Problemen in Ihrer Anwendung.

## Spuren verwenden

Jetzt haben wir all das Setup, mit dem wir anfangen können. Hier ist ein Beispiel für eine Spur in meiner Anwendung.

![Http Trace](httptrace.png)

Dies zeigt Ihnen die Übersetzung eines einzelnen Markdown-Post. Sie können die verschiedenen Schritte für einen einzelnen Beitrag und alle HttpClient Anfragen und Timings sehen.

Hinweis Ich verwende Postgres für meine Datenbank, im Gegensatz zu SQL-Server hat der npgsql-Treiber native Unterstützung für die Nachverfolgung, so dass Sie sehr nützliche Daten aus Ihren Datenbankanfragen wie SQL ausgeführt, Timings etc. erhalten können. Diese werden als 'Spans' in Seq gespeichert und sehen wie folgt aus:

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

Sie können sehen, dass dies so ziemlich alles beinhaltet, was Sie über die Abfrage, die SQL ausgeführt, den Verbindungsstring etc. wissen müssen. Dies ist alles nützliche Informationen, wenn Sie versuchen, ein Problem aufzuspüren. In einer kleineren App wie dieser ist dies nur interessant, in einer verteilten Anwendung ist es solide Goldinformationen, um Probleme aufzuspüren.

# Schlussfolgerung

Ich habe nur die Oberfläche von Tracing hier gekratzt, es ist ein bisschen Bereich mit leidenschaftlichen Befürwortern. Hoffentlich habe ich gezeigt, wie einfach es ist, mit dem einfachen Tracing mit Seq & Serilog für ASP.NET Core-Anwendungen loszukommen. Auf diese Weise kann ich viel von den Vorteilen von leistungsfähigeren Tools wie Application Insights ohne die Kosten von Azure bekommen (diese Dinge können ausgebeutet werden, wenn die Protokolle groß sind).