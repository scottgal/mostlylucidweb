# Volledige tekst zoeken (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Inleiding

In de [laatste artikel](/blog/textsearchingpt1) Ik heb je laten zien hoe je een full text zoekopdracht kunt instellen met behulp van de ingebouwde full text zoekmogelijkheden van Postgres. Terwijl ik een zoek api ontmaskerde, had ik geen manier om het te gebruiken, dus... het was een beetje een plaag. In dit artikel laat ik je zien hoe je de zoekapi gebruikt om naar tekst te zoeken in je database.

Dit zal een beetje zoekvak toevoegen aan de header van de site die gebruikers zal toestaan om te zoeken naar tekst in de blog berichten.

![Zoeken](searchbox.png?format=webp&quality=25)

**Opmerking: De olifant in de kamer is dat ik niet de beste manier om dit te doen. Om multi-taal te ondersteunen is super complex (ik zou een andere kolom per taal nodig hebben) en ik zou moeten omgaan met afstammelingen en andere taal specifieke dingen. Ik ga dit nu negeren en me focussen op Engels. Straks laten we zien hoe we dit moeten aanpakken in OpenSearch.**

[TOC]

## Zoeken naar tekst

Om een zoekfunctie toe te voegen moest ik enkele wijzigingen aanbrengen in de zoekapi. Ik voegde handling voor zinnen met behulp van de `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Dit wordt optioneel gebruikt als er ruimte is in de query

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

Anders gebruik ik de bestaande zoekmethode die het prefix-teken bijvoegt.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Zoekopdracht

Gebruik [Alpine.jsunit synonyms for matching user input](https://alpinejs.dev/) Ik maakte een eenvoudige gedeeltelijke controle die een super eenvoudige zoekvak biedt.

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

Dit heeft een heleboel CSS klassen om correct te renderen voor zowel donker als licht modus. De Alpine.js code is vrij eenvoudig. Het is een eenvoudige typahead control die de zoek api aanroept wanneer de gebruiker in het zoekvak typt.
We hebben ook een kleine code om onfocus aan te pakken om de zoekresultaten te sluiten.

```html
   x-on:click.outside="results = []"
```

Merk op dat we hier een debounce hebben om te voorkomen dat we de server hameren met verzoeken.

## Het Typeahead JS

Dit roept op tot onze JS functie (gedefinieerd in `src/js/main.js`)

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

Zoals je kunt zien is dit vrij eenvoudig (veel van de complexiteit is het hanteren van de op en neer toetsen om resultaten te selecteren).
Deze berichten naar onze `SearchApi`
Wanneer een resultaat wordt geselecteerd navigeren we naar de url van het resultaat.

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

Ik veranderde ook de fetch om te werken met HTMX, dit verandert gewoon de `search` methode om een HTMX-verversing te gebruiken:

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

Merk op dat we de innerlijkeHTML van de `contentcontainer` met het resultaat van de zoektocht. Dit is een eenvoudige manier om de inhoud van de pagina bij te werken met het zoekresultaat zonder een paginaverversing.
We veranderen ook de url in de geschiedenis naar de nieuwe url.

## Conclusie

Dit voegt een krachtige maar eenvoudige zoekfunctie aan de site. Het is een geweldige manier om gebruikers te helpen vinden wat ze zoeken.
Het geeft deze site een meer professioneel gevoel en maakt het gemakkelijker om te navigeren.