# Recherche en texte intégral (Pt 1.1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21T20:30</datetime>

## Présentation

Dans le [dernier article](/blog/textsearchingpt1) Je vous ai montré comment configurer une recherche texte complète en utilisant les capacités de recherche texte intégrées de Postgres. Alors que j'ai exposé un api de recherche, je n'avais pas un moyen de l'utiliser vraiment donc... c'était un peu un tracas. Dans cet article, je vais vous montrer comment utiliser la recherche api pour rechercher du texte dans votre base de données.

Pièces précédentes de cette série:

- [Recherche de texte complet avec Postgres](/blog/textsearchingpt1)

Les prochaines parties de cette série:

- [Introduction à OpenSearch](/blog/textsearchingpt2)
- [Ouvrir la recherche avec C#](/blog/textsearchingpt3)

Cela ajoutera une petite boîte de recherche à l'en-tête du site qui permettra aux utilisateurs de rechercher du texte dans les messages de blog.

![Rechercher](searchbox.png?format=webp&quality=25)

**Note: L'éléphant dans la pièce est que je ne considère pas la meilleure façon de le faire. Pour soutenir le multi-langue est super complexe (j'aurais besoin d'une colonne différente par langue) et j'aurais besoin de gérer le collage et d'autres choses spécifiques à la langue. Je vais ignorer ça pour l'instant et me concentrer sur l'anglais. Plus tard, nous montrerons comment gérer ça dans OpenSearch.**

[TOC]

## Recherche de texte

Pour ajouter une capacité de recherche, j'ai dû apporter quelques changements à la recherche api. J'ai ajouté la manipulation pour les phrases en utilisant le `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Ceci est utilisé en option lorsqu'il y a un espace dans la requête

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

Sinon, j'utilise la méthode de recherche existante qui ajoute le caractère de préfixe.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Contrôle de recherche

Utilisation [Alpine.js](https://alpinejs.dev/) J'ai fait un simple contrôle partiel qui fournit une boîte de recherche super simple.

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

Cela a un tas de classes CSS à rendre correctement pour le mode sombre ou léger. Le code Alpine.js est assez simple. C'est un simple contrôle de typeahead qui appelle la recherche api lorsque l'utilisateur tape dans la boîte de recherche.
Nous avons également un petit code pour gérer un focus pour fermer les résultats de recherche.

```html
   x-on:click.outside="results = []"
```

Notez que nous avons un débonflement ici pour éviter de frapper le serveur avec des requêtes.

## Le système JS Typeahead

Cela fait appel à notre fonction JS (définie dans `src/js/main.js`)

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

Comme vous pouvez le voir, c'est assez simple (une bonne partie de la complexité est de gérer les touches haut et bas pour sélectionner les résultats).
Ce poste à notre `SearchApi`
Lorsqu'un résultat est sélectionné, nous naviguons jusqu'à l'url du résultat.

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

J'ai aussi changé l'allée pour travailler avec HTMX, cela change simplement la `search` méthode pour utiliser un rafraîchissement HTMX:

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

Notez que nous échangeons l'innerHTML du `contentcontainer` avec le résultat de la recherche. C'est une façon simple de mettre à jour le contenu de la page avec le résultat de recherche sans mise à jour de la page.
Nous changeons aussi l'url de l'histoire en nouvelle url.

## En conclusion

Cela ajoute une capacité de recherche puissante mais simple au site. C'est un excellent moyen d'aider les utilisateurs à trouver ce qu'ils recherchent.
Il donne à ce site une sensation plus professionnelle et facilite la navigation.