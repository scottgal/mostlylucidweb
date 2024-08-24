# Fulltextsökning (Pt 3 - OpenSearch med ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40 ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------</datetime>

## Inledning

I de tidigare delarna av denna serie introducerade vi begreppet fulltextsökning och hur den kan användas för att söka efter text i en databas. I denna del kommer vi att introducera hur man använder OpenSearch med ASP.NET Core.

Tidigare delar:

- [Fullständig textsökning med postgres](/blog/textsearchingpt1)
- [Sökruta med postgres](/blog/textsearchingpt11)
- [Introduktion till OpenSearch](/blog/textsearchingpt2)

I denna del kommer vi att täcka hur du börjar använda dig nya glänsande OpenSearch instans med ASP.NET Core.

[TOC]

## Ställ in

När vi har OpenSearch instansen igång kan vi börja interagera med den. Vi kommer att använda [OpenSearch- klientName](https://opensearch.org/docs/latest/clients/OSC-dot-net/) För.NET.
Först satte vi upp kunden i vårt Setup-tillägg

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Detta sätter upp klienten med slutpunkt och referenser. Vi aktiverar även felsökningsläge så att vi kan se vad som händer. Vidare eftersom vi inte använder REAL SSL-certifikat inaktiverar vi certifikatvalidering (gör inte detta i produktionen).

## Indexeringsuppgifter

Kärnkonceptet i OpenSearch är Index. Tänk på ett index som en databastabell; det är där alla dina data lagras.

För att göra detta kommer vi att använda [OpenSearch- klientName](https://opensearch.org/docs/latest/clients/OSC-dot-net/) För.NET. Du kan installera detta via NuGet:

Du kommer att märka att det finns två där - Opensearch.Net och Opensearch.Client. Den första är låg nivå saker som anslutning management, den andra är hög nivå saker som indexering och sökning.

Nu när vi har installerat den kan vi börja titta på indexeringsdata.

Att skapa ett index är semi-straightforward. Du definierar bara hur ditt index ska se ut och sedan skapa det.
I koden nedan kan du se att vi "kartlägger" vår indexmodell (en förenklad version av bloggens databasmodell).
För varje fält i denna modell definierar vi sedan vilken typ det är (text, datum, nyckelord etc) och vilken analysator som ska användas.

Typen är viktig eftersom den definierar hur data lagras och hur den kan sökas. Ett "textfält" analyseras och polletteras till exempel, ett "nyckelord" fält är inte det. Så du förväntar dig att söka efter ett nyckelordsfält precis som det är lagrat, men ett textfält kan du söka efter delar av texten.

Även här Kategorier är faktiskt en sträng[] men nyckelordet typ förstår hur man hanterar dem korrekt.

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

## Lägga till objekt i indexet

När vi har vårt index satt upp för att lägga till objekt till det måste vi lägga till objekt till detta index. Här som vi lägger till en BUNCH vi använder en bulk insats metod.

Du kan se att vi först kallar in en metod som kallas`GetExistingPosts` vilket returnerar alla inlägg som redan finns i indexet. Vi grupperar sedan inläggen efter språk och filtrerar ut "uk" språket (eftersom vi inte vill indexera att eftersom det behöver en extra plugin vi inte har ännu). Vi filtrerar sedan bort alla inlägg som redan finns i indexet.
Vi använder hash och id för att identifiera om ett inlägg redan finns i indexet.

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

När vi har filtrerat bort de befintliga inläggen och vår saknade analysator skapar vi ett nytt index (baserat på namnet, i mitt fall "mestadelslylucid-blogg-<language>") och sedan skapa en bulk begäran. Denna bulk begäran är en samling av åtgärder att utföra på indexet.
Detta är effektivare än att lägga till varje post en efter en.

Det ska du få se. `BulkRequest` Vi ställer in `Refresh` egendom till `true`....................................... Detta innebär att efter bulkinsatsen är klar uppdateras indexet. Detta är inte riktigt nödvändigt men det är användbart för felsökning.

## Söka i indexet

Ett bra sätt att testa för att se vad som faktiskt har skapats här är att gå in i Dev Tools på OpenSearch Dashboards och köra en sökfråga.

```json
GET /mostlylucid-blog-*
{}
```

Denna fråga kommer att returnera oss alla index som matchar mönstret `mostlylucid-blog-*`....................................... (så alla våra index hittills).

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

Dev Tools i OpenSearch Dashboards är ett bra sätt att testa dina frågor innan du sätter dem i din kod.

![Verktyg för Dev](devtools.png?width=900&quality=25)

## Söka i indexet

Nu kan vi börja söka i indexet. Vi kan använda `Search` metod på klienten för att göra detta.
Det är här den verkliga kraften i OpenSearch kommer in. Det har bokstavligen [dussintals olika typer av frågor](https://opensearch.org/docs/latest/query-dsl/) du kan använda för att söka din data. Allt från en enkel sökordssökning till en komplex 'neural' sökning.

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

### Frågebeskrivning

Denna metod `GetSearchResults`, är utformad för att fråga en specifik OpenSearch index för att hämta blogginlägg. Det krävs tre parametrar: `language`, `query`, och paginationsparametrar `page` och `pageSize`....................................... Så här gör den:

1. **Indexval**:
   
   - Den hämtar indexnamnet med hjälp av `GetBlogIndexName` Metod baserad på det angivna språket. Indexet är dynamiskt valt enligt språket.

2. **Sök efter fråga**:
   
   - Förfrågan använder a `Bool` fråga med en `Must` Klausul för att säkerställa att resultaten överensstämmer med vissa kriterier.
   - Inuti `Must` klausul, a `MultiMatch` fråga används för att söka över flera fält (`Title`, `Categories`, och `Content`).
     - **Förstärkning**: för `Title` fält ges en boost av `2.0`, vilket gör det viktigare i sökandet, och `Categories` har en ökning av `1.5`....................................... Detta innebär att dokument där sökfrågan visas i titeln eller kategorierna rankas högre.
     - **Förfrågans typ**: Den använder `BestFields`, som försöker hitta det bästa matchande fältet för frågan.
     - **Knäpphet**: för `Fuzziness.Auto` Parametern möjliggör ungefärliga matchningar (t.ex. hantering av mindre stavfel).

3. **Paginering**:
   
   - I detta sammanhang är det viktigt att se till att `Skip` metoden hoppar över den första `n` resultat beroende på sidnummer, beräknat som `(page - 1) * pageSize`....................................... Detta hjälper till att navigera genom paginerade resultat.
   - I detta sammanhang är det viktigt att se till att `Size` metod begränsar antalet handlingar som returneras till de angivna `pageSize`.

4. **Felhantering**:
   
   - Om frågan misslyckas loggas ett fel och en tom lista returneras.

5. **Resultat**:
   
   - Metoden returnerar en lista över `BlogIndexModel` dokument som motsvarar sökkriterierna.

Så du kan se att vi kan vara superflexibla om hur vi söker våra data. Vi kan söka efter specifika fält, vi kan öka vissa fält, vi kan även söka över flera index.

En stor fördel är lättheten qith som vi kan stödja flera språk. Vi har olika index för varje språk och möjliggör sökning inom det indexet. Det innebär att vi kan använda rätt analysator för varje språk och få bästa resultat.

## Det nya sökgränssnittet

I motsats till det Search API vi såg i de tidigare delarna av denna serie, kan vi avsevärt förenkla sökprocessen genom att använda OpenSearch. Vi kan bara skicka in text till denna fråga och få bra resultat tillbaka.

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

Som ni kan se har vi alla data vi behöver i indexet för att returnera resultaten. Vi kan sedan använda detta för att generera en URL till blogginlägget. Detta tar bort belastningen från vår databas och gör sökprocessen mycket snabbare.

## Slutsatser

I det här inlägget såg vi hur man skriver en C#-klient för att interagera med vår OpenSearch instans.