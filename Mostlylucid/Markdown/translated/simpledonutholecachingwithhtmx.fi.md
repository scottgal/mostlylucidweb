# Yksinkertainen "Donut Hole" -välimuisti HTMX:llä

# Johdanto

Donutin reiän välilyönti voi olla hyödyllinen tekniikka, jossa haluat piilottaa tiettyjä elementtejä sivun, mutta ei kaikkia. Sitä voi kuitenkin olla hankala toteuttaa. Tässä viestissä näytän sinulle, kuinka voit toteuttaa yksinkertaisen donitsien välimuistitekniikan HTMX:n avulla.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# Ongelma

Yksi asia, joka minulla oli tämän sivuston kanssa, on se, että halusin käyttää väärennösten vastaisia rahakkeita lomakkeillani. Tämä on hyvä turvallisuuskäytäntö, jolla estetään CSRF:n (Crist-Site Device Forgery) hyökkäykset. Se kuitenkin aiheutti ongelmia sivujen välilyönnissä. Väärennöksen vastainen rahake on ainutlaatuinen jokaiselle sivulle, joten jos jätät sivun, se on sama kaikille käyttäjille. Tämä tarkoittaa, että jos käyttäjä lähettää lomakkeen, merkki on virheellinen ja lomake ei toimi. ASP.NET Core estää tämän estämällä kaikki välimuistit pyydettäessä, jossa käytetään väärennösvastaista rahaketta. Tämä on hyvä turvallisuuskäytäntö, mutta se tarkoittaa, että sivua ei välitetä lainkaan. Tämä ei sovi tällaiseen sivustoon, jossa sisältö on enimmäkseen staattista.

# Ratkaisu

Yleinen tapa kiertää tämä on "donitsireikä" välimuistissa, jossa on suurin osa sivusta, mutta tiettyjä elementtejä. ASP.NET Coressa on useita tapoja saavuttaa tämä osittainen näkymäkehys, joka on kuitenkin monimutkainen toteuttaa ja vaatii usein tiettyjä paketteja ja konfigurointia. Halusin yksinkertaisemman ratkaisun.

Kuten olen jo käyttänyt erinomainen [HTMX](https://htmx.org/examples/lazy-load/) Tässä projektissa on superyksinkertainen tapa saada dynaamista "donitsireikää" toiminnallisuuteen lataamalla osa HTMX:llä.
Olen jo bloggannut [AntiForgeryRequest Tokensin käyttö Javascriptin avulla](/blog/addingxsrfforjavascript) kysymys oli kuitenkin jälleen siitä, että tämä tehokkaasti vammautti välimuistin sivulle.

Nyt voin palauttaa tämän toiminnon, kun lataan osia dynaamisesti HTMX:n avulla.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Helppoa, vai mitä? Tämä on vain kutsumista rekisterinpitäjän ainoaan koodiriviin, joka palauttaa osittaisen näkymän. Tämä tarkoittaa, että anti-väärennöksen token luodaan palvelimelle ja sivu voidaan välimuistiin normaalisti. Osittainen näkymä on ladattu dynaamisesti, joten merkki on edelleen ainutlaatuinen jokaiseen pyyntöön.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Osittainen WITH on yhä yksinkertaisessa muodossa anti-väärennöksen kanssa.

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

Tämä kiteyttää sitten koko koodin hakua varten, ja kun se lähetetään, se vetää kupongin esiin ja lisää sen pyyntöön (täsmennetysti kuten ennenkin).

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

# Johtopäätöksenä

Tämä on erittäin yksinkertainen tapa saada "donitsireikä" välimuistiin HTMX:n avulla. Se on hieno tapa saada välimuistin edut ilman ylimääräisen paketin monimutkaisuutta. Toivottavasti pidät tätä hyödyllisenä. Ilmoita, jos sinulla on kysymyksiä alla olevissa kommenteissa.