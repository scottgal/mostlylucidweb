# Seq for ASP.NET Logging - Tragging with SerilogTracing

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Johdanto

Edellisessä osassa näytin, miten pystytetään [ASP.NET Core -sovelluksen avulla Seqille omatoiminen isännöinti ](/blog/selfhostingseq)...................................................................................................................................... Nyt kun se on pystytetty, on aika käyttää enemmän sen ominaisuuksia mahdollistaakseen täydellisemmän kirjauksen ja jäljittämisen uuden Seq-instanssin avulla.

[TÄYTÄNTÖÖNPANO

# Jäljitys

Jäljitys on kuin kirjautuminen ++, joka antaa sinulle lisätason tietoa siitä, mitä hakemuksessasi tapahtuu. Se on erityisen hyödyllinen, kun käytössä on hajautettu järjestelmä ja pyyntö pitää jäljittää useiden palveluiden kautta.
Tällä sivustolla käytän sitä ongelmien selvittämiseen nopeasti, mutta vain siksi, että kyseessä on harrastussivusto, se ei tarkoita, että luopuisin ammatillisista normeistani.

## Serilogin perustaminen

Jäljitys Serilogin kanssa on aika yksinkertaista käyttää [Serilogien jäljitys](https://github.com/serilog-tracing/serilog-tracing) paketti. Ensin sinun täytyy asentaa paketit:

Lisäämme tähän myös konsolin ja Seqin altaan.

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Konsolista on aina hyötyä vianetsintään, ja Seq on se, mitä varten olemme täällä. Seqissä on myös joukko "rikkauttajia", jotka voivat lisätä lokiin lisätietoja.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

Jotta voit käyttää näitä rikastusaineita, sinun täytyy lisätä ne omaan `Serilog` konfiguraatio `appsettings.json` Kansio. Sinun täytyy myös asentaa kaikki erilliset rikastusaineet Nugetin avulla.

Se on yksi Serilogin hyvistä ja pahoista puolista, ja lopulta asennetaan BUNCH-paketti, mutta se tarkoittaa, että siihen lisätään vain se, mitä tarvitaan, eikä vain yksi monolitiikkapaketti.
Tässä on minun.

![Serilog Enrichers](serilogenrichers.png)

Kaikkien näiden pommitusten myötä saan aika hyvän log-tuloksen Seqissä.

![Serilog Seq -virhe](serilogerror.png)

Tässä näet virheilmoituksen, pinojäljen, langan tunnistuksen, prosessin tunnistuksen ja prosessin nimen. Tämä kaikki on hyödyllistä tietoa, kun yrität löytää jotain ongelmaa.

Yksi asia on huomioitava, että olen asettanut `  "MinimumLevel": "Warning",` in my `appsettings.json` Kansio. Tämä tarkoittaa, että Seqiin kirjautuu vain varoituksia ja edellä mainittuja. Tästä on hyötyä, jotta ääni pysyy matalana tukissa.

Seqissä voit kuitenkin myös määritellä tämän Api-avaimella, joten sinulla voi olla `Information` Tai jos olet todella innoissasi `Debug`) kirjaus asetetaan tähän ja rajoitetaan sitä, mitä Seq todella vangitsee API-avaimella.

![Seq Api Key](apikey.png)

Huomaa: sinulla on edelleen sovellus päällä, voit myös tehdä tästä dynaamisempaa, jotta voit säätää lentotasoa. Katso [Seq-nielu ](https://github.com/datalust/serilog-sinks-seq)Tarkempia tietoja.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## Jäljitys

Nyt lisäämme Tracing, taas SerilogTracingin avulla se on aika yksinkertaista. Meillä on sama asetelma kuin ennenkin, mutta lisäämme uuden altaan jäljittämiseen.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Lisäämme myös ylimääräisen paketin, jotta voimme kirjata tarkemmat aspnet-ytimeen liittyvät tiedot.

### Asennettu `Program.cs`

Nyt voimme alkaa käyttää jäljitystä. Ensin meidän täytyy lisätä jäljitys meidän `Program.cs` Kansio.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Jäljityksessä käytetään työn yksikköä edustavaa käsitettä "Aktiviteetit". Voit aloittaa toiminnan, tehdä töitä ja sitten lopettaa sen. Tästä on hyötyä pyynnölle useiden palveluiden kautta.

Tässä tapauksessa lisäämme ylimääräistä jäljitystä HttpClient-pyyntöihin ja AspNetCore-pyyntöihin. Lisäämme myös: `TraceToSharedLogger` joka kirjautuu aktiviteettiin samalla metsurilla kuin muukin sovelluksemme.

## Jäljityksen käyttö palveluksessa

Nyt meillä on jäljitysjärjestelmä, jonka avulla voimme alkaa käyttää sitä sovelluksessamme. Tässä esimerkki palvelusta, jossa käytetään jäljitystä.

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

Tärkeät linjat ovat seuraavat:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Tämä aloittaa uuden "aktiivisuuden", joka on työn yksikkö. Siitä on hyötyä pyynnön seuraamisessa useiden palveluiden kautta.
Koska se on kääritty käytettyyn lausuntoon, se valmistuu ja hävitetään menetelmämme lopussa, mutta on hyvä käytäntö saada se selkeästi valmiiksi.

```csharp
            activity.Complete();
```

Poikkeuskäsittelyssämme saamme myös aktiviteetin päätökseen, mutta virhetasolla ja poikkeuksella. Tästä on hyötyä hakemuksessasi olevien ongelmien selvittämisessä.

## Jälkien käyttö

Nyt meillä on kaikki tämä järjestelmä, jonka avulla voimme alkaa käyttää sitä. Tässä esimerkki hakemuksessani olevasta jäljityksestä.

![Http Trace](httptrace.png)

Tämä näyttää yhden markkalaskun pylvään käännöksen. Voit nähdä useita vaiheita yhden viran ja kaikki HttpClient-pyynnöt ja ajoitukset.

Note I use Postgres for my database, toisin kuin SQL-palvelimella, npgsql-ajurilla on natiivituki jäljitykseen, joten voit saada erittäin hyödyllisiä tietoja tietokantakyselyistäsi, kuten SQL:stä, ajoituksista jne. Ne tallennetaan Seq:lle spaneina, ja ne näyttävät seuraavanlaisilta:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

Huomaat, että tämä sisältää melko paljon kaikkea, mitä sinun tarvitsee tietää kyselystä, SQL:n toteuttamasta, yhteysketjusta jne. Tämä kaikki on hyödyllistä tietoa, kun yrität löytää jotain ongelmaa. Tällaisessa pienemmässä sovelluksessa on vain mielenkiintoista, jaetussa sovelluksessa se on vankkaa kultatietoa ongelmien selvittämiseksi.

# Johtopäätöksenä

Olen vain raapaissut Tracingin pintaa, se on hieman intohimoisten kannattajien aluetta. Toivottavasti olen näyttänyt, kuinka yksinkertaista on päästä yksinkertaisella jäljityksellä Seq & Serilogin avulla ASP.NET Core -sovelluksiin. Näin saan paljon hyötyä tehokkaampien työkalujen, kuten Application Insightsin, hyödystä ilman Azuren kustannuksia (nämä asiat voivat mennä hukkaan, kun lokit ovat suuret).