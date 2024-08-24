# Tekstin haku kokonaisuudessaan (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20-30</datetime>

## Johdanto

• • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • [viimeinen artikkeli](/blog/textsearchingpt1) Näytin sinulle, miten pystytät koko tekstihaun käyttäen Postgresin koko tekstihakukykyä. Kun paljastin hakuapin, en voinut käyttää sitä, joten se oli kiusallista. Tässä artikkelissa näytän, miten hakuapin avulla voit hakea tekstiä tietokannastasi.

Aikaisemmat osat tässä sarjassa:

- [Täydellinen tekstihaku postinjakajilla](/blog/textsearchingpt1)

Seuraavat osat tässä sarjassa:

- [Johdatus avoimeen hakuun](/blog/textsearchingpt2)
- [Avaa haku C#:llä](/blog/textsearchingpt3)

Tämä lisää sivuston otsikkoon pienen hakulaatikon, jonka avulla käyttäjät voivat hakea tekstiä blogikirjoituksista.

![Etsi](searchbox.png?format=webp&quality=25)

**Huomaa: Norsu huoneessa on, että en pidä sitä parhaana tapana. Monikielisyyden tukeminen on superkompleksista (tarvitsen eri palstan kieltä kohti) ja minun pitäisi käsitellä juurruttavia ja muita kielikohtaisia asioita. Jätän tämän toistaiseksi huomiotta ja keskityn vain englantiin. Myöhemmin näytämme, miten tämä hoidetaan OpenSearchissa.**

[TOC]

## Haetaan tekstiä

Lisätäkseni hakukykyä jouduin tekemään muutoksia hakuapiin. Lisäsin fraasien käsittelyn `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

```csharp
    private async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
        var processedQuery = query;
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)) // Use precomputed SearchVector for title and content
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)))) // Search in categories
                && x.LanguageEntity.Name == "en")// Filter by language
            
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", processedQuery))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug,  })
            .Take(5)
            .ToListAsync();
        return posts.Select(x=> (x.Title, x.Slug)).ToList();
    }
```

Tätä käytetään valinnaisesti, kun kyselyssä on tilaa

```csharp
    if (!query.Contains(" "))
        {
            posts = await GetSearchResultForComplete(query);
        }
        else
        {
            posts = await GetSearchResultForQuery(query);
        }
```

Muuten käytän olemassa olevaa hakumenetelmää, joka liittää etuliitteen merkkiin.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Etsinnän hallinta

Käyttäminen [Alppi.js](https://alpinejs.dev/) Tein yksinkertaisen Osittaisen ohjauksen, joka tarjoaa super yksinkertaisen hakuruudun.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative"    x-on:click.outside="results = []">

    <label class="input input-sm dark:bg-custom-dark-bg bg-white input-bordered flex items-center gap-2">
       
        
        <input
            type="text"
            x-model="query"

            x-on:input.debounce.300ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow  input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-b-neutral-600 dark:border-gray-300   rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                    'dark:hover:bg-blue-dark hover:bg-blue-light': true
                }"
                class="cursor-pointer text-sm p-2 m-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

Tässä on joukko CSS-luokkia, jotka on tehtävä oikein joko pimeälle tai valolle. Alpine.js-koodi on aika yksinkertainen. Se on yksinkertainen tyyppikytkentä, joka kutsuu hakua apiksi, kun hakuruudussa on käyttäjätyyppejä.
Meillä on myös pieni koodi, jonka avulla voimme sulkea hakutulokset.

```html
   x-on:click.outside="results = []"
```

Huomaa, että täällä on debounce, jotta palvelinta ei moukaroitaisi pyynnöillä.

## Typeahead JS

Tämä vie JS-toimintoomme (määritelty `src/js/main.js`)

```javascript
window.mostlylucid = window.mostlylucid || {};

window.mostlylucid.typeahead = function () {
    return {
        query: '',
        results: [],
        highlightedIndex: -1, // Tracks the currently highlighted index

        search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },

        moveDown() {
            if (this.highlightedIndex < this.results.length - 1) {
                this.highlightedIndex++;
            }
        },

        moveUp() {
            if (this.highlightedIndex > 0) {
                this.highlightedIndex--;
            }
        },

        selectHighlighted() {
            if (this.highlightedIndex >= 0 && this.highlightedIndex < this.results.length) {
                this.selectResult(this.results[this.highlightedIndex]);
            }
        },

        selectResult(result) {
           window.location.href = result.url;
            this.results = []; // Clear the results
            this.highlightedIndex = -1; // Reset the highlighted index
        }
    }
}
```

Kuten näette, tämä on melko yksinkertaista (suuri osa monimutkaisuudesta on ylös- ja alas-avainten käsitteleminen tulosten valitsemiseksi).
Tämä viesti on meidän `SearchApi`
Kun tulos on valittu, navigoimme tuloksen urliin.

```javascript
     search() {
            if (this.query.length < 2) {
                this.results = [];
                this.highlightedIndex = -1;
                return;
            }

            fetch(`/api/search/${encodeURIComponent(this.query)}`)
                .then(response => response.json())
                .then(data => {
                    this.results = data;
                    this.highlightedIndex = -1; // Reset index on new search
                });
        },
```

### HTMX

Vaihdoin myös noudon toimimaan HTMX:n kanssa, tämä vain muuttaa `search` HTMX-päivityksen käyttötapa:

```javascript
    selectResult(result) {
    htmx.ajax('get', result.url, {
        target: '#contentcontainer',  // The container to update
        swap: 'innerHTML', // Replace the content inside the target
    }).then(function() {
        history.pushState(null, '', result.url); // Push the new url to the history
    });

    this.results = []; // Clear the results
    this.highlightedIndex = -1; // Reset the highlighted index
    this.query = ''; // Clear the query
}
```

Huomaa, että vaihdamme HTML:n `contentcontainer` hakutuloksella. Tämä on yksinkertainen tapa päivittää sivun sisältöä hakutuloksella ilman sivun päivitystä.
Muutamme myös historian urlin uudeksi urliksi.

## Johtopäätöksenä

Tämä lisää sivuston tehokasta mutta yksinkertaista hakukykyä. Se on loistava tapa auttaa käyttäjiä löytämään etsimänsä.
Se antaa tälle sivustolle ammattimaisemman tuntemuksen ja helpottaa navigointia.