# Eenvoudige 'Donut Hole' Caching met HTMX

# Inleiding

Donut gat caching kan een nuttige techniek zijn waar u bepaalde elementen van een pagina wilt cache maar niet alle. Het kan echter lastig zijn om te implementeren. In dit bericht zal ik u laten zien hoe u een eenvoudige donut gat caching techniek met behulp van HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Het probleem

Een probleem dat ik had met deze site is dat ik wilde gebruiken Anti-forgery tokens met mijn formulieren. Dit is een goede beveiligingspraktijk om Cross-Site Request Forgery (CSRF) aanvallen te voorkomen. Het veroorzaakte echter een probleem met het cachen van de pagina's. De Anti-forgery token is uniek voor elke pagina verzoek, dus als je cache de pagina, de token zal hetzelfde zijn voor alle gebruikers. Dit betekent dat als een gebruiker een formulier indient, het token ongeldig zal zijn en het formulier niet zal worden ingediend. ASP.NET Core voorkomt dit door het uitschakelen van alle caching op aanvraag waar de Anti-forgery token wordt gebruikt. Dit is een goede beveiliging praktijk, maar het betekent dat de pagina helemaal niet zal worden gecached. Dit is niet ideaal voor een site als deze waar de inhoud meestal statisch is.

# De oplossing

Een veelvoorkomende manier om dit te doen is 'donut hole' caching waar je het grootste deel van de pagina maar bepaalde elementen cache. Er is een heleboel manieren om dit te bereiken in ASP.NET Core met behulp van de gedeeltelijke weergave kader echter het is complex om te implementeren en vereist vaak specifieke pakketten en configuratie. Ik wilde een eenvoudigere oplossing.

Zoals ik al gebruik de uitstekende [HTMX](https://htmx.org/examples/lazy-load/) In dit project is er een super eenvoudige manier om dynamische 'donut hole' functionaliteit te krijgen door partialen dynamisch te laden met HTMX.
Ik heb al gelogd over [AntiForgeryRequest Tokens met Javascript gebruiken](/blog/addingxsrfforjavascript) Het probleem was echter ook dat deze effectief uitgeschakelde caching voor de pagina.

Nu kan ik deze functionaliteit herstellen wanneer ik HTMX gebruik om partituren dynamisch te laden.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Doodsimpel, toch? Het enige wat dit doet is de ene regel code in de controller aanroepen die de gedeeltelijke weergave teruggeeft. Dit betekent dat de Anti-forgery token wordt gegenereerd op de server en de pagina kan worden gecached als normaal. De gedeeltelijke weergave wordt dynamisch geladen zodat het token nog steeds uniek is voor elke aanvraag.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Within de gedeeltelijke hebben we nog steeds de simpele vorm met de Anti-forgery token.

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

Dit inkapselt vervolgens alle code voor het typeahead search en wanneer het is ingediend trekt het aan het token en voegt het toe aan het verzoek (precies zoals voorheen).

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

# Conclusie

Dit is een super eenvoudige manier om 'donut hole' caching te krijgen met HTMX. Het is een geweldige manier om de voordelen van caching te krijgen zonder de complexiteit van een extra pakket. Ik hoop dat je dit nuttig vindt. Laat het me weten als u vragen heeft in de onderstaande opmerkingen.