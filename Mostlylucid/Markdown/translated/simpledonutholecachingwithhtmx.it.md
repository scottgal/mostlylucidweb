# Semplice cache 'buco di ciambella' con HTMX

# Introduzione

La cache dei fori di ciambella può essere una tecnica utile in cui si desidera nascondere alcuni elementi di una pagina, ma non tutti. Tuttavia può essere difficile da implementare. In questo post vi mostrerò come implementare una semplice tecnica di cache del foro della ciambella utilizzando HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Il problema

Un problema che stavo avendo con questo sito è che ho voluto utilizzare gettoni Anti-forgery con le mie forme. Questa è una buona pratica di sicurezza per prevenire gli attacchi Cross-Site Request Forgery (CSRF). Tuttavia, stava causando un problema con la cache delle pagine. Il token Anti-forgery è unico per ogni richiesta di pagina, quindi se cacherai la pagina, il token sarà lo stesso per tutti gli utenti. Ciò significa che se un utente invia un modulo, il token non sarà valido e l'invio del modulo fallirà. ASP.NET Core lo impedisce disabilitando tutto il caching su richiesta dove viene utilizzato il token Anti-forgery. Questa è una buona pratica di sicurezza, ma significa che la pagina non sarà cached affatto. Questo non è l'ideale per un sito come questo dove il contenuto è per lo più statico.

# La soluzione

Un modo comune intorno a questo è 'donut buco' cache in cui si cache la maggior parte della pagina, ma alcuni elementi. C'è un sacco di modi per raggiungere questo obiettivo in ASP.NET Core utilizzando il framework di vista parziale, tuttavia è complesso da implementare e spesso richiede pacchetti specifici e configurazione. Volevo una soluzione piu' semplice.

Come ho già usato l'eccellente [HTMX](https://htmx.org/examples/lazy-load/) in questo progetto c'è un modo super semplice per ottenere una funzionalità dinamica 'donut hole' caricando dinamicamente Partials con HTMX.
Ho già bloggato su [usando AntiForgeryRequest Tokens con Javascript](/blog/addingxsrfforjavascript) Tuttavia, ancora una volta il problema era che questo effettivamente disabilitato cache per la pagina.

Ora posso ripristinare questa funzionalità quando si utilizza HTMX per caricare dinamicamente i parziali.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Semplice da morire, vero? Tutto ciò che fa è chiamare in una riga di codice nel controller che restituisce la vista parziale. Ciò significa che il token Anti-forgery viene generato sul server e la pagina può essere cached come normale. La vista parziale è caricata dinamicamente in modo che il token sia ancora unico per ogni richiesta.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

All'interno del parziale abbiamo ancora la semplice forma semplice con il token Anti-forgery.

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

Questo incapsula quindi tutto il codice per la ricerca in anteprima e quando viene inviato tira il token e lo aggiunge alla richiesta (esatto come prima).

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

# In conclusione

Questo è un modo super semplice per ottenere 'donut buco' cache con HTMX. E 'un ottimo modo per ottenere i benefici della cache senza la complessità di un pacchetto extra. Spero che lo trovi utile. Fatemi sapere se avete domande nei commenti qui sotto.