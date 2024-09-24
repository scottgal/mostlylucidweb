# Volltextsuche (Pt 1.1)

<!--category-- Postgres, Alpine.js -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Einleitung

In der [letzter Artikel](/blog/textsearchingpt1) Ich habe Ihnen gezeigt, wie Sie eine Volltextsuche mit Hilfe der integrierten Volltextsuche von Postgres einrichten können. Während ich ein Suchapi entlarvte, hatte ich keine Möglichkeit, es tatsächlich zu benutzen, also... es war ein bisschen necken. In diesem Artikel werde ich Ihnen zeigen, wie Sie die Suche api verwenden, um nach Text in Ihrer Datenbank zu suchen.

Frühere Teile dieser Serie:

- [Volltextsuche mit Postgres](/blog/textsearchingpt1)

Nächste Teile dieser Serie:

- [Einführung in OpenSearch](/blog/textsearchingpt2)
- [Offene Suche mit C#](/blog/textsearchingpt3)

Dies wird ein kleines Suchfeld zum Header der Website hinzufügen, das Benutzern erlaubt, nach Text in den Blog-Beiträgen zu suchen.

![Suchen](searchbox.png?format=webp&quality=25)

**Hinweis: Der Elefant im Raum ist, dass ich nicht den besten Weg, dies zu tun. Um Multi-Sprache zu unterstützen ist super komplex (ich würde eine andere Spalte pro Sprache benötigen) und ich würde mich mit stammenden und anderen sprachspezifischen Dingen befassen müssen. Ich werde das vorerst ignorieren und mich nur auf Englisch konzentrieren. SPÄTER werden wir zeigen, wie man das in OpenSearch macht.**

[TOC]

## Suche nach Text

Um eine Suchfunktion hinzuzufügen, musste ich einige Änderungen an der Suchfunktion vornehmen. Ich fügte hinzu, Handhabung für Phrasen mit dem `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Dies wird optional verwendet, wenn in der Abfrage ein Leerzeichen vorhanden ist.

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

Ansonsten verwende ich die bestehende Suchmethode, die das Präfix-Zeichen anhängt.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Suchsteuerung

Verwendung [Alpine.js](https://alpinejs.dev/) Ich habe eine einfache Teilsteuerung gemacht, die ein super einfaches Suchfeld bietet.

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

Dies hat eine Reihe von CSS-Klassen, die korrekt für entweder dunklen oder hellen Modus zu rendern. Der Alpine.js-Code ist ziemlich einfach. Es ist eine einfache Typeahead-Steuerung, die die Suche api ruft, wenn der Benutzer im Suchfeld tippt.
Wir haben auch einen kleinen Code, um unfokussiert zu behandeln, um die Suchergebnisse zu schließen.

```html
   x-on:click.outside="results = []"
```

Beachten Sie, dass wir hier einen Debounce haben, um zu vermeiden, den Server mit Anfragen zu hämmern.

## Der Typeahead JS

Dies ruft unsere JS-Funktion auf (definiert in `src/js/main.js`)

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

Wie Sie sehen können, ist dies ziemlich einfach (viele der Komplexität ist die Handhabung der up und down-Tasten, um Ergebnisse zu wählen).
Diese Beiträge zu unseren `SearchApi`
Wenn ein Ergebnis ausgewählt wird, navigieren wir zur URL des Ergebnisses.

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

### HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX

Ich änderte auch das holen, um mit HTMX zu arbeiten, dies ändert einfach die `search` Methode, um einen HTMX-Refresh zu verwenden:

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

Beachten Sie, dass wir die innerHTML der `contentcontainer` mit dem Ergebnis der Suche. Dies ist ein einfacher Weg, um den Inhalt der Seite mit dem Suchergebnis ohne Seitenwiederholung zu aktualisieren.
Wir ändern auch die URL in der Geschichte in die neue URL.

## Schlussfolgerung

Dies fügt eine leistungsfähige, aber einfache Suchfunktion auf der Website. Es ist eine gute Möglichkeit, Benutzer zu helfen, zu finden, was sie suchen.
Es gibt dieser Website ein professionelleres Gefühl und macht es einfacher zu navigieren.