# Повний пошук тексту (Pt 1. 1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024- 08- 21T20: 30</datetime>

## Вступ

У [Остання стаття](/blog/textsearchingpt1) Я показав вам, як встановити повний текстовий пошук, використовуючи вбудовані можливості повнотекстового пошуку у Postgres. Поки я викривав апі, у мене не було можливості його використати, так що це було трохи насмішкою. У цій статті я покажу вам, як використовувати api пошуку для пошуку тексту у вашій базі даних.

За допомогою цього пункту можна додати невеличке поле пошуку до заголовка сайта, за допомогою якого користувачі зможуть шукати текст у дописах блогу.

![Пошук](searchbox.png?format=webp&quality=25)

**Зверніть увагу на те, що я не вважаю, як найкраще це зробити. Щоб підтримати багатомовність, мені потрібен супер комплекс (мені потрібна інша колонка на мову) і я повинен мати справу з сплетенням та іншими специфічними для мови речами. Зараз я це проігнорую і зосередити увагу на англійській. ПОНАД того ми продемонструємо, як поводитися з цим у OpenSearch.**

[TOC]

## Пошук тексту

Щоб додати можливість пошуку, я повинен був зробити деякі зміни в api пошуку. Я додав обробки для фраз, використовуючи `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

Це, за бажання, використовується, якщо у запиті є пробіл

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

У іншому випадку буде використано існуючий метод пошуку, за допомогою якого буде додано символ префікса.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## Керування пошуками

Користування [Альпійський.js](https://alpinejs.dev/) Я зробив простий частковий контроль, який забезпечує надпросту скриньку для пошуку.

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

Це має набір класів CSS, які правильно відтворюються для темного або світлого режиму. Кодекс альпійських.js досить простий. Це просте керування типом шапки, яке викликає api пошуку, якщо користувач вводить у поле пошуку.
У нас також є невеликий код, за допомогою якого ми можемо не фокусувати фокус, щоб закрити результати пошуку.

```html
   x-on:click.outside="results = []"
```

Помітьте, у нас тут дебют, щоб не забивати сервер просьбами.

## S typeahead JS

Це викликає в нашу функцію JS (визначений в `src/js/main.js`)

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

Як ви можете бачити, це досить просто (багато складністю є керування клавішами вгору і вниз для вибору результатів).
Це дописи до нашого `SearchApi`
Коли буде обрано результат, ми перейдемо до адреси url результату.

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

Я також змінив отримання на роботу з HTMX, це просто змінює `search` Метод для використання оновлення HTMX:

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

Зауважте, що ми змінюємо внутрішній HTML `contentcontainer` з результатами пошуку. Це простий спосіб оновлення вмісту сторінки з результатом пошуку без оновлення сторінки.
Ми також змінюємо URL в історії на новий URL.

## Включення

За допомогою цього пункту можна додати до сайта потужну, але просту можливість пошуку. Це чудовий спосіб допомогти користувачам знайти те, що вони шукають.
Це надає цьому сайту більш професійного відчуття і полегшує навігацію.