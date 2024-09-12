# Enkel "Donut Hole" Caching med HTMX

# Inledning

Donut hål caching kan vara en användbar teknik där du vill cache vissa delar av en sida men inte alla. Men det kan vara svårt att genomföra. I detta inlägg kommer jag att visa dig hur man implementerar en enkel donut hål caching teknik med hjälp av HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Problemet

Ett problem jag hade med denna webbplats är att jag ville använda Anti-förfalska polletter med mina formulär. Detta är en god säkerhetspraxis för att förhindra gränsöverskridande förfalskningsattacker (CSRF). Men det orsakade ett problem med cachelagringen av sidorna. Anti-förfalskning token är unik för varje sida begäran, så om du cache sidan, token kommer att vara samma för alla användare. Detta innebär att om en användare lämnar in ett formulär, token kommer att vara ogiltig och formuläret inlämning kommer att misslyckas. ASP.NET Core förhindrar detta genom att inaktivera all caching på begäran där Anti-förfalska token används. Detta är en bra säkerhetspraxis, men det innebär att sidan inte kommer att vara cachad alls. Detta är inte idealiskt för en webbplats som denna där innehållet är mestadels statiskt.

# Lösningen

Ett vanligt sätt runt detta är "donut hål" caching där du cache majoriteten av sidan men vissa element. Det finns ett gäng sätt att uppnå detta i ASP.NET Core med hjälp av partiella vyramverk men det är komplicerat att genomföra och kräver ofta specifika paket och konfiguration. Jag ville ha en enklare lösning.

Som jag redan använder den utmärkta [HTMX Ordförande](https://htmx.org/examples/lazy-load/) I detta projekt finns det ett super enkelt sätt att få dynamisk "donut hål" funktionalitet genom att dynamiskt ladda Partials med HTMX.
Jag har redan bloggat om [Använda AntiForgeryRequest Tokens med Javascript](/blog/addingxsrfforjavascript) Men återigen var frågan att denna effektivt funktionshindrade caching för sidan.

NU kan jag återställa denna funktionalitet när jag använder HTMX för att dynamiskt ladda delar.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Ganska enkelt, eller hur? Allt detta gör är att kalla in en rad kod i den controller som returnerar den partiella vyn. Detta innebär att Anti-förfalska token genereras på servern och sidan kan cachas som vanligt. Den partiella vyn laddas dynamiskt så att symbolen fortfarande är unik för varje begäran.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Vi har fortfarande den enkla formen med Anti-förfalskade token.

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

Detta inkapslar sedan all kod för typeahead sökning och när den lämnas in drar den token och lägger till den till begäran (exakt som tidigare).

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

# Slutsatser

Detta är ett super enkelt sätt att få "donut hål" caching med HTMX. Det är ett bra sätt att få fördelarna med caching utan komplexiteten i ett extra paket. Jag hoppas att du tycker att det här är användbart. Säg till om du har några frågor i kommentarerna nedan.