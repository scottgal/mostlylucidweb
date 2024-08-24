# Volledige tekst zoeken (Pt 3 - OpenZoeken met ASP.NET-kern)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Inleiding

In de vorige delen van deze serie introduceerden we het concept van full text searching en hoe het gebruikt kan worden om tekst binnen een database te zoeken. In dit deel zullen we introduceren hoe OpenSearch te gebruiken met ASP.NET Core.

Vorige delen:

- [Volledige tekst zoeken met Postgres](/blog/textsearchingpt1)
- [Zoekvak met Postgres](/blog/textsearchingpt11)
- [Inleiding tot OpenSearch](/blog/textsearchingpt3)

In dit deel behandelen we hoe we je nieuwe glanzende OpenSearch instance kunnen gaan gebruiken met ASP.NET Core.

[TOC]

## Instellen

Zodra we de OpenSearch instantie aan de praat hebben kunnen we ermee beginnen. We zullen gebruik maken van de [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) voor.NET.
Eerst zetten we de client in onze Setup extensie

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Dit stelt de klant met het eindpunt en de referenties. We zetten ook debug-modus in zodat we kunnen zien wat er aan de hand is. Verder omdat we geen REAL SSL certificaten gebruiken schakelen we certificaatvalidatie uit (doe dit niet in productie).

## Indexeergegevens

Het kernconcept in OpenSearch is de Index. Denk aan een Index zoals een Database tabel; het is waar al uw gegevens worden opgeslagen.

Om dit te doen gebruiken we de [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) voor.NET. U kunt dit installeren via NuGet:

U zult merken dat er twee daar - Opensearch.Net en Opensearch.Client. De eerste is het lage niveau dingen zoals verbinding management, de tweede is het hoge niveau dingen zoals indexeren en zoeken.

Nu we het hebben geïnstalleerd kunnen we beginnen met het indexeren van gegevens.

Het maken van een index is semi-rechtdoor. Je bepaalt gewoon hoe je index eruit moet zien en dan maak je het.
In de onderstaande code kun je zien dat we ons Index Model'map' (een vereenvoudigde versie van de database van de blog model).
Voor elk veld van dit model bepalen we vervolgens welk type het is (tekst, datum, trefwoord enz.) en welke analysator het moet gebruiken.

Het Type is belangrijk omdat het bepaalt hoe de gegevens worden opgeslagen en hoe het kan worden doorzocht. Bijvoorbeeld, een 'tekst' veld wordt geanalyseerd en gesymboliseerd, een'sleutelwoord' veld is dat niet. Dus je zou verwachten te zoeken naar een zoekwoord veld precies zoals het is opgeslagen, maar een tekst veld kunt u zoeken naar delen van de tekst.

Ook hier Categorieën is eigenlijk een string[] maar het type trefwoord begrijpt hoe ze correct te behandelen.

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

## Items aan de index toevoegen

Zodra we onze index hebben ingesteld om items toe te voegen moeten we items toevoegen aan deze index. Hier als we een BUNCH toevoegen gebruiken we een bulk insert methode.

U kunt zien dat we in eerste instantie oproepen tot een methode genaamd`GetExistingPosts` die alle berichten teruggeeft die al in de index staan. Vervolgens groeperen we de berichten per taal en filteren we de 'uk' taal (omdat we dat niet willen indexeren omdat het een extra plugin nodig heeft die we nog niet hebben). Vervolgens filteren we alle berichten die al in de index staan.
We gebruiken de hash en id om te identificeren of een bericht al in de index staat.

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

Zodra we hebben gefilterd uit de bestaande berichten en onze ontbrekende analyser maken we een nieuwe Index (gebaseerd op de naam, in mijn geval "meest lucid-blog-<language>") en creëer vervolgens een bulk verzoek. Deze bulk verzoek is een verzameling van bewerkingen uit te voeren op de index.
Dit is efficiënter dan het toevoegen van elk item een voor een.

Dat zie je in de `BulkRequest` We zetten de `Refresh` eigendom aan `true`. Dit betekent dat nadat de bulk inzet is voltooid de index wordt ververst. Dit is niet echt nodig, maar het is nuttig voor debuggen.

## De index doorzoeken

Een goede manier om te testen om te zien wat hier eigenlijk is gemaakt is om in de Dev Tools op OpenSearch Dashboards te gaan en een zoekopdracht uit te voeren.

