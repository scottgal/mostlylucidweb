# Tekstihaku kokonaisuudessaan (Pt 3 - OpenSearch with ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Johdanto

Sarjan edellisissä osissa esitimme täydellisen tekstinhaun konseptin ja sen, miten sitä voidaan käyttää tekstin hakemiseen tietokannasta. Tässä osassa esittelemme, miten OpenSearch käytetään ASP.NET Corella.

Aiemmat osat:

- [Täydellinen tekstihaku postinjakajilla](/blog/textsearchingpt1)
- [Hakulaatikko, jossa postgres](/blog/textsearchingpt11)
- [Johdatus avoimeen hakuun](/blog/textsearchingpt2)

Tässä osassa kerromme, miten voit alkaa käyttää uutta kiiltävää OpenSearch-instanssia ASP.NET Corella.

[TÄYTÄNTÖÖNPANO

## Asetukset

Kun OpenSearch-instanssi on saatu käyntiin, voimme aloittaa vuorovaikutuksen sen kanssa. Me käytämme... [Avoimen etsinnän asiakas](https://opensearch.org/docs/latest/clients/OSC-dot-net/) .NET-verkolle.
Ensin asetimme asiakkaan Setup-laajennukseen

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Näin asiakas saa päätepisteen ja valtakirjan. Käytämme myös debug-tilaa, jotta näemme, mitä on meneillään. Lisäksi, koska emme käytä Real SSL -sertifikaatteja, poistamme sertifikaattien validoinnin käytöstä (älä tee tätä tuotannossa).

## Hakemistotiedot

OpenSearchin ydinkonsepti on Index. Ajattele Hakemistoa kuin tietokantataulukkoa, johon kaikki tietosi tallennetaan.

Tätä varten käytämme [Avoimen etsinnän asiakas](https://opensearch.org/docs/latest/clients/OSC-dot-net/) .NET-verkolle. Voit asentaa tämän NuGetin kautta:

Siellä on kaksi: Opensearch.Net ja Opensearch.Client. Ensimmäinen on matalatasoiset jutut, kuten yhteyksien hallinta, toinen korkeatasoiset jutut, kuten indeksointi ja etsiminen.

Nyt kun se on asennettu, voimme alkaa tutkia indeksointitietoja.

Indeksin luominen on puolisuoraa. Määrittelet vain, miltä indeksin pitäisi näyttää, ja sitten luot sen.
Alla olevassa koodissa näet, että "kartoitamme" Index Modelia (yksinkertaistettu versio blogin tietokantamallista).
Tämän mallin jokaiselle kentälle määrittelemme sitten, minkä tyyppinen se on (teksti, päivämäärä, avainsana jne.) ja mitä analysaattoria tulee käyttää.

Tyyppi on tärkeä, koska se määrittelee, miten tiedot tallennetaan ja miten niitä voidaan etsiä. Esimerkiksi "tekstikenttää" analysoidaan ja kuitataan, "avainsana" kenttää ei. Voit siis odottaa etsiväsi avainsanakenttää juuri sellaisena kuin se on tallennettuna, mutta tekstikenttää voit etsiä tekstin osista.

Myös tässä kategoriat ovat itse asiassa merkkijono[] mutta avainsanatyyppi ymmärtää, miten niitä voi käsitellä oikein.

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

## Kohteiden lisääminen hakemistoon

Kun olemme saaneet indeksimme valmiiksi lisätäksemme siihen kohteita, meidän on lisättävä tähän indeksiin eriä. Kun lisäämme BUNCH:ia, käytämme irtotavarana inserttimenetelmää.

Huomaat, että kutsumme aluksi metodiksi nimeltä`GetExistingPosts` joka palauttaa kaikki jo indeksissä olevat virat. Ryhmittelemme viestit sitten kielellä ja suodatamme pois "uk"-kielen (koska emme halua indeksoida, että koska se tarvitsee ylimääräisen lisäosan, meillä ei ole vielä). Sen jälkeen suodatamme pois kaikki hakemistossa jo olevat viestit.
Käytämme hasista ja tunnistetta tunnistaaksemme, onko viesti jo indeksissä.

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

Kun olemme suodattaneet pois olemassa olevat viestit ja puuttuva analysaattorimme, luomme uuden hakemiston (joka perustuu nimeen, minun tapauksessani "lähinnä "lylucid-blogi-<language>") ja luoda sitten suuri pyyntö. Tämä laajamittainen pyyntö on kokoelma hakemistoon tehtäviä toimintoja.
Tämä on tehokkaampaa kuin kunkin kohteen lisääminen yksi kerrallaan.

Huomaat sen myöhemmin. `BulkRequest` Me asetamme `Refresh` kiinteistöä `true`...................................................................................................................................... Tämä tarkoittaa, että insertin valmistuttua indeksi päivittyy. Tämä ei ole tarpeen, mutta siitä on hyötyä vianetsintään.

## Hakemistoa etsitään

Hyvä tapa testata, mitä täällä on oikeasti luotu, on mennä Dev Toolsin OpenSearch Dashboardsiin ja tehdä hakukysely.

```json
GET /mostlylucid-blog-*
{}
```

Tämä kysely palauttaa meille kaikki malliin sopivat indeksit `mostlylucid-blog-*`...................................................................................................................................... (niin kaikki indeksit tähän mennessä).

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

Dev Tools OpenSearch Dashboardsissa on hyvä tapa testata kyselyjäsi ennen kuin laitat ne koodiisi.

![Dev-työkalut](devtools.png?width=900&quality=25)

## Hakemistoa etsitään

Nyt voimme alkaa etsiä hakemistoa. Voimme käyttää `Search` tapa, jolla asiakas tekee tämän.
Tässä OpenSearchin todellinen voima tulee kuvaan. Se on kirjaimellisesti [kymmeniä erityyppisiä kyselyitä](https://opensearch.org/docs/latest/query-dsl/) dataa voi käyttää hakuun. Kaikki yksinkertaisesta avainsanahausta monimutkaiseen "hermostohakuun".

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

### Kyselyn kuvaus

Tämä menetelmä, `GetSearchResults`, on suunniteltu tiedustelemaan tiettyä OpenSearch-indeksiä blogikirjoitusten noutamiseksi. Siihen tarvitaan kolme muuttujaa: `language`, `query`, ja paginaatioparametrit `page` sekä `pageSize`...................................................................................................................................... Näin se toimii:

1. **Hakemistovalinta**:
   
   - Se noutaa indeksinimen `GetBlogIndexName` käytettyyn kieleen perustuva menetelmä. Indeksi valitaan dynaamisesti kielen mukaan.

2. **Etsi kyselystä**:
   
   - Kyselyssä käytetään a `Bool` query with a `Must` lauseke, jolla varmistetaan, että tulokset vastaavat tiettyjä kriteerejä.
   - Sisällä `Must` lauseke, a `MultiMatch` Kyselyä käytetään useiden kenttien etsimiseen (`Title`, `Categories`, ja `Content`).
     - **Tehostaminen**: `Title` Kenttä saa lisäpotkua `2.0`, mikä tekee sen tärkeämmäksi etsinnöissä, ja `Categories` on piristysruiske `1.5`...................................................................................................................................... Tämä tarkoittaa, että dokumentit, joissa hakupyyntö näkyy otsikossa tai kategorioissa, sijoittuvat korkeammalle.
     - **Kyselyn tyyppi**: Se käyttää `BestFields`, joka yrittää löytää parhaan sopivan kentän kyselylle.
     - **Huolimattomuus**: `Fuzziness.Auto` Parametri mahdollistaa likimääräiset osumat (esim. pienten kirjoitusvirheiden käsittelyn).

3. **Paginaatio**:
   
   - Erytropoietiini `Skip` menetelmä jättää ensimmäisen väliin `n` tulokset sivun numerosta riippuen laskettuna seuraavasti: `(page - 1) * pageSize`...................................................................................................................................... Tämä auttaa navigoimaan paginoitujen tulosten kautta.
   - Erytropoietiini `Size` menetelmä rajoittaa annettuihin asiakirjoihin palautettujen asiakirjojen määrää `pageSize`.

4. **Virheiden käsittely**:
   
   - Jos kysely epäonnistuu, virhe kirjataan ja tyhjä lista palautetaan.

5. **Tulos**:
   
   - Menetelmä palauttaa listan `BlogIndexModel` dokumentit, jotka vastaavat hakukriteereitä.

Voimme siis olla superjoustavia sen suhteen, miten tutkimme tietojamme. Voimme etsiä tiettyjä kenttiä, voimme lisätä tiettyjä kenttiä, voimme jopa etsiä useita indeksejä.

Yksi iso etu on helppous qith, jolla voimme tukea useita kieliä. Meillä on eri hakemisto jokaiselle kielelle ja mahdollistamme etsimisen kyseisen hakemiston sisällä. Tämä tarkoittaa, että voimme käyttää oikeaa analysaattoria jokaiselle kielelle ja saada parhaat tulokset.

## Uusi hakurajapinta

Toisin kuin hakurajapinta, jonka näimme sarjan edellisissä osissa, voimme yksinkertaistaa hakuprosessia huomattavasti OpenSearchin avulla. Voimme vain lähettää tekstiviestejä tähän kyselyyn ja saada hyviä tuloksia takaisin.

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

Kuten näette, meillä on kaikki tiedot, joita tarvitsemme indeksissä tulosten palauttamiseksi. Voimme sitten käyttää tätä luodaksemme URL-osoitteen blogikirjoitukseen. Tämä vie kuorman tietokannastamme ja nopeuttaa hakuprosessia huomattavasti.

## Johtopäätöksenä

Tässä viestissä näimme, miten C#-asiakas kirjoitetaan vuorovaikutukseen OpenSearch-instanssin kanssa.