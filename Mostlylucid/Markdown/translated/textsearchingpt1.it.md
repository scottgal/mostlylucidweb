# Ricerca completa del testo (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Introduzione

La ricerca di contenuti è una parte critica di qualsiasi sito web pesante contenuto. Migliora la scopribilità e l'esperienza degli utenti. In questo post coprirò come ho aggiunto testo completo alla ricerca di questo sito

[TOC]

# Avvicinamenti

C'è un certo numero di modi per fare la ricerca di testo completo compreso

1. Basta cercare una struttura di dati in memoria (come una lista), questo è relativamente semplice da implementare, ma non scala bene. Inoltre non supporta query complesse senza un sacco di lavoro.
2. Usando un database come SQL Server o Postgres. Mentre questo funziona e ha il supporto da quasi tutti i tipi di database non è sempre la soluzione migliore per strutture di dati più complesse o interrogazioni complesse; tuttavia è ciò che questo articolo coprirà.
3. Utilizzando una tecnologia di ricerca leggera come [Lucene](https://lucenenet.apache.org/) o SQLite FTS. Questa è una via di mezzo tra le due soluzioni di cui sopra. E' piu' complesso della semplice ricerca di una lista, ma meno complessa di una soluzione di database completa. Tuttavia, è ancora abbastanza complesso da implementare (soprattutto per l'ingestione di dati) e non scala così come una soluzione di ricerca completa. In verità molte altre tecnologie di ricerca [usare Lucene sotto il cappuccio per ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) E' una straordinaria capacita' di ricerca vettoriale.
4. Utilizzando un motore di ricerca come ElasticSearch, OpenSearch o Azure Search. Questa è la soluzione più complessa e ricca di risorse, ma anche la più potente. E 'anche il più scalabile e in grado di gestire le query complesse con facilità. Andrò in profondità straziante nella prossima settimana o giù di lì su come auto-ospitare, configurare e utilizzare OpenSearch da C#.

# Database Ricerca di testo completo con Postgres

In questo blog mi sono recentemente trasferito a utilizzare Postgres per il mio database. Postgres ha una funzione di ricerca del testo completa che è molto potente e (qualcosa) facile da usare. E 'anche molto veloce e in grado di gestire domande complesse con facilità.

Quando si costruisce young `DbContext` puoi specificare quali campi hanno la funzionalità di ricerca di testo piena abilitata.

Postgres utilizza il concetto di vettori di ricerca per raggiungere la ricerca veloce ed efficiente del testo completo. Un vettore di ricerca è una struttura dati che contiene le parole in un documento e le loro posizioni. Essenzialmente la precomputazione del vettore di ricerca per ogni riga nel database permette a Postgres di cercare le parole nel documento molto rapidamente.
Esso utilizza due tipi di dati speciali per raggiungere questo obiettivo:

- TSVector: uno speciale tipo di dati PostgreSQL che memorizza un elenco di lexemes (consideralo un vettore di parole). È la versione indicizzata del documento utilizzato per la ricerca veloce.
- TSQuery: Un altro tipo di dati speciale che memorizza la query di ricerca, che include i termini di ricerca e gli operatori logici (come AND, OR, NOT).

Inoltre offre una funzione di ranking che consente di classificare i risultati in base a come corrispondono alla query di ricerca. Questo è molto potente e consente di ordinare i risultati per rilevanza.
PostgreSQL assegna un ranking ai risultati in base alla pertinenza. La pertinenza è calcolata prendendo in considerazione fattori quali la vicinanza dei termini di ricerca tra di loro e la frequenza con cui essi appaiono nel documento.
Le funzioni ts_rank o ts_rank_cd sono utilizzate per calcolare questo ranking.

Puoi leggere di più sulle funzionalità di ricerca del testo completo di Postgres [qui](https://www.postgresql.org/docs/current/textsearch.html)

## Quadro dell'entità

Il pacchetto quadro per le entità di Postgres [qui](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) fornisce un supporto potente per la ricerca completa del testo. Consente di specificare quali campi sono interamente indicizzati e come interrogarli.

Per fare questo aggiungiamo specifici tipi di indice alle nostre Entità come definite in `DbContext`:

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

Qui stiamo aggiungendo un indice di testo completo al `Title` e `PlainTextContent` campi della nostra `BlogPostEntity`. Stiamo anche specificando che l'indice dovrebbe usare il `GIN` tipo di indice e `english` Linguaggio. Questo è importante in quanto dice a Postgres come indicizzare i dati e quale lingua usare per fermare le parole.

Questo è ovviamente un problema per il nostro blog in quanto abbiamo più lingue. Purtroppo per ora sto solo usando il `english` lingua per tutti i post. Questo è qualcosa che dovrò affrontare in futuro, ma per ora funziona abbastanza bene.

Aggiungiamo anche un indice al nostro `Category` entità:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Facendo questo Postgres genera un vettore di ricerca per ogni riga nel database. Questo vettore contiene le parole nel `Title` e `PlainTextContent` campi. Possiamo quindi usare questo vettore per cercare le parole nel documento.

Questo si traduce in una funzione to_tsvector in SQL che genera il vettore di ricerca per la riga. Possiamo quindi usare la funzione ts_rank per classificare i risultati in base alla pertinenza.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Applicare questo come migrazione al nostro database e siamo pronti per iniziare la ricerca.

# Ricerca

## Indice TsVector

Per la ricerca che usiamo useremo il `EF.Functions.ToTsVector` e `EF.Functions.WebSearchToTsQuery` funzioni per creare un vettore di ricerca e una query. Possiamo poi usare il `Matches` funzione per cercare la query nel vettore di ricerca.

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

La funzione EF.Functions.WebSearchToTsQuery genera la query per la riga basata sulla sintassi dei motori di ricerca web.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

In questo esempio si può vedere che questo genera una query che cerca le parole "gatto triste" o "rat grasso" nel documento. Questa è una caratteristica potente che ci permette di cercare domande complesse con facilità.

Come dichiarato befpre questi metodi generano sia il vettore di ricerca che la query per la riga. Poi usiamo il `Matches` funzione per cercare la query nel vettore di ricerca. Possiamo anche usare il `Rank` funzione per classificare i risultati in base alla pertinenza.

Come potete vedere questa non è una semplice query ma è molto potente e ci permette di cercare parole nel `Title`, `PlainTextContent` e `Category` campi della nostra `BlogPostEntity` e classificarli per rilevanza.

## WebAPI

Per utilizzarli (in futuro) possiamo creare un semplice endpoint WebAPI che prende una query e restituisce i risultati. Questo è un semplice controller che prende una query e restituisce i risultati:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(MostlylucidDbContext context) : ControllerBase
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

## Colonna generata e TypeAhead

Un approccio alternativo all'utilizzo di questi indici TsVector'semplici' è quello di usare una colonna generata per memorizzare il vettore di ricerca e quindi utilizzare questo per cercare. Si tratta di un approccio più complesso, ma consente prestazioni migliori.
Qui modifichiamo il nostro `BlogPostEntity` per aggiungere un tipo speciale di colonna:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Questa è una colonna calcolata che genera il vettore di ricerca per la riga. Possiamo quindi usare questa colonna per cercare le parole nel documento.

Abbiamo quindi impostato questo indice all'interno della nostra definizione di entità (ancora per confermare, ma questo può anche permetterci di avere più lingue specificando una colonna linguistica per ogni post).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Vedrete qui che usiamo `HasComputedColumnSql` specificare esplicitamente la funzione PostGreSQL per generare il vettore di ricerca. Specificamo inoltre che la colonna è memorizzata nel database. Questo è importante in quanto dice a Postgres di memorizzare il vettore di ricerca nel database. Questo ci permette di cercare le parole nel documento usando il vettore di ricerca.

Nel database questo ha generato questo per ogni riga, che sono i 'lexemes' nel documento e le loro posizioni:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### CercaAPI

Possiamo quindi usare questa colonna per cercare le parole nel documento. Possiamo usare il `Matches` funzione per cercare la query nel vettore di ricerca. Possiamo anche usare il `Rank` funzione per classificare i risultati in base alla pertinenza.

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

Vedrete qui che usiamo anche un costruttore di query diverso `EF.Functions.ToTsQuery("english", query + ":*")`  che ci permette di offrire una funzionalità di tipo TipoAhead (dove possiamo digitare ad es. 'gatto' e ottenere 'gatto', 'gatto', 'pilastro' ecc.).

Inoltre ci permette di semplificare la query post principale del blog per cercare semplicemente la query nel `SearchVector` colonna. Questa è una caratteristica potente che ci permette di cercare parole nel `Title`, `PlainTextContent`. Usiamo ancora l'indice che abbiamo mostrato sopra per il `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Poi usiamo il `Rank` funzione per classificare i risultati in base alla pertinenza in base alla query.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Questo ci permette di usare l'endpoint come segue, dove possiamo passare nelle prime lettere di una parola e recuperare tutti i post che corrispondono a quella parola:

È possibile visualizzare il [API in azione qui](https://www.mostlylucid.net/swagger/index.html) Cerca la `/api/SearchApi`. (Nota; Ho abilitato Swagger per questo sito in modo da poter vedere l'API in azione, ma la maggior parte del tempo questo dovrebbe essere riservato per l'Is Development()).

![API](searchapi.png?width=900&format=webp&quality=50)

In futuro aggiungerò una funzione TypeAhead alla casella di ricerca sul sito che utilizza questa funzionalità.

# In conclusione

Potete vedere che è possibile ottenere potenti funzionalità di ricerca utilizzando Postgres e Entity Framework. Tuttavia ha complessità e limitazioni che dobbiamo tenere in considerazione (come la cosa della lingua). Nella prossima parte coprirò come faremmo usando OpenSearch - che ha una tonnellata più di configurazione ma è più potente e scalabile.