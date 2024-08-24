# Ricerca di testo completo (Pt 3 - OpenSearch with ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Introduzione

Nelle parti precedenti di questa serie abbiamo introdotto il concetto di ricerca completa del testo e come può essere utilizzato per cercare il testo all'interno di un database. In questa parte presenteremo come utilizzare OpenSearch con ASP.NET Core.

Parti precedenti:

- [Ricerca di testo completo con Postgres](/blog/textsearchingpt1)
- [Casella di ricerca con Postgres](/blog/textsearchingpt11)
- [Introduzione alla ricerca aperta](/blog/textsearchingpt2)

In questa parte ci occuperemo di come iniziare a usare la nuova istanza OpenSearch con ASP.NET Core.

[TOC]

## Configurazione

Una volta che abbiamo l'istanza OpenSearch in esecuzione possiamo iniziare ad interagire con essa. Useremo il [Client di ricerca aperta](https://opensearch.org/docs/latest/clients/OSC-dot-net/) per.NET.
Prima abbiamo impostato il client nella nostra estensione di configurazione

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Questo imposta il client con l'endpoint e le credenziali. Abilitiamo anche la modalità debug in modo da poter vedere cosa sta succedendo. Inoltre, poiché non utilizziamo certificati SSL REAL, disabilitiamo la convalida dei certificati (non farlo in produzione).

## Dati di indicizzazione

Il concetto principale di OpenSearch è l'Indice. Pensa a un indice come una tabella dei database; è dove vengono memorizzati tutti i tuoi dati.

