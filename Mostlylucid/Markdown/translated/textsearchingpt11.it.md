# Ricerca completa del testo (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Introduzione

Nella [ultimo articolo](/blog/textsearchingpt1) Vi ho mostrato come impostare una ricerca di testo completo utilizzando le funzionalità di ricerca di testo completo di Postgres. Mentre esponevo una pipi' di ricerca, non avevo un modo per usarla, quindi... era un po' una presa in giro. In questo articolo vi mostrerò come usare l'api di ricerca per cercare il testo nel vostro database.

Questo aggiungerà una piccola casella di ricerca all'intestazione del sito che permetterà agli utenti di cercare testo nei post del blog.

![Cerca](searchbox.png?format=webp&quality=25)

**Nota: L'elefante nella stanza è che non considero il modo migliore per farlo. Per supportare multi-lingua è super complesso (avrei bisogno di una colonna diversa per lingua) e dovrei gestire le cose specifiche di lingua e di stiramento. Ignorero' questo per ora e mi concentrero' solo sull'inglese. Dopo mostreremo come gestire questa cosa in OpenSearch.**

[TOC]

## Ricerca del testo

Per aggiungere una capacità di ricerca ho dovuto apportare alcune modifiche alla ricerca api. Ho aggiunto la manipolazione per le frasi usando il `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Questo viene usato opzionalmente quando c'è uno spazio nella query

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

Altrimenti uso il metodo di ricerca esistente che aggiunge il carattere prefisso.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Controllo di ricerca

Uso [Alpine.js](https://alpinejs.dev/) Ho fatto un semplice controllo parziale che fornisce una casella di ricerca super semplice.

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

Questo ha una serie di classi CSS da rendere correttamente per la modalità scura o leggera. Il codice Alpine.js è piuttosto semplice. Si tratta di un semplice tipo di controllo che chiama l'api di ricerca quando l'utente digita nella casella di ricerca.
Abbiamo anche un po 'di codice per gestire sfocato per chiudere i risultati della ricerca.

```html
   x-on:click.outside="results = []"
```

Si noti che abbiamo un debounce qui per evitare di martellare il server con le richieste.

## Il JS Typeahead

Questo chiama nella nostra funzione JS (defined in `src/js/main.js`)

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

Come potete vedere questo è abbastanza semplice (molta della complessità è la gestione dei tasti su e giù per selezionare i risultati).
Questo post per il nostro `SearchApi`
Quando viene selezionato un risultato, navighiamo verso l'url del risultato.

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

Ho anche cambiato il fetch per lavorare con HTMX, questo semplicemente cambia il `search` metodo per utilizzare un aggiornamento HTMX:

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

Si noti che scambiamo l'HTML interno del `contentcontainer` con il risultato della ricerca. Questo è un modo semplice per aggiornare il contenuto della pagina con il risultato di ricerca senza un aggiornamento pagina.
Cambiamo anche l'url della storia con il nuovo url.

## In conclusione

Questo aggiunge una potente ma semplice capacità di ricerca al sito. E 'un ottimo modo per aiutare gli utenti a trovare quello che stanno cercando.
Dà a questo sito una sensazione più professionale e rende più facile da navigare.