```json
GET /mostlylucid-blog-*
{}
```

Deze zoekopdracht geeft ons alle indexen terug die overeenkomen met het patroon `mostlylucid-blog-*`. (dus al onze indexen tot nu toe).

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

Dev Tools in OpenSearch Dashboards is een geweldige manier om uw vragen te testen voordat u ze in uw code.

![Dev-gereedschappen](devtools.png?width=900&quality=25)

## De index doorzoeken

Nu kunnen we beginnen met zoeken naar de index. We kunnen gebruik maken van de `Search` methode op de cliënt om dit te doen.
Hier komt de echte kracht van OpenSearch. Het heeft letterlijk [tientallen verschillende soorten zoekopdrachten](https://opensearch.org/docs/latest/query-dsl/) u kunt gebruiken om uw gegevens te doorzoeken. Alles van een simpel zoekwoord tot een complexe 'neurale' zoekopdracht.

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

### Beschrijving van de zoekopdracht

Deze methode, `GetSearchResults`, is ontworpen om een specifieke OpenSearch-index op te vragen om blogberichten op te halen. Er zijn drie parameters voor nodig: `language`, `query`, en paginatieparameters `page` en `pageSize`. Dit is wat het doet:

1. **Indexselectie**:
   
   - Het haalt de index naam met behulp van de `GetBlogIndexName` methode op basis van de verstrekte taal. De index wordt dynamisch geselecteerd volgens de taal.

2. **Zoekopdracht**:
   
   - De query maakt gebruik van a `Bool` query met a `Must` bepaling om ervoor te zorgen dat de resultaten aan bepaalde criteria voldoen.
   - Binnenin de `Must` clausule, a `MultiMatch` query wordt gebruikt om meerdere velden te doorzoeken (`Title`, `Categories`, en `Content`).
     - **Boosten**: De `Title` het veld krijgt een boost van `2.0`, waardoor het belangrijker in de zoektocht, en `Categories` heeft een boost van `1.5`. Dit betekent dat documenten waar de zoekopdracht verschijnt in de titel of categorieën hoger zullen rangschikken.
     - **Zoektype**: Het maakt gebruik van `BestFields`, die probeert het beste bijpassende veld voor de query te vinden.
     - **Tuzzinessunit synonyms for matching user input**: De `Fuzziness.Auto` parameter maakt het mogelijk om bij benadering overeen te komen (bv. kleine typefouten te verwerken).

3. **Paginatie**:
   
   - De `Skip` methode slaat de eerste over `n` resultaten afhankelijk van het paginanummer, berekend als `(page - 1) * pageSize`. Dit helpt bij het navigeren door gepagineerde resultaten.
   - De `Size` methode beperkt het aantal documenten dat wordt teruggestuurd naar de gespecificeerde `pageSize`.

4. **Fout bij omgaan**:
   
   - Als de query mislukt, wordt een fout gelogd en wordt een lege lijst teruggegeven.

5. **Resultaat**:
   
   - De methode geeft een lijst van `BlogIndexModel` documenten die voldoen aan de zoekcriteria.

Dus je kunt zien dat we super flexibel kunnen zijn over hoe we onze gegevens doorzoeken. We kunnen zoeken naar specifieke velden, we kunnen bepaalde velden stimuleren, we kunnen zelfs zoeken over meerdere indexen.

Een groot voordeel is het gemak Qith dat we meerdere talen kunnen ondersteunen. We hebben een andere index voor elke taal en maken het mogelijk om binnen die index te zoeken. Dit betekent dat we de juiste analysator voor elke taal kunnen gebruiken en de beste resultaten kunnen behalen.

## De API voor nieuwe zoekopdrachten

In tegenstelling tot de Search API die we in de vorige delen van deze serie zagen, kunnen we het zoekproces enorm vereenvoudigen door gebruik te maken van OpenSearch. We kunnen gewoon tekst naar deze vraag gooien en geweldige resultaten terugkrijgen.

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

Zoals je kunt zien hebben we alle gegevens die we nodig hebben in de index om de resultaten terug te geven. We kunnen dit vervolgens gebruiken om een URL aan te maken voor de blogpost. Dit haalt de lading van onze database en maakt het zoekproces veel sneller.

## Conclusie

In dit bericht zagen we hoe we een C# client konden schrijven om te communiceren met onze OpenSearch instantie.