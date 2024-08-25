# Volledige tekst zoeken (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Inleiding

Het zoeken naar inhoud is een cruciaal onderdeel van elke inhoud zware website. Het verbetert de ontdekbaarheid en gebruikerservaring. In dit bericht zal ik behandelen hoe ik toegevoegd full text zoeken naar deze site

Volgende delen in deze serie:

- [Zoekvak met Postgres](/blog/textsearchingpt11)
- [Inleiding tot OpenSearch](/blog/textsearchingpt2)
- [Open zoeken met C#](/blog/textsearchingpt3)

[TOC]

# Aanpak

Er zijn een aantal manieren om full text te zoeken, waaronder

1. Gewoon zoeken naar een in het geheugen data structuur (zoals een lijst), dit is relatief eenvoudig te implementeren, maar niet goed schaalt. Bovendien ondersteunt het geen complexe queries zonder veel werk.
2. Het gebruik van een database zoals SQL Server of Postgres. Hoewel dit werkt en ondersteuning heeft van bijna alle database types is het niet altijd de beste oplossing voor complexere datastructuren of complexe vragen; maar het is wat dit artikel zal behandelen.
3. Met behulp van een lichtgewicht zoektechnologie zoals [Luceen](https://lucenenet.apache.org/) of SQLite FTS. Dit is een middenweg tussen de twee bovenstaande oplossingen. Het is complexer dan alleen zoeken naar een lijst maar minder complex dan een volledige database oplossing. Echter; het is nog steeds vrij complex om te implementeren (vooral voor het inslikken van gegevens) en niet schalen als een volledige zoekoplossing. In werkelijkheid vele andere zoektechnologieën [gebruik Lucene onder de kap voor ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) Het is verbazingwekkende vector zoekmogelijkheden.
4. Met behulp van een zoekmachine zoals ElasticSearch, OpenSearch of Azure Search. Dit is de meest complexe & resource intensive oplossing maar ook de meest krachtige. Het is ook de meest schaalbare en kan complexe vragen met gemak omgaan. Ik ga in de komende week naar ondraaglijke diepte over hoe je zelf-hosten, configureren en OpenSearch van C# kunt gebruiken.

# Database Volledige tekst zoeken met Postgres

In deze blog heb ik onlangs verplaatst naar het gebruik van Postgres voor mijn database. Postgres heeft een full text zoekfunctie die zeer krachtig en (iets) gemakkelijk te gebruiken is. Het is ook erg snel en kan complexe vragen gemakkelijk aan.

Bij het bouwen van yout `DbContext` u kunt aangeven welke velden full text search functionaliteit ingeschakeld hebben.

Postgres gebruikt het concept van zoekvectoren om snel, efficiënt Full Text Searching te bereiken. Een zoekvector is een gegevensstructuur die de woorden in een document en hun posities bevat. Het vooraf berekenen van de zoekvector voor elke rij in de database laat Postgres toe om snel naar woorden in het document te zoeken.
Het maakt gebruik van twee speciale data types om dit te bereiken:

- TSVector: Een speciaal PostgreSQL datatype dat een lijst van lexemes opslaat (zie het als een vector van woorden). Het is de geïndexeerde versie van het document gebruikt voor het snel zoeken.
- TSQuery: Een ander speciaal datatype dat de zoekopdracht opslaat, inclusief de zoektermen en logische operators (zoals AND, OR, NOT).

Daarnaast biedt het een ranking functie waarmee u de resultaten te rangschikken op basis van hoe goed ze overeenkomen met de zoekopdracht. Dit is zeer krachtig en stelt u in staat om de resultaten te bestellen door relevantie.
PostgreSQL kent een ranking toe aan de resultaten op basis van relevantie. Relevantie wordt berekend door rekening te houden met factoren zoals de nabijheid van de zoektermen naar elkaar en hoe vaak ze in het document verschijnen.
De ts_rank of ts_rank_cd functies worden gebruikt om deze ranking te berekenen.

U kunt meer lezen over de full text zoekfuncties van Postgres [Hier.](https://www.postgresql.org/docs/current/textsearch.html)

## Entiteitskader

Het kaderpakket van de Postgres-entiteit [Hier.](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) biedt krachtige ondersteuning voor het zoeken naar volledige tekst. Hiermee kunt u aangeven welke velden full text geïndexeerd zijn en hoe u ze kunt opvragen.

Om dit te doen voegen we specifieke indextypes toe aan onze entiteiten zoals gedefinieerd in `DbContext`:

```csharp
   modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

                entity.HasIndex(b => new { b.Title, b.PlainTextContent})
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");
  ...
```

Hier voegen we een full text index toe aan de `Title` en `PlainTextContent` velden van onze `BlogPostEntity`. We geven ook aan dat de index gebruik moet maken van de `GIN` index type en de `english` taal. Dit is belangrijk omdat het Postgres vertelt hoe de gegevens te indexeren en welke taal te gebruiken om woorden te onderdrukken en te stoppen.

Dit is natuurlijk een probleem voor onze blog als we hebben meerdere talen. Helaas voor nu gebruik ik alleen de `english` taal voor alle posten. Dit is iets waar ik in de toekomst iets aan moet doen, maar voor nu werkt het goed genoeg.

We voegen ook een index toe aan onze `Category` entiteit:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Door dit te doen genereert Postgres een zoekvector voor elke rij in de database. Deze vector bevat de woorden in de `Title` en `PlainTextContent` Velden. We kunnen dan deze vector gebruiken om naar woorden te zoeken in het document.

Dit vertaalt zich naar een to_tsvector functie in SQL die de zoekvector voor de rij genereert. We kunnen dan de ts_rank functie gebruiken om de resultaten te rangschikken op basis van relevantie.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Pas dit toe als een migratie naar onze database en we zijn klaar om te gaan zoeken.

# Zoeken

## Tsvectorindex

Om te zoeken gebruiken we de `EF.Functions.ToTsVector` en `EF.Functions.WebSearchToTsQuery` functies om een zoekvector en query aan te maken. We kunnen dan gebruik maken van de `Matches` functie om te zoeken naar de zoekopdracht in de zoekvector.

```csharp
  var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
       
```

De EF.Functions.WebSearchToTsQuery functie genereert de query voor de rij op basis van gemeenschappelijke Web Zoekmachine syntax.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

In dit voorbeeld kun je zien dat dit een query genereert die zoekt naar de woorden "sad cat" of "fat rat" in het document. Dit is een krachtige functie die ons in staat stelt om te zoeken naar complexe queries met gemak.

Zoals aangegeven genereren deze methoden zowel de zoekvector als de zoekopdracht voor de rij. We gebruiken dan de `Matches` functie om te zoeken naar de zoekopdracht in de zoekvector. We kunnen ook gebruik maken van de `Rank` functie om de resultaten te rangschikken naar relevantie.

Zoals je kunt zien is dit geen eenvoudige vraag, maar het is zeer krachtig en laat ons zoeken naar woorden in de `Title`, `PlainTextContent` en `Category` velden van onze `BlogPostEntity` en rangschik deze door relevantie.

## WebAPI

Om deze (in de toekomst) te gebruiken kunnen we een eenvoudig WebAPI-eindpunt maken dat een query neemt en de resultaten teruggeeft. Dit is een eenvoudige controller die een query neemt en de resultaten teruggeeft:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(IMostlylucidDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<JsonHttpResult<List<SearchResults>>> Search(string query)
    {;

        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
        
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug)).ToList();
        
        return TypedResults.Json(output);
    }

```

## Gegenereerde kolom en typeAhead

Een alternatieve benadering van het gebruik van deze'simpele' TsVector-indexen is om een gegenereerde kolom te gebruiken om de Search Vector op te slaan en dit vervolgens te gebruiken om te zoeken. Dit is een complexere aanpak, maar zorgt voor betere prestaties.
Hier passen we onze `BlogPostEntity` om een speciaal type kolom toe te voegen:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Dit is een berekende kolom die de zoekvector voor de rij genereert. We kunnen dan deze kolom gebruiken om naar woorden in het document te zoeken.

Vervolgens zetten we deze index op binnen onze entiteitsdefinitie (nog om te bevestigen maar dit kan ons ook toelaten om meerdere talen te hebben door een taalkolom voor elke post te specificeren).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Je zult hier zien dat we gebruiken `HasComputedColumnSql` om expliciet de PostGreSQL-functie te specificeren om de zoekvector te genereren. We geven ook aan dat de kolom is opgeslagen in de database. Dit is belangrijk omdat het Postgres vertelt om de zoekvector op te slaan in de database. Hiermee kunnen we zoeken naar woorden in het document met behulp van de zoekvector.

In de database dit gegenereerd voor elke rij, die zijn de 'lexemes' in het document en hun posities:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### ZoekenAPI

We kunnen dan deze kolom gebruiken om naar woorden in het document te zoeken. We kunnen gebruik maken van de `Matches` functie om te zoeken naar de zoekopdracht in de zoekvector. We kunnen ook gebruik maken van de `Rank` functie om de resultaten te rangschikken naar relevantie.

```csharp
       var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
```

Je ziet hier dat we ook een andere query constructor gebruiken `EF.Functions.ToTsQuery("english", query + ":*")`  waarmee we een TypeAhead type functionaliteit kunnen aanbieden (waar we bijvoorbeeld kunnen typen. 'cat' en krijg 'cat', 'cats', 'rups' enz.).

Bovendien laat het ons vereenvoudigen van de belangrijkste blog post query om gewoon te zoeken naar de query in de `SearchVector` Column. Dit is een krachtige functie die ons in staat stelt om te zoeken naar woorden in de `Title`, `PlainTextContent`. We gebruiken nog steeds de index die we hierboven hebben getoond voor de `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

We gebruiken dan de `Rank` functie om de resultaten te rangschikken naar relevantie op basis van de query.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Dit laat ons het eindpunt als volgt gebruiken, waar we in de eerste paar letters van een woord kunnen passeren en alle berichten terugkrijgen die overeenkomen met dat woord:

U kunt de [API in actie hier](https://www.mostlylucid.net/swagger/index.html) kijk voor de `/api/SearchApi`. (Opmerking; Ik heb Swagger ingeschakeld voor deze site zodat u de API in actie kunt zien, maar meestal moet dit worden gereserveerd voor 

![API](searchapi.png?width=900&format=webp&quality=50)

In de toekomst voeg ik een TypeAhead functie toe aan het zoekvak op de site die deze functionaliteit gebruikt.

# Conclusie

U kunt zien dat het mogelijk is om krachtige zoekfunctionaliteit te krijgen met behulp van Postgres en Entity Framework. Het heeft echter complexiteiten en beperkingen waar we rekening mee moeten houden (zoals het taalgedoe). In het volgende deel zal ik behandelen hoe we dit zouden doen met OpenSearch - wat een ton meer setup heeft maar krachtiger en schaalbaar is.