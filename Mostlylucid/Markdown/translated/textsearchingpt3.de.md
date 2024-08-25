# Volltextsuche (Pt 3 - OpenSearch mit ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Einleitung

In den vorherigen Teilen dieser Reihe haben wir das Konzept der Volltextsuche eingeführt und wie es verwendet werden kann, um Text innerhalb einer Datenbank zu suchen. In diesem Teil werden wir vorstellen, wie Sie OpenSearch mit ASP.NET Core verwenden.

Frühere Teile:

- [Volltextsuche mit Postgres](/blog/textsearchingpt1)
- [Suchfeld mit Postgres](/blog/textsearchingpt11)
- [Einführung in OpenSearch](/blog/textsearchingpt2)

In diesem Teil behandeln wir, wie Sie neue glänzende OpenSearch-Instanz mit ASP.NET Core verwenden.

[TOC]

## Einrichtung

Sobald die OpenSearch-Instanz läuft, können wir mit ihr interagieren. Wir werden die [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) für.NET.
Zuerst richten wir den Client in unserer Setup-Erweiterung ein

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Dadurch wird der Client mit dem Endpunkt und den Anmeldeinformationen eingerichtet. Wir aktivieren auch den Debug-Modus, damit wir sehen können, was los ist. Da wir keine REAL SSL-Zertifikate verwenden, deaktivieren wir die Zertifikatsvalidierung (nicht in der Produktion).

## Indexierungsdaten

Das Kernkonzept in OpenSearch ist der Index. Denken Sie an einen Index wie eine Datenbanktabelle; dort werden alle Ihre Daten gespeichert.

Um dies zu tun, benutzen wir die [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) für.NET. Sie können dies über NuGet installieren:

Sie werden feststellen, dass es zwei gibt - Opensearch.Net und Opensearch.Client. Die erste ist die Low-Level-Sache wie Verbindungsmanagement, die zweite ist die High-Level-Sache wie Indexierung und Suche.

Jetzt, wo wir es installiert haben, können wir anfangen, Indexierungsdaten zu betrachten.

Einen Index zu erstellen ist semi-straightforward. Sie definieren einfach, wie Ihr Index aussehen soll und erstellen Sie ihn dann.
Im Code unten sehen Sie, dass wir unser Indexmodell'map' (eine vereinfachte Version des Datenbankmodells des Blogs).
Für jedes Feld dieses Modells definieren wir dann, welcher Typ es ist (Text, Datum, Schlüsselwort usw.) und welcher Analysator zu verwenden ist.

Der Typ ist wichtig, da er definiert, wie die Daten gespeichert werden und wie sie durchsucht werden können. So wird z.B. ein 'Text' Feld analysiert und getokenisiert, ein 'Schlüsselwort' Feld ist es nicht. So würden Sie erwarten, nach einem Schlüsselwortfeld genau so zu suchen, wie es gespeichert ist, aber ein Textfeld, das Sie nach Teilen des Textes suchen können.

Auch hier Kategorien ist eigentlich eine Zeichenkette[] aber der Schlüsselworttyp versteht, wie man sie richtig behandelt.

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## Einträge zum Index hinzufügen

Sobald wir unseren Index eingerichtet haben, um Elemente hinzuzufügen, müssen wir Elemente zu diesem Index hinzufügen. Hier, da wir eine BUNCH hinzufügen, verwenden wir eine Bulk-Insert-Methode.

Sie können sehen, dass wir zunächst in eine Methode namens rufen`GetExistingPosts` die alle Posts zurückgibt, die bereits im Index enthalten sind. Wir gruppieren dann die Beiträge nach Sprache und filtern die 'uk' Sprache heraus (da wir das nicht indexieren wollen, da es ein zusätzliches Plugin benötigt, das wir noch nicht haben). Wir filtern dann alle Beiträge, die bereits im Index sind.
Wir verwenden den Hash und die ID, um zu identifizieren, ob ein Post bereits im Index ist.

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

