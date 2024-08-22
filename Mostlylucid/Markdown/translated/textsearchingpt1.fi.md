# Tekstin haku kokonaisuudessaan (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Johdanto

Sisällön etsiminen on kriittinen osa mitä tahansa sisältöä painavia verkkosivuja. Se parantaa löytävyyttä ja käyttökokemusta. Tässä viestissä kirjoitan, miten lisäsin koko tekstin etsiessäni tätä sivustoa

[TÄYTÄNTÖÖNPANO

# Lähestymistavat

Tekstihakuun on monia tapoja, mm.

1. Tämä on suhteellisen helppo toteuttaa, mutta se ei skaalaudu hyvin, kun etsitään vain muistitietorakennetta (kuten listaa). Lisäksi se ei tue monimutkaisia kyselyitä ilman paljon työtä.
2. Käyttämällä tietokantaa, kuten SQL Server tai Postgres. Vaikka tämä toimii ja saa tukea lähes kaikilta tietokantatyypeiltä, se ei aina ole paras ratkaisu monimutkaisempiin tietorakenteisiin tai monimutkaisiin kyselyihin, mutta se on kuitenkin se, mitä tässä artikkelissa käsitellään.
3. Käyttämällä kevyttä hakutekniikkaa, kuten [Lucene](https://lucenenet.apache.org/) tai SQLite FTS. Tämä on kahden edellä mainitun ratkaisun välimaasto. Se on monimutkaisempaa kuin vain listan etsiminen, mutta vähemmän monimutkaista kuin täysi tietokantaratkaisu. Se on kuitenkin edelleen aika monimutkainen toteuttaa (etenkin datan nauttimisen osalta) eikä skaalaudu yhtä hyvin kuin täydellinen hakuratkaisu. Totuudessa monet muut hakutekniikat [käytä Lucenea konepellin alla ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) Se on hämmästyttävää vektorihakukykyä.
4. Käyttämällä hakukonetta, kuten ElastinenSearch, OpenSearch tai Azure Search. Tämä on monimutkaisin ja voimavaraintensiivisin ratkaisu, mutta myös tehokkain. Se on myös skaalautuvin ja osaa käsitellä monimutkaisia kyselyitä vaivattomasti. Menen seuraavan viikon aikana tuskalliseen syvyyteen sen suhteen, miten voin isännöidä, konfiguroida ja käyttää OpenSearchia C#:sta.

# Database Full Text Searching with Postgres

Tässä blogissa siirryin hiljattain käyttämään Postgresiä tietokantaani. Postgresissä on tekstihakuominaisuus, joka on erittäin tehokas ja (osittain) helppokäyttöinen. Se on myös erittäin nopea ja pystyy käsittelemään monimutkaisia kyselyitä vaivattomasti.

Nuoruutta rakennettaessa `DbContext` Voit tarkentaa, missä kentissä on koko tekstihakutoiminto käytössä.

Postgres käyttää hakuvektorien käsitettä saavuttaakseen nopean ja tehokkaan kokotekstihaun. Hakuvektori on datarakenne, joka sisältää dokumentin sanat ja niiden sijainnit. Pohjimmiltaan tietokannan jokaisen rivin hakuvektorin esikirjoittaminen mahdollistaa sen, että Postgres voi nopeasti etsiä sanoja asiakirjasta.
Tähän päästään kahdella erityisellä tietotyypillä:

- TSVector: Erityinen PostgreSQL-datatyyppi, joka tallentaa listan lekseistä (ajattele sitä sanojen vektorina). Se on pikahakuun käytetyn asiakirjan indeksoitu versio.
- TSQuery: Toinen erityinen tietotyyppi, joka tallentaa hakukyselyn, joka sisältää hakuehdot ja loogiset toimijat (kuten AND, OR, EI).

Lisäksi se tarjoaa ranking-toiminnon, jonka avulla voit arvioida tulokset sen perusteella, kuinka hyvin ne vastaavat hakukyselyä. Tämä on erittäin voimakas ja antaa mahdollisuuden tilata tulokset relevanssilla.
PostgreSQL luokittelee tulokset relevanssin perusteella. Merkitys lasketaan tarkastelemalla esimerkiksi hakuehtojen läheisyyttä toisiinsa ja sitä, kuinka usein ne näkyvät asiakirjassa.
Tämän rankingin laskemisessa käytetään ts_rank tai ts_rank_cd -toimintoja.

Postgresin tekstihakuominaisuuksista voit lukea lisää [täällä](https://www.postgresql.org/docs/current/textsearch.html)

## "Yhteenkuuluvuuskehys"

Postinvälitysyksikön kehyspaketti [täällä](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) Tarjoaa vahvan tuen koko tekstin etsimiselle. Sen avulla voit määritellä, mitkä kentät on indeksoitu täyteen tekstiin ja miten niitä voi tiedustella.

Tätä varten lisäämme tiettyjä indeksityyppejä yhteisöihimme sellaisina kuin ne on määritelty `DbContext`:

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

Tässä lisäämme koko tekstihakemiston `Title` sekä `PlainTextContent` kentät meidän `BlogPostEntity`...................................................................................................................................... Määrittelemme myös, että indeksin tulisi käyttää `GIN` Indeksityyppi ja `english` kieli. Tämä on tärkeää, sillä se kertoo Postgresille, miten tiedot indeksoidaan ja mitä kieltä käytetään sanojen tukahduttamiseen ja pysäyttämiseen.

Tämä on tietysti blogimme ongelma, koska meillä on useita kieliä. Valitettavasti tällä hetkellä käytän vain `english` kieli kaikille viroille. Tähän minun on puututtava tulevaisuudessa, mutta toistaiseksi se toimii riittävän hyvin.

Lisäämme myös indeksin `Category` yhteisö:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Tekemällä tämän Postgres luo hakuvektorin jokaiselle tietokannan riville. Tämä vektori sisältää sanat `Title` sekä `PlainTextContent` peltoja. Voimme sitten käyttää tätä vektoria etsiäksemme sanoja asiakirjasta.

Tämä kääntää SQL:n to_tsvector-toiminnon, joka luo rivin hakuvektorin. Sen jälkeen voimme käyttää ts_rank-toimintoa tulosten luokittelemiseen relevanssin perusteella.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Sovella tätä muuttoliikkeenä tietokantaamme, niin olemme valmiita aloittamaan etsinnät.

# Haetaan

## TsVektorin hakemisto

Käytössä oleva haku käyttää `EF.Functions.ToTsVector` sekä `EF.Functions.WebSearchToTsQuery` Toiminnot luoda hakuvektori ja kysely. Sitten voimme käyttää `Matches` Funktio hakua varten hakuvektorissa.

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

EF.Functions.WebSearchToTsQuery -toiminto tuottaa kyselyn riville yhteisen Web Search -koneen syntaksin perusteella.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

Tässä esimerkissä näet, että tämä synnyttää kyselyn, jossa etsitään asiakirjassa olevia sanoja "surukissa" tai "läski rotta". Tämä on tehokas ominaisuus, jonka avulla voimme helposti etsiä monimutkaisia kyselyitä.

Kuten on todettu, nämä menetelmät luovat sekä hakuvektorin että kyselyn riville. Sitten käytämme `Matches` Funktio hakua varten hakuvektorissa. Voimme käyttää myös `Rank` Toiminto, jolla tulokset luokitellaan relevanssin mukaan.

Kuten näette, tämä ei ole yksinkertainen kysely, mutta se on hyvin voimakas ja antaa meille mahdollisuuden etsiä sanoja `Title`, `PlainTextContent` sekä `Category` kentät meidän `BlogPostEntity` ja arvottaa ne relevanssin mukaan.

## WebAPI

Käyttääksemme näitä (tulevaisuudessa) voimme luoda yksinkertaisen WebAPI-päätteen, joka ottaa kyselyn ja palauttaa tulokset. Tämä on yksinkertainen ohjain, joka tekee kyselyn ja palauttaa tulokset:

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

## Luotu sarake ja typeAhead

Vaihtoehtoinen tapa käyttää näitä "yksinkertaisia" TsVector-indeksejä on käyttää luotua saraketta hakuvektorin tallentamiseen ja käyttää tätä hakuun. Tämä on monimutkaisempi lähestymistapa, mutta mahdollistaa paremman suorituksen.
Tässä me muokkaamme `BlogPostEntity` Lisätään erityinen sarake:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Tämä on laskettu sarake, joka luo rivin hakuvektorin. Tämän jälkeen voimme käyttää tätä palstaa etsiäksemme sanoja asiakirjasta.

Sen jälkeen olemme laatineet tämän hakemiston yhteisömääritelmämme sisällä (mitä voimme vielä vahvistaa, mutta tämä voi myös antaa meille mahdollisuuden saada useita kieliä määrittelemällä kielisarakkeen kutakin virkaa varten).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Näet tästä, että käytämme `HasComputedColumnSql` Tarkennetaan tarkemmin hakuvektorin luontitoiminto PostGreSQL-toiminnolla. Määrittelemme myös, että sarake on tallennettu tietokantaan. Tämä on tärkeää, koska se kertoo Postgresin tallentavan hakuvektorin tietokantaan. Näin voimme etsiä sanoja asiakirjasta hakuvektorin avulla.

Tietokantaan tämä tuotti tämän jokaiselle riville, jotka ovat asiakirjan "kirjaimet" ja niiden sijainnit:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### EtsiAPIsta

Tämän jälkeen voimme käyttää tätä palstaa etsiäksemme sanoja asiakirjasta. Voimme käyttää `Matches` Funktio hakua varten hakuvektorissa. Voimme käyttää myös `Rank` Toiminto, jolla tulokset luokitellaan relevanssin mukaan.

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

Huomaat, että käytämme myös eri kyselykonstruktoria. `EF.Functions.ToTsQuery("english", query + ":*")`  jonka avulla voimme tarjota TypeAhead-tyyppistä toimintoa (jossa voimme kirjoittaa esim. "kissa" ja "kissa", "kissa", "kissatoukka" jne.).

Lisäksi sen avulla voimme yksinkertaistaa blogikirjoituksen pääkyselyä vain etsiäksesi kyselyn `SearchVector` kolumni. Tämä on voimakas ominaisuus, jonka avulla voimme etsiä sanoja `Title`, `PlainTextContent`...................................................................................................................................... Käytämme edelleen indeksiä näytimme edellä varten `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Sitten käytämme `Rank` Funktio, jossa tulokset luokitellaan relevanssin mukaan kyselyn perusteella.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Näin voimme käyttää päätepistettä seuraavasti, jossa voimme välittää muutaman sanan alkukirjaimen ja saada takaisin kaikki viestit, jotka vastaavat tuota sanaa:

Voit katsoa [API toiminnassa täällä](https://www.mostlylucid.net/swagger/index.html) Etsi `/api/SearchApi`...................................................................................................................................... (Huomautus: Olen ottanut Swaggerin käyttöön tälle sivustolle, jotta näet API:n toiminnassa, mutta suurimman osan ajasta tämä pitäisi varata "IsDevelopment" () -ohjelmalle.

![API](searchapi.png?width=900&format=webp&quality=50)

Tulevaisuudessa lisään TypeAhead-ominaisuuden sivuston hakuruutuun, joka käyttää tätä toimintoa.

# Johtopäätöksenä

Voit nähdä, että on mahdollista saada tehokkaita hakutoimintoja Postgresin ja Entity Frameworkin avulla. Sillä on kuitenkin monimutkaisuuksia ja rajoituksia, jotka meidän on otettava huomioon (kuten kielijuttu). Seuraavassa osassa selvitän, miten tekisimme tämän OpenSearchin avulla, jossa on paljon enemmän asetelmaa, mutta joka on tehokkaampi ja skaalautuvampi.