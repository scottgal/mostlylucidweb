# Seq per ASP.NET Logging - Tracciamento con SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Introduzione

Nella parte precedente vi ho mostrato come impostare [self hosting per Seq utilizzando ASP.NET Core ](/blog/selfhostingseq). Ora che lo abbiamo impostato è ora di utilizzare più delle sue funzionalità per consentire una registrazione più completa del & tracciamento usando la nostra nuova istanza Seq.

[TOC]

# Rintracciamento

Tracciare è come registrare ++ ti dà un ulteriore livello di informazioni su ciò che sta accadendo nella tua applicazione. E 'particolarmente utile quando si dispone di un sistema distribuito e è necessario rintracciare una richiesta attraverso più servizi.
In questo sito sto usando per rintracciare rapidamente i problemi; solo perché questo è un sito di hobby non significa che rinuncio ai miei standard professionali.

## Configurazione di Serilog

Impostare il tracciamento con Serilog è davvero abbastanza semplice utilizzando il [Serilog Tracciamento](https://github.com/serilog-tracing/serilog-tracing) pacco. Per prima cosa è necessario installare i pacchetti:

Qui aggiungiamo anche il lavello Console e il lavello Seq

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

La console è sempre utile per il debug e Seq è quello per cui siamo qui. Seq dispone anche di un gruppo di 'enricher' che possono aggiungere ulteriori informazioni ai tuoi log.

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

Per utilizzare questi arricchenti è necessario aggiungerli al vostro `Serilog` configurazione nella tua `appsettings.json` Archivio. È anche necessario installare tutti i separatori arricchenti utilizzando Nuget.

E 'una delle cose buone e cattive di Serilog, si finisce per installare un BUNCH di pacchetti; ma significa che si aggiunge solo quello che serve e non un solo pacchetto monolitico.
Ecco il mio.

![Serilog Enrichers](serilogenrichers.png)

Con tutte queste bombe ottengo una buona uscita di log in Seq.

![Errore di Serilog Seq](serilogerror.png)

Qui vedete il messaggio di errore, la traccia dello stack, l'id del thread, l'id del processo e il nome del processo. Queste sono tutte informazioni utili quando si sta cercando di rintracciare un problema.

Una cosa da notare è che ho impostato il `  "MinimumLevel": "Warning",` nel mio `appsettings.json` Archivio. Ciò significa che solo gli avvisi e sopra saranno registrati a Seq. Questo è utile per mantenere il rumore giù nei vostri registri.

Tuttavia in Seq puoi anche specificare questo per Api Key; così puoi avere `Information` (o se sei davvero entusiasta `Debug`) registrazione impostata qui e limitare ciò che Seq cattura effettivamente dalla chiave API.

![Seq Api Key](apikey.png)

Nota: hai ancora app overhead, è anche possibile rendere questo più dinamico in modo da poter regolare il livello al volo). Vedere il [Seq lavello ](https://github.com/datalust/serilog-sinks-seq)per ulteriori dettagli.

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

## Rintracciamento

Ora aggiungiamo Tracciamento, di nuovo usando SerilogTracing è abbastanza semplice. Abbiamo lo stesso setup di prima, ma aggiungiamo un nuovo lavello per il tracciamento.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Aggiungiamo anche un pacchetto aggiuntivo per registrare informazioni più dettagliate sul core dell'aspnet.

### Configurazione in `Program.cs`

Ora possiamo iniziare a usare il tracciamento. Prima dobbiamo aggiungere il tracciamento al nostro `Program.cs` Archivio.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Tracciare utilizza il concetto di 'Attività' che rappresentano un'unità di lavoro. Puoi iniziare un'attività, fare un po' di lavoro e poi fermarlo. Questo è utile per tracciare una richiesta attraverso più servizi.

In questo caso aggiungiamo tracce extra per le richieste HttpClient e AspNetCore. Aggiungiamo anche una `TraceToSharedLogger` che registrerà l'attività allo stesso logger del resto della nostra applicazione.

## Utilizzo di Tracciamento in un Servizio

Ora abbiamo impostato il tracciamento possiamo iniziare a usarlo nella nostra applicazione. Ecco un esempio di un servizio che utilizza il tracciamento.

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

Le linee importanti qui sono:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Inizia così una nuova 'attività' che è un'unità di lavoro. È utile per rintracciare una richiesta attraverso più servizi.
Come abbiamo avvolto in una dichiarazione di utilizzo questo completerà e disporre alla fine del nostro metodo, ma è buona pratica per completarlo esplicitamente.

```csharp
            activity.Complete();
```

Nella nostra gestione delle eccezioni completiamo anche l'attività ma con un livello di errore e l'eccezione. Questo è utile per rintracciare i problemi nella vostra applicazione.

## Uso di Traces

Ora abbiamo tutta questa configurazione che possiamo iniziare ad usarla. Ecco un esempio di una traccia nella mia domanda.

![Traccia Http](httptrace.png)

Questo mostra la traduzione di un singolo post di markdown. Puoi vedere i passaggi multipli per un singolo post e tutte le richieste e i tempi di HttpClient.

Si noti che uso Postgres per il mio database, a differenza del server SQL il driver npgsql ha il supporto nativo per il tracciamento in modo da poter ottenere dati molto utili dalle query del database come il SQL eseguito, tempi, ecc. Questi sono salvati come'spans' a Seq e guardare mentik il seguente:

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

Potete vedere questo include praticamente tutto quello che dovete sapere circa la query, il SQL eseguito, la stringa di connessione ecc. Queste sono tutte informazioni utili quando si sta cercando di rintracciare un problema. In un'applicazione più piccola come questa è interessante, in un'applicazione distribuita è l'oro solido informazioni per rintracciare i problemi.

# In conclusione

Ho solo graffiato la superficie di Tracciare qui, è un po' un'area con sostenitori appassionati. Speriamo di aver mostrato quanto sia semplice procedere con il semplice tracciamento usando Seq & Serilog per le applicazioni ASP.NET Core. In questo modo posso ottenere molto del beneficio di strumenti più potenti come Applique Insights senza il costo di Azure (queste cose possono essere spesi quando i tronchi sono grandi).