Sobald wir die vorhandenen Beiträge und unseren fehlenden Analysator herausgefiltert haben, erstellen wir einen neuen Index (basierend auf dem Namen, in meinem Fall "meistlucid-blog-<language>== Weblinks ==== Einzelnachweise == Dieser Großauftrag ist eine Sammlung von Operationen, die auf dem Index durchgeführt werden sollen.
Dies ist effizienter, als jedes Element nacheinander hinzuzufügen.

Sie werden sehen, dass in der `BulkRequest` stellen wir die `Refresh` Eigentum an `true`......................................................................................................... Das bedeutet, dass der Index aktualisiert wird, nachdem der Masseneinsatz abgeschlossen ist. Das ist nicht wirklich notwendig, aber es ist nützlich zum Debuggen.

## Durchsuchen des Index

Eine gute Möglichkeit zu testen, um zu sehen, was hier tatsächlich erstellt wurde, ist, in die Dev Tools auf OpenSearch Dashboards zu gehen und eine Suchanfrage auszuführen.

```json
GET /mostlylucid-blog-*
{}
```

Diese Abfrage liefert uns alle Indexe, die dem Muster entsprechen. `mostlylucid-blog-*`......................................................................................................... (so dass alle unsere Indizes bisher).

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

Dev Tools in OpenSearch Dashboards ist eine gute Möglichkeit, Ihre Abfragen zu testen, bevor Sie sie in Ihren Code eingeben.

![Werkzeuge entwickeln](devtools.png?width=900&quality=25)

## Durchsuchen des Index

Jetzt können wir anfangen, den Index zu durchsuchen. Wir können die `Search` Methode auf dem Client, dies zu tun.
Hier kommt die wahre Kraft von OpenSearch ins Spiel. Es hat buchstäblich [Dutzende von verschiedenen Arten von Abfragen](https://opensearch.org/docs/latest/query-dsl/) Sie können Ihre Daten durchsuchen. Alles von einer einfachen Stichwortsuche bis hin zu einer komplexen 'neuralen' Suche.

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### Abfragebeschreibung

Diese Methode, `GetSearchResults`, wurde entwickelt, um einen bestimmten OpenSearch Index abzufragen, um Blog-Beiträge abzurufen. Es braucht drei Parameter: `language`, `query`, und Paginationsparameter `page` und `pageSize`......................................................................................................... Hier ist, was es tut:

1. **Indexauswahl**:
   
   - Es ruft den Index-Namen mit dem `GetBlogIndexName` Methode auf der Grundlage der bereitgestellten Sprache. Der Index wird dynamisch entsprechend der Sprache ausgewählt.

2. **Suchanfrage**:
   
   - Die Abfrage verwendet eine `Bool` Abfrage mit einem `Must` Klausel, um sicherzustellen, dass die Ergebnisse bestimmten Kriterien entsprechen.
   - Im Inneren des `Must` Abschnitt, a) `MultiMatch` Abfrage wird verwendet, um über mehrere Felder zu suchen (`Title`, `Categories`, und `Content`).
     - **Förderung**: Das `Title` Feld wird einen Schub von gegeben `2.0`, macht es wichtiger bei der Suche, und `Categories` hat einen Schub von `1.5`......................................................................................................... Dies bedeutet, dass Dokumente, in denen die Suchanfrage im Titel oder Kategorien angezeigt wird, höher rangieren.
     - **Abfragetyp**: Es verwendet `BestFields`, welches versucht, das am besten passende Feld für die Abfrage zu finden.
     - **Benommenheit**: Das `Fuzziness.Auto` Parameter erlaubt ungefähre Übereinstimmungen (z.B. Umgang mit kleinen Tippfehlern).

3. **Paginierung**:
   
   - Das `Skip` Methode überspringt die erste `n` Ergebnisse je nach Seitenzahl, berechnet als `(page - 1) * pageSize`......................................................................................................... Dies hilft bei der Navigation durch paginierte Ergebnisse.
   - Das `Size` Methode begrenzt die Anzahl der auf das angegebene Dokument zurückgegebenen Dokumente `pageSize`.

4. **Fehlerbehebung**:
   
   - Wenn die Abfrage fehlschlägt, wird ein Fehler protokolliert und eine leere Liste zurückgegeben.

5. **Ergebnis**:
   
   - Die Methode gibt eine Liste der `BlogIndexModel` Dokumente, die den Suchkriterien entsprechen.

So können Sie sehen, dass wir super flexibel darüber sein können, wie wir unsere Daten durchsuchen. Wir können nach bestimmten Feldern suchen, wir können bestimmte Felder steigern, wir können sogar über mehrere Indexe suchen.

Ein großer Vorteil ist die einfache qith, die wir unterstützen können mehrere Sprachen. Wir haben einen anderen Index für jede Sprache und ermöglichen die Suche innerhalb dieses Index. Das bedeutet, dass wir für jede Sprache den richtigen Analysator verwenden und die besten Ergebnisse erzielen können.

## Die neue Such-API

Im Gegensatz zu der Such-API, die wir in den vorherigen Teilen dieser Serie gesehen haben, können wir den Suchprozess durch OpenSearch erheblich vereinfachen. Wir können einfach Text auf diese Frage werfen und tolle Ergebnisse zurückbekommen.

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

Wie Sie sehen können, haben wir alle Daten, die wir im Index benötigen, um die Ergebnisse zurückzugeben. Wir können dies dann verwenden, um eine URL zum Blog-Post zu generieren. Dies nimmt die Last von unserer Datenbank und macht den Suchprozess viel schneller.

## Schlussfolgerung

In diesem Beitrag haben wir gesehen, wie man einen C#-Client schreibt, um mit unserer OpenSearch-Instanz zu interagieren.