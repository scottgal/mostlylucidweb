# Fullständig textsökning (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Inledning

I och med att [förra artikeln](/blog/textsearchingpt1) Jag visade dig hur du konfigurerar en fulltextsökning med hjälp av Postgres inbyggda sökfunktioner i fulltext. Medan jag avslöjade en sökning api jag inte hade ett sätt att faktiskt använda det så... det var lite av en retas. I den här artikeln ska jag visa dig hur du använder sökapi för att söka efter text i din databas.

Detta kommer att lägga till en liten sökruta till rubriken på webbplatsen som gör det möjligt för användare att söka efter text i blogginlägg.

![Sök](searchbox.png?format=webp&quality=25)

**Observera: Elefanten i rummet är att jag inte anser det bästa sättet att göra detta. Att stödja flera språk är super komplext (jag skulle behöva en annan kolumn per språk) och jag skulle behöva hantera reverserande och andra språk specifika saker. Jag ska ignorera det här och fokusera på engelska. Senare visar vi hur man hanterar detta i OpenSearch.**

[TOC]

## Söker efter text

För att lägga till en sökförmåga var jag tvungen att göra några ändringar i sökapi. Jag lade till hantering för fraser med hjälp av `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Detta används valfritt när det finns ett utrymme i frågan

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

Annars använder jag den befintliga sökmetoden som lägger till prefixet.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Sökkontroll

Användning [Alpina.js](https://alpinejs.dev/) Jag gjorde en enkel Partiell kontroll som ger en super enkel sökruta.

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

Detta har en massa CSS-klasser att göra korrekt för antingen mörkt eller ljust läge. Alpin.js-koden är ganska enkel. Det är en enkel typeahead-kontroll som ringer sökapi när användaren skriver in i sökrutan.
Vi har också en liten kod att hantera unfocus för att stänga sökresultaten.

```html
   x-on:click.outside="results = []"
```

Observera att vi har en debounce här för att undvika att hamra servern med förfrågningar.

## Typeahead JS

Detta kallar in vår JS-funktion (definierad i `src/js/main.js`)

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

Som du kan se är detta ganska enkelt (mycket av komplexiteten är att hantera upp och ner nycklar för att välja resultat).
Detta inlägg till vår `SearchApi`
När ett resultat är valt navigerar vi till url av resultatet.

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

### HTMX Ordförande

Jag ändrade också hämta för att arbeta med HTMX, detta ändrar helt enkelt `search` metod för att använda en HTMX-uppdatering:

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

Observera att vi byter den inreHTML av `contentcontainer` med resultatet av sökningen. Detta är ett enkelt sätt att uppdatera innehållet på sidan med sökresultatet utan en sida uppdatera.
Vi byter också url i historien till den nya url.

## Slutsatser

Detta lägger till en kraftfull men ändå enkel sökförmåga till webbplatsen. Det är ett bra sätt att hjälpa användare att hitta det de letar efter.
Det ger denna webbplats en mer professionell känsla och gör det lättare att navigera.