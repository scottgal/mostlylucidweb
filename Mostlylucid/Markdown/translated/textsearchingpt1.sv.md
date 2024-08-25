# Fullständig textsökning (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">I enlighet med artikel 4 i förordning (EU) nr 1307/2013 ska medlemsstaterna se till att de behöriga myndigheterna i de medlemsstater som avses i artikel 4 i förordning (EU) nr 1307/2013 vidtar lämpliga åtgärder för att se till att dessa åtgärder genomförs på ett effektivt sätt och på ett sätt som är förenligt med den inre marknaden.</datetime>

# Inledning

Söka efter innehåll är en kritisk del av något innehåll tung webbplats. Det förbättrar upptäckbarheten och användarupplevelsen. I det här inlägget ska jag täcka hur jag lagt till fulltext söker efter denna webbplats

Nästa delar i denna serie:

- [Sökruta med postgres](/blog/textsearchingpt11)
- [Introduktion till OpenSearch](/blog/textsearchingpt2)
- [Opensearch med C#](/blog/textsearchingpt3)

[TOC]

# Inflygningar

Det finns ett antal sätt att göra fullständig textsökning inklusive

1. Bara att söka i en minnesdatastruktur (som en lista), är detta relativt enkelt att genomföra, men det skalas inte väl. Dessutom stöder det inte komplexa frågor utan mycket arbete.
2. Använda en databas som SQL Server eller Postgres. Även om detta fungerar och har stöd från nästan alla databastyper är det inte alltid den bästa lösningen för mer komplexa datastrukturer eller komplexa frågor; men det är vad den här artikeln kommer att täcka.
3. Använda en lätt sökteknik som [Lönnsocker (inbegripet sirap och andra lösningar av druvsocker) samt sirap och andra lösningar av druvsocker eller maltodextrin (exkl. sirap och andra lösningar av druvsocker eller maltodextrin samt sirap och andra lösningar av druvsocker eller maltodextrin samt sirap och andra lösningar av druvsocker eller maltodextrin samt sirap och andra lösningar av druvsocker eller maltodextrin)](https://lucenenet.apache.org/) eller SQLite FTS. Detta är en medelväg mellan de två ovan nämnda lösningarna. Det är mer komplicerat än att bara söka en lista men mindre komplex än en fullständig databaslösning. Men, det är fortfarande ganska komplicerat att genomföra (särskilt för intag av data) och inte skala så bra som en fullständig söklösning. I sanning många andra sökteknologier [använda Lucene under huven för ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) Det är fantastiska vektorsökfunktioner.
4. Använda en sökmotor som ElasticSearch, OpenSearch eller Azure Search. Detta är den mest komplexa och resursintensiva lösningen men också den mest kraftfulla. Det är också den mest skalbara och kan hantera komplexa frågor med lätthet. Jag kommer att gå in på olidligt djup i nästa vecka eller så om hur man själv-värd, konfigurera och använda OpenSearch från C#.

# Databas Fulltextsökning med postgres

I den här bloggen har jag nyligen flyttat till att använda Postgres för min databas. Postgres har en fulltextsökningsfunktion som är mycket kraftfull och (något) lätt att använda. Det är också mycket snabbt och kan hantera komplexa frågor med lätthet.

När du bygger ditt `DbContext` Du kan ange vilka fält som har fulltextsökning aktiverad.

Postgres använder begreppet sökvektorer för att uppnå snabb, effektiv fulltextsökning. En sökvektor är en datastruktur som innehåller orden i ett dokument och deras positioner. I grund och botten förkomputerar sökvektorn för varje rad i databasen gör det möjligt för Postgres att söka efter ord i dokumentet mycket snabbt.
Den använder två särskilda datatyper för att uppnå detta:

- TSVector: En speciell PostgreSQL datatyp som lagrar en lista med lexemes (tänk på det som en vektor för ord). Det är den indexerade versionen av dokumentet som används för snabb sökning.
- TSQuery: En annan speciell datatyp som lagrar sökfrågan, som innehåller sökord och logiska operatörer (som AND, OR, NOT).

Dessutom erbjuder det en ranking funktion som gör att du kan rangordna resultaten baserat på hur väl de matchar sökfrågan. Detta är mycket kraftfullt och gör att du kan beställa resultaten av relevans.
PostgreSQL tilldelar en ranking till resultaten baserat på relevans. Relevansen beräknas genom att ta hänsyn till faktorer som närheten av sökorden till varandra och hur ofta de förekommer i dokumentet.
Funktionerna ts_rank eller ts_rank_cd används för att beräkna denna rankning.

Du kan läsa mer om sökfunktionerna i fulltext i Postgres [här](https://www.postgresql.org/docs/current/textsearch.html)

## Entity Framework (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework)) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework) (Entity Framework)

Rampaketet för postgress-enheter [här](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) ger kraftfullt stöd för fulltextsökning. Det låter dig ange vilka fält som är fulltext indexerade och hur man frågar dem.

För att göra detta lägger vi till specifika indextyper till våra Enheter enligt definition i `DbContext`:

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

Här lägger vi till ett fulltextindex till `Title` och `PlainTextContent` våra områden `BlogPostEntity`....................................... Vi specificerar också att indexet bör använda `GIN` Indextyp och `english` Språk. Detta är viktigt eftersom det talar om för Postgres hur man indexerar data och vilket språk man ska använda för att dra tillbaka och stoppa ord.

Detta är naturligtvis en fråga för vår blogg eftersom vi har flera språk. Tyvärr just nu använder jag bara `english` Språk för alla inlägg. Detta är något som jag kommer att behöva ta itu med i framtiden, men för tillfället fungerar det bra nog.

Vi lägger också till ett index till vår `Category` Företag:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Genom att göra detta genererar Postgres en sökvektor för varje rad i databasen. Den här vektorn innehåller orden i `Title` och `PlainTextContent` Fält. Vi kan sedan använda denna vektor för att söka efter ord i dokumentet.

Detta översätts till en to_tsvector-funktion i SQL som genererar sökvektorn för raden. Vi kan sedan använda ts_rank-funktionen för att rangordna resultaten baserat på relevans.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Tillämpa detta som en migration till vår databas och vi är redo att börja söka.

# Söker

## TsVector Index

För att söka vi använder kommer att använda `EF.Functions.ToTsVector` och `EF.Functions.WebSearchToTsQuery` funktioner för att skapa en sökvektor och sökfråga. Vi kan sedan använda `Matches` funktion för att söka efter sökfrågan i sökvektorn.

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

Funktionen EF.Functions.WebSearchToTsQuery genererar frågan för raden baserat på vanliga Web Search motor syntax.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

I detta exempel kan du se att detta genererar en fråga som söker efter orden "sad katt" eller "fat råtta" i dokumentet. Detta är en kraftfull funktion som gör att vi kan söka efter komplexa frågor med lätthet.

Som anges befpre dessa metoder både generera sökvektorn och fråga för raden. Vi använder sedan `Matches` funktion för att söka efter sökfrågan i sökvektorn. Vi kan också använda `Rank` Funktion för att rangordna resultaten efter relevans.

Som ni kan se är detta inte en enkel fråga men det är mycket kraftfull och tillåter oss att söka efter ord i `Title`, `PlainTextContent` och `Category` våra områden `BlogPostEntity` och rangordna dessa efter relevans.

## WebbaPI

För att använda dessa (i framtiden) kan vi skapa en enkel WebAPI endpoint som tar en fråga och returnerar resultaten. Detta är en enkel controller som tar en fråga och returnerar resultaten:

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

## Genererad kolumn och typeAhead

Ett alternativ till att använda dessa 'enkel' TsVector index är att använda en genererad kolumn för att lagra Sök Vector och sedan använda detta för att söka. Detta är ett mer komplext tillvägagångssätt men möjliggör bättre resultat.
Här modifierar vi vår `BlogPostEntity` För att lägga till en särskild typ av kolumn:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Detta är en beräknad kolumn som genererar sökvektorn för raden. Vi kan sedan använda denna kolumn för att söka efter ord i dokumentet.

Vi sätter sedan upp detta index inom vår enhetsdefinition (ännu för att bekräfta men detta kan också göra det möjligt för oss att ha flera språk genom att ange en språk kolumn för varje inlägg).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Du kommer att se här att vi använder `HasComputedColumnSql` för att explicit ange PostGreSQL-funktionen för att skapa sökvektorn. Vi anger också att kolumnen lagras i databasen. Detta är viktigt eftersom det säger Postgres att lagra sökvektorn i databasen. Detta gör att vi kan söka efter ord i dokumentet med hjälp av sökvektorn.

I databasen genererade detta för varje rad, som är "lexemen" i dokumentet och deras positioner:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### SökAPI

Vi kan sedan använda denna kolumn för att söka efter ord i dokumentet. Vi kan använda `Matches` funktion för att söka efter sökfrågan i sökvektorn. Vi kan också använda `Rank` Funktion för att rangordna resultaten efter relevans.

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

Ni ser här att vi också använder en annan frågebyggare. `EF.Functions.ToTsQuery("english", query + ":*")`  vilket gör att vi kan erbjuda en typeAhead-funktionalitet (där vi kan skriva t.ex. 'Katt' och 'katt', 'katt', 'kattpelare' etc.).

Dessutom kan vi förenkla den huvudsakliga blogginlägg fråga för att bara söka efter frågan i `SearchVector` Kolumn. Detta är en kraftfull funktion som gör att vi kan söka efter ord i `Title`, `PlainTextContent`....................................... Vi använder fortfarande indexet vi visade ovan för `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Vi använder sedan `Rank` funktion för att rangordna resultaten efter relevans baserat på frågan.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Detta låter oss använda endpointen som följer, där vi kan passera i de första bokstäverna i ett ord och få tillbaka alla inlägg som matchar det ordet:

Du kan se [API i verksamhet här](https://www.mostlylucid.net/swagger/index.html) leta efter `/api/SearchApi`....................................... (Observera; Jag har aktiverat Swagger för denna webbplats så att du kan se API i praktiken, men för det mesta bör detta reserveras för `IsUtveckling()).

![API: er](searchapi.png?width=900&format=webp&quality=50)

I framtiden lägger jag till en typeAhead-funktion i sökrutan på webbplatsen som använder denna funktionalitet.

# Slutsatser

Du kan se att det är möjligt att få kraftfull sökfunktion med Postgres och Entity Framework. Men det har komplexitet och begränsningar som vi måste redogöra för (som språkgrejen). I nästa del ska jag täcka hur vi skulle göra detta med OpenSearch - vilket är har en ton mer setup men är mer kraftfull och skalbar.