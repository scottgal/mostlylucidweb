# Einfaches 'Donut Hole' Caching mit HTMX

# Einleitung

Donut Loch Caching kann eine nützliche Technik sein, bei der Sie bestimmte Elemente einer Seite verbergen möchten, aber nicht alle. Allerdings kann es schwierig sein, umzusetzen. In diesem Beitrag werde ich Ihnen zeigen, wie man eine einfache Donut Loch Caching Technik mit HTMX implementieren.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Das Problem

Ein Problem, das ich mit dieser Website hatte, ist, dass ich Anti-Fälschungs-Tokens mit meinen Formularen verwenden wollte. Dies ist eine gute Sicherheitspraxis, um Cross-Site Request Forgery (CSRF) Angriffe zu verhindern. Es verursachte jedoch ein Problem mit dem Caching der Seiten. Das Anti-Forgery Token ist für jede Seitenanforderung einzigartig, wenn Sie also die Seite zwischenspeichern, wird das Token für alle Benutzer gleich sein. Dies bedeutet, dass, wenn ein Benutzer ein Formular abgibt, das Token ungültig ist und die Formular-Einreichung fehlschlägt. ASP.NET Core verhindert dies durch Deaktivierung aller Caching auf Anfrage, wenn das Anti-Fugery Token verwendet wird. Dies ist eine gute Sicherheitspraxis, aber es bedeutet, dass die Seite überhaupt nicht zwischengespeichert wird. Dies ist nicht ideal für eine Website wie diese, wo der Inhalt meist statisch ist.

# Die Lösung

Ein üblicher Weg um dies herum ist 'Donut-Loch'-Caching, wo Sie die Mehrheit der Seite, aber bestimmte Elemente zwischenspeichern. Es gibt eine Reihe von Möglichkeiten, dies in ASP.NET Core mit dem Teilansicht-Framework zu erreichen, aber es ist komplex zu implementieren und erfordert oft spezifische Pakete und Konfiguration. Ich wollte eine einfachere Lösung.

Da ich bereits die ausgezeichnete [HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX HTMX](https://htmx.org/examples/lazy-load/) In diesem Projekt gibt es eine super einfache Möglichkeit, dynamische 'Donut-Loch'-Funktionalität durch dynamisches Laden von Partials mit HTMX zu erhalten.
Ich habe schon darüber geloggt [Verwendung von AntiForgeryRequest Tokens mit Javascript](/blog/addingxsrfforjavascript) aber wieder das Problem war, dass diese effektiv deaktiviert Caching für die Seite.

JETZT kann ich diese Funktionalität wieder einsetzen, wenn ich HTMX benutze, um Partials dynamisch zu laden.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Dead einfach, nicht wahr? Alles, was dies tut, ist in die eine Zeile des Codes im Controller zu rufen, der die Teilansicht zurückgibt. Das bedeutet, dass das Anti-Forgery Token auf dem Server generiert wird und die Seite wie gewohnt zwischengespeichert werden kann. Die Teilansicht wird dynamisch geladen, so dass das Token immer noch für jede Anfrage einzigartig ist.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Im Teil haben wir noch die schlichte einfache Form mit dem Anti-Fälschungs-Token.

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

Dies verkapselt dann den gesamten Code für die Typeahead-Suche und wenn er eingereicht wird, zieht er das Token und fügt es der Anfrage hinzu (genau wie zuvor).

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

# Schlussfolgerung

Dies ist ein super einfacher Weg, um 'Donut Loch' Caching mit HTMX zu bekommen. Es ist ein guter Weg, um die Vorteile des Caching ohne die Komplexität eines zusätzlichen Pakets zu erhalten. Ich hoffe, Sie finden das nützlich. Lassen Sie mich wissen, wenn Sie irgendwelche Fragen in den Kommentaren unten haben.