Per fare questo useremo il [Client di ricerca aperta](https://opensearch.org/docs/latest/clients/OSC-dot-net/) per.NET. È possibile installare questo tramite NuGet:

Noterete che ce ne sono due lì - Opensearch.Net e Opensearch.Client. Il primo è la roba di basso livello come la gestione della connessione, il secondo è la roba di alto livello come l'indicizzazione e la ricerca.

Ora che l'abbiamo installato possiamo iniziare a guardare i dati di indicizzazione.

Creare un indice è semi-drittorno. Definisci semplicemente come dovrebbe essere il tuo indice e poi crealo.
Nel codice qui sotto potete vedere'mappa' il nostro Modello Index (una versione semplificata del modello del database del blog).
Per ogni campo di questo modello definiamo poi che tipo è (testo, data, parola chiave, ecc.) e quale analizzatore usare.

Il Tipo è importante in quanto definisce come i dati vengono memorizzati e come possono essere cercati. Ad esempio, un campo 'testo' viene analizzato e tokenizzato, un campo 'keyword' non lo è. Quindi ci si aspetterebbe di cercare un campo di parole chiave esattamente come viene memorizzato, ma un campo di testo è possibile cercare parti del testo.

Anche qui Categorie è in realtà una stringa[] ma il tipo di parola chiave capisce come gestirli correttamente.

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

## Aggiunta di elementi all'indice

Una volta che abbiamo il nostro indice impostato per aggiungere elementi ad esso abbiamo bisogno di aggiungere elementi a questo indice. Qui come stiamo aggiungendo un BUNCH usiamo un metodo di inserimento di massa.

Potete vedere che inizialmente chiamiamo in un metodo chiamato`GetExistingPosts` che restituisce tutti i post che sono già nell'indice. Raggruppiamo quindi i post per lingua e filtriamo il linguaggio 'uk' (in quanto non vogliamo indicizzarlo perché ha bisogno di un plugin extra che non abbiamo ancora). Poi filtriamo tutti i post che sono già nell'indice.
Usiamo l'hash e l'id per identificare se un post è già nell'indice.

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

Una volta filtrati i post esistenti e il nostro analizzatore mancante creiamo un nuovo Indice (basato sul nome, nel mio caso "per lo più Lucid-blog-<language>") e poi creare una richiesta all'ingrosso. Questa richiesta all'ingrosso è una raccolta di operazioni da effettuare sull'indice.
Questo è più efficiente di aggiungere ogni elemento uno per uno.

Lo vedrai nel `BulkRequest` Abbiamo impostato il `Refresh` proprietà a `true`. Ciò significa che dopo l'inserto sfuso è completato l'indice viene aggiornato. Questo non è davvero necessario, ma è utile per il debug.

## Ricerca dell'indice

Un buon modo per testare ciò che è stato effettivamente creato qui è quello di andare nei Dev Tools su OpenSearch Dashboards ed eseguire una query di ricerca.

```json
GET /mostlylucid-blog-*
{}
```

Questa query ci restituisce tutti gli indici corrispondenti al modello `mostlylucid-blog-*`. (quindi tutti i nostri indici finora).

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

Dev Tools in OpenSearch Dashboards è un ottimo modo per testare le tue query prima di inserirle nel tuo codice.

![Strumenti Dev](devtools.png?width=900&quality=25)

## Ricerca dell'indice

Ora possiamo iniziare a cercare l'indice. Possiamo usare il `Search` metodo sul cliente per fare questo.
Qui entra in gioco il vero potere di OpenSearch. Ha letteralmente [decine di diversi tipi di query](https://opensearch.org/docs/latest/query-dsl/) puoi usare per cercare i tuoi dati. Tutto da una semplice ricerca di parole chiave ad una ricerca 'neurale' complessa.

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

### Descrizione dell'interrogazione

Questo metodo, `GetSearchResults`, è stato progettato per interrogare uno specifico indice OpenSearch per recuperare i post del blog. Ci vogliono tre parametri: `language`, `query`, e parametri di paginazione `page` e `pageSize`. Ecco cosa fa:

1. **Selezione dell'indice**:
   
   - Recupera il nome dell'indice usando il `GetBlogIndexName` metodo basato sulla lingua fornita. L'indice è selezionato dinamicamente in base alla lingua.

2. **Interrogazione di ricerca**:
   
   - La query utilizza un `Bool` query con una `Must` clausola per garantire che i risultati corrispondano a determinati criteri.
   - All'interno della `Must` clausola, a `MultiMatch` query è usato per cercare tra più campi (`Title`, `Categories`, e `Content`).
     - **Innalzamento**: La `Title` campo è dato un impulso di `2.0`, rendendolo più importante nella ricerca, e `Categories` ha una spinta di `1.5`. Ciò significa che i documenti in cui la query di ricerca appare nel titolo o categorie sarà rango più alto.
     - **Tipo di interrogazione**: Usa `BestFields`, che cerca di trovare il miglior campo di corrispondenza per la query.
     - **Fuzziness**: La `Fuzziness.Auto` Il parametro permette corrispondenze approssimative (ad esempio, la gestione di errori di battitura minori).

3. **Paginazione**:
   
   - La `Skip` metodo salta il primo `n` risultati a seconda del numero di pagina, calcolati come `(page - 1) * pageSize`. Questo aiuta a navigare attraverso i risultati immaginati.
   - La `Size` metodo limita il numero di documenti restituiti al specificato `pageSize`.

4. **Gestione degli errori**:
   
   - Se la query fallisce, viene registrato un errore e viene restituito un elenco vuoto.

5. **Risultato**:
   
   - Il metodo restituisce un elenco di `BlogIndexModel` documenti corrispondenti ai criteri di ricerca.

Così potete vedere che possiamo essere super flessibili su come cerchiamo i nostri dati. Possiamo cercare campi specifici, possiamo aumentare alcuni campi, possiamo anche cercare su più indici.

Un vantaggio BIG è la facilità qith che possiamo supportare più lingue. Abbiamo un indice diverso per ogni lingua e abilitare la ricerca all'interno di tale indice. Questo significa che possiamo utilizzare l'analizzatore corretto per ogni lingua e ottenere i migliori risultati.

## La nuova API di ricerca

In contrasto con l'API di ricerca che abbiamo visto nelle parti precedenti di questa serie, possiamo semplificare notevolmente il processo di ricerca utilizzando OpenSearch. Possiamo semplicemente scrivere a questa domanda e ottenere ottimi risultati.

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

Come potete vedere abbiamo tutti i dati di cui abbiamo bisogno nell'indice per restituire i risultati. Possiamo quindi usarlo per generare un URL al post del blog. Questo toglie il carico dal nostro database e rende il processo di ricerca molto più veloce.

## In conclusione

In questo post abbiamo visto come scrivere un client C# per interagire con la nostra istanza OpenSearch.