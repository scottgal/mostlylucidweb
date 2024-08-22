# Volltextsuche (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Einleitung

Die Suche nach Inhalten ist ein kritischer Teil von Inhalten schwere Website. Es verbessert die Auffindbarkeit und Benutzererfahrung. In diesem Beitrag werde ich abdecken, wie ich hinzugefügt Volltext Suche nach dieser Website

[TOC]

# Ansätze

Es gibt eine Reihe von Möglichkeiten, um Volltextsuche zu tun, einschließlich

1. Durchsuchen einer in Speicherdatenstruktur (wie eine Liste), ist dies relativ einfach zu implementieren, aber nicht gut zu skalieren. Zusätzlich unterstützt es keine komplexen Abfragen ohne viel Arbeit.
2. Verwendung einer Datenbank wie SQL Server oder Postgres. Während dies funktioniert und hat Unterstützung von fast allen Datenbanktypen ist es nicht immer die beste Lösung für komplexere Datenstrukturen oder komplexe Abfragen, aber es ist, was dieser Artikel abdecken wird.
3. Verwendung einer leichten Suchtechnologie wie [L 347 vom 20.12.2013, S. 671.](https://lucenenet.apache.org/) oder SQLite FTS. Dies ist ein Mittelweg zwischen den beiden oben genannten Lösungen. Es ist komplexer als nur die Suche nach einer Liste, aber weniger komplex als eine vollständige Datenbanklösung. Allerdings ist es immer noch ziemlich komplex zu implementieren (vor allem für die Aufnahme von Daten) und skaliert nicht so gut wie eine vollständige Suchlösung. In Wahrheit viele andere Suchtechnologien [Verwenden Sie Lucene unter der Haube für ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) Es ist erstaunlich Vektorsuche Fähigkeiten.
4. Verwenden einer Suchmaschine wie ElasticSearch, OpenSearch oder Azure Search. Dies ist die komplexeste und ressourcenintensivste Lösung, aber auch die leistungsstärkste. Es ist auch die skalierbarste und kann komplexe Abfragen mit Leichtigkeit handhaben. Ich werde in der nächsten Woche oder so in qualvolle Tiefe gehen, wie ich mich selbst hoste, konfigurieren und OpenSearch von C# verwenden kann.

# Datenbank Volltextsuche mit Postgres

In diesem Blog bin ich vor kurzem zu Postgres für meine Datenbank umgezogen. Postgres hat eine Volltextsuche Funktion, die sehr leistungsfähig und (etwas) einfach zu bedienen ist. Es ist auch sehr schnell und kann komplexe Abfragen mit Leichtigkeit handhaben.

Beim Bau von Yout `DbContext` Sie können festlegen, welche Felder Volltextsuche aktiviert haben.

Postgres nutzt das Konzept der Suchvektoren, um eine schnelle, effiziente Volltextsuche zu erreichen. Ein Suchvektor ist eine Datenstruktur, die die Wörter in einem Dokument und deren Positionen enthält. Im Wesentlichen vorkomputiert der Suchvektor für jede Zeile in der Datenbank ermöglicht Postgres, sehr schnell nach Wörtern im Dokument zu suchen.
Um dies zu erreichen, nutzt es zwei spezielle Datentypen:

- TSVector: Ein spezieller PostgreSQL-Datentyp, der eine Liste von Lexemen speichert (denk daran als Vektor von Wörtern). Es ist die indizierte Version des Dokuments, das für die schnelle Suche verwendet wird.
- TSQuery: Ein weiterer spezieller Datentyp, der die Suchanfrage speichert, einschließlich der Suchbegriffe und logischen Operatoren (wie AND, OR, NOT).

Zusätzlich bietet es eine Ranking-Funktion, die Ihnen erlaubt, die Ergebnisse zu ordnen, basierend darauf, wie gut sie mit der Suchanfrage übereinstimmen. Dies ist sehr leistungsfähig und ermöglicht es Ihnen, die Ergebnisse nach Relevanz zu bestellen.
PostgreSQL weist den Ergebnissen anhand der Relevanz ein Ranking zu. Bedeutung wird berechnet, indem Faktoren wie die Nähe der Suchbegriffe zueinander und wie oft sie im Dokument erscheinen berücksichtigt werden.
Die Funktionen ts_rank oder ts_rank_cd werden verwendet, um dieses Ranking zu berechnen.

Lesen Sie mehr über die Volltextsuche von Postgres [Hierher](https://www.postgresql.org/docs/current/textsearch.html)

## Rahmen für die Einrichtung

Das Postgres Entity Framework Paket [Hierher](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) bietet leistungsstarke Unterstützung für die Volltextsuche. Sie können festlegen, welche Felder Volltextindexiert sind und wie Sie sie abfragen können.

Um dies zu tun, fügen wir spezifische Indextypen zu unseren Entities nach Definition in `DbContext`:

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

Hier fügen wir einen Volltextindex zum `Title` und `PlainTextContent` Bereiche unserer `BlogPostEntity`......................................................................................................... Wir spezifizieren auch, dass der Index sollte die `GIN` index type und die `english` Sprache. Dies ist wichtig, da es Postgres sagt, wie man die Daten indexiert und welche Sprache man zum Anhalten und Stoppen von Wörtern verwendet.

Dies ist offensichtlich ein Thema für unseren Blog, da wir mehrere Sprachen haben. Leider im Moment bin ich nur mit dem `english` Sprache für alle Beiträge. Das ist etwas, das ich in der Zukunft ansprechen muss, aber für den Moment funktioniert es gut genug.

Wir fügen auch einen Index zu unserem `Category` Einrichtung:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Dadurch erzeugt Postgres einen Suchvektor für jede Zeile in der Datenbank. Dieser Vektor enthält die Wörter in der `Title` und `PlainTextContent` ........................................................................................................................................ Wir können dann diesen Vektor verwenden, um nach Wörtern im Dokument zu suchen.

Dies bedeutet eine to_tsvector-Funktion in SQL, die den Suchvektor für die Zeile generiert. Wir können dann die ts_rank-Funktion verwenden, um die Ergebnisse anhand der Relevanz zu ordnen.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Wenden Sie dies als Migration auf unsere Datenbank an und wir sind bereit, mit der Suche zu beginnen.

# Suchen

## TsVector Index

Um zu suchen, verwenden wir die `EF.Functions.ToTsVector` und `EF.Functions.WebSearchToTsQuery` Funktionen, um einen Suchvektor und Abfrage zu erstellen. Wir können dann die `Matches` Funktion zur Suche nach der Abfrage im Suchvektor.

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

Die Funktion EF.Functions.WebSearchToTsQuery generiert die Abfrage für die Zeile basierend auf der gemeinsamen Syntax der Web Search Engine.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

In diesem Beispiel sehen Sie, dass dies eine Abfrage erzeugt, die nach den Wörtern "Sad Cat" oder "Fettratte" im Dokument sucht. Dies ist eine leistungsstarke Funktion, die es uns ermöglicht, komplexe Abfragen mit Leichtigkeit zu suchen.

Wie angegeben erzeugen befpre diese Methoden sowohl den Suchvektor als auch die Abfrage für die Zeile. Wir benutzen dann die `Matches` Funktion zur Suche nach der Abfrage im Suchvektor. Wir können auch die `Rank` Funktion, um die Ergebnisse nach Relevanz zu ordnen.

Wie Sie sehen können, ist dies keine einfache Abfrage, aber es ist sehr leistungsfähig und ermöglicht es uns, nach Wörtern in der Suche `Title`, `PlainTextContent` und `Category` Bereiche unserer `BlogPostEntity` und ordnen diese nach Relevanz.

## WebAPI

Um diese (in Zukunft) zu nutzen, können wir einen einfachen WebAPI-Endpunkt erstellen, der eine Abfrage benötigt und die Ergebnisse zurückgibt. Dies ist ein einfacher Controller, der eine Abfrage annimmt und die Ergebnisse zurückgibt:

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

## Erzeugte Säule und TypeAhead

Ein alternativer Ansatz zur Verwendung dieser 'einfachen' TsVector-Indizes besteht darin, eine generierte Spalte zu verwenden, um den Suchvektor zu speichern und diese dann zur Suche zu verwenden. Dies ist ein komplexerer Ansatz, ermöglicht aber eine bessere Leistung.
Hier ändern wir unsere `BlogPostEntity` um eine spezielle Art von Spalte hinzuzufügen:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Dies ist eine berechnete Spalte, die den Suchvektor für die Zeile generiert. Mit dieser Spalte können wir dann nach Wörtern im Dokument suchen.

Wir richten diesen Index dann innerhalb unserer Entity Definition ein (noch zu bestätigen, aber dies kann uns auch erlauben, mehrere Sprachen zu haben, indem wir eine Sprachspalte für jeden Beitrag angeben).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Sie werden hier sehen, dass wir `HasComputedColumnSql` zur expliziten Angabe der PostGreSQL-Funktion, um den Suchvektor zu generieren. Wir geben auch an, dass die Spalte in der Datenbank gespeichert ist. Dies ist wichtig, da Postgres den Suchvektor in der Datenbank speichern soll. So können wir mit dem Suchvektor nach Wörtern im Dokument suchen.

In der Datenbank generierte dies für jede Zeile, die die 'Lexeme' im Dokument und ihre Positionen sind:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### SuchAPI

Mit dieser Spalte können wir dann nach Wörtern im Dokument suchen. Wir können die `Matches` Funktion zur Suche nach der Abfrage im Suchvektor. Wir können auch die `Rank` Funktion, um die Ergebnisse nach Relevanz zu ordnen.

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

Sie sehen hier, dass wir auch einen anderen Abfrage-Konstruktor verwenden. `EF.Functions.ToTsQuery("english", query + ":*")`  die es uns ermöglicht, eine TypeAhead-Funktionalität anzubieten (wo wir z.B. tippen können). 'Katze' und 'Katze', 'Katze', 'Katze' usw.).

Zusätzlich ermöglicht es uns, die Haupt-Blog-Post-Abfrage zu vereinfachen, um nur die Suche nach der Abfrage in der `SearchVector` Spalte. Dies ist eine leistungsfähige Funktion, die es uns ermöglicht, nach Wörtern in der Suche `Title`, `PlainTextContent`......................................................................................................... Wir verwenden immer noch den Index, den wir oben für die `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Wir benutzen dann die `Rank` Funktion, um die Ergebnisse nach Relevanz basierend auf der Abfrage zu ordnen.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Damit können wir den Endpunkt wie folgt verwenden, wo wir in den ersten Buchstaben eines Wortes passieren können und alle Beiträge zurückbekommen, die mit diesem Wort übereinstimmen:

Sie können die [API in Aktion hier](https://www.mostlylucid.net/swagger/index.html) suchen für die `/api/SearchApi`......................................................................................................... (Anmerkung; Ich habe Swagger für diese Seite aktiviert, damit Sie die API in Aktion sehen können, aber meistens sollte dies für `IsDevelopment() reserviert sein).

![API](searchapi.png?width=900&format=webp&quality=50)

In Zukunft werde ich eine TypeAhead-Funktion zum Suchfeld auf der Website hinzufügen, die diese Funktionalität nutzt.

# Schlussfolgerung

Sie können sehen, dass es möglich ist, leistungsstarke Suchfunktionen mit Postgres und Entity Framework zu erhalten. Allerdings hat es Komplexitäten und Einschränkungen, die wir berücksichtigen müssen (wie die Sprache Sache). Im nächsten Teil werde ich abdecken, wie wir dies mit OpenSearch - das ist eine Tonne mehr Setup, sondern ist leistungsfähiger und skalierbar.