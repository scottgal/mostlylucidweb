# Cache simple 'Donut Hole' avec HTMX

# Présentation

Le cache de trous de donut peut être une technique utile où vous voulez mettre en cache certains éléments d'une page, mais pas tous. Cependant, il peut être difficile à mettre en œuvre. Dans ce post, je vais vous montrer comment implémenter une simple technique de cache de trous de donut à l'aide de HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Le problème

Un problème que j'ai eu avec ce site est que je voulais utiliser des jetons anti-forgery avec mes formulaires. Il s'agit d'une bonne pratique en matière de sécurité pour prévenir les attaques à la force de la demande croisée (CSRF). Cependant, cela posait un problème avec la mise en cache des pages. Le jeton Anti-forgery est unique à chaque demande de page, donc si vous cachez la page, le jeton sera le même pour tous les utilisateurs. Cela signifie que si un utilisateur soumet un formulaire, le jeton sera invalide et la soumission du formulaire échouera. ASP.NET Core empêche cela en désactivant tous les caches sur demande où le jeton Anti-forgery est utilisé. Il s'agit d'une bonne pratique de sécurité, mais cela signifie que la page ne sera pas mise en cache du tout. Ce n'est pas idéal pour un site comme celui-ci où le contenu est essentiellement statique.

# La solution

Un moyen commun autour de cela est « trou de donut » en cache où vous cachez la majorité de la page, mais certains éléments. Il y a un tas de façons d'y parvenir dans ASP.NET Core en utilisant le cadre de vue partielle, mais il est complexe à implémenter et nécessite souvent des paquets et des configurations spécifiques. Je voulais une solution plus simple.

Comme j'utilise déjà l'excellent [HTMX](https://htmx.org/examples/lazy-load/) Dans ce projet, il y a un moyen super simple d'obtenir la fonctionnalité dynamique de « trou de donut » en chargeant dynamiquement les partiels avec HTMX.
J'ai déjà fait un blog sur [utilisant des jetons AntiForgeryRequest avec Javascript](/blog/addingxsrfforjavascript) Cependant, encore une fois, le problème était que cela a effectivement désactivé la mise en cache pour la page.

MAINTENANT, je peux rétablir cette fonctionnalité lors de l'utilisation de HTMX pour charger dynamiquement des partiels.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Mort simple, n'est-ce pas? Tout cela est d'appeler dans la seule ligne de code dans le contrôleur qui retourne la vue partielle. Cela signifie que le jeton Anti-forgery est généré sur le serveur et que la page peut être mise en cache comme normale. La vue partielle est chargée dynamiquement de sorte que le jeton est toujours unique à chaque requête.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Dans la partie, nous avons encore la forme simple avec le jeton Anti-forgery.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement"  x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
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

Cela encapsule alors tout le code pour la recherche de typeahead et quand il est soumis, il tire le jeton et l'ajoute à la requête (exactement comme avant).

```javascript
        let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
```

# En conclusion

C'est une façon super simple d'obtenir un « trou de donut » en cache avec HTMX. C'est un excellent moyen d'obtenir les avantages de la mise en cache sans la complexité d'un paquet supplémentaire. J'espère que vous trouverez cela utile. Dites-moi si vous avez des questions dans les commentaires ci-dessous.