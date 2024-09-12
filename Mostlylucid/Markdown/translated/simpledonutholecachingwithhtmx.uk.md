# Простий Caching " Donut Лунка " з HTMXName

# Вступ

Обробка карежу для дірок може бути корисною, якщо ви бажаєте кешувати певні елементи сторінки, але не всі. Однак її не так легко втілити в життя. В цьому полі я покажу вам, як реалізувати простий метод відколювання дірок за допомогою HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024- 09- 12T16: 00</datetime>
[TOC]

# Проблема

Одна з проблем, яку я мав з цим сайтом, це те, що я хотів використовувати анти-продуктивні жетони зі своїми бланками. Це добра процедура безпеки, щоб запобігти нападам на Cross-Site Request Forgery (CSRF). Однак через це виникла проблема, пов'язана з кешуванням сторінок. Символ блокування є унікальним для кожного запиту на сторінку, отже, якщо ви кешуєте сторінку, ключ буде однаковим для всіх користувачів. Це означає, що під час надсилання користувачем форми ключ буде некоректним, а підкорення форми зазнає невдачі. Ядро ASP.NET запобігає цьому, умикаючи всі кешування за запитом, де використовується анти- підробка. Це непогана вправа з безпеки, але означає, що сторінку взагалі не буде кешовано. Це не ідеально для такого сайту, де зміст переважно статичний.

# Розв'язання

Типовим способом визначення цього параметра є " кешування лунки до дну," у якому ви кешуєте більшість сторінок, але певні елементи. Існує безліч способів досягти цього у ядрах ASP.NET, використовуючи часткову оболонку перегляду, але це складно реалізувати і часто вимагає певних пакунків та налаштувань. Я хотів простіше розв'язання.

Як я вже використовую чудові [HTMX](https://htmx.org/examples/lazy-load/) У цьому проекті є супер простий спосіб отримати динамічну " лунку-понту " динамічно завантаженням частин з HTMX.
Я вже написала блог про [Використання AntiForgeryRequest Tokens за допомогою Javascript](/blog/addingxsrfforjavascript) Одначе, знову проблема була, що це ефективно покалічило кешування сторінки.

Тепер я можу відновити цю функціональність, використовуючи HTMX для динамічного завантаження частин.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Помер просто, так? Все, що це робить, це викликає в єдиний рядок коду у контролері, який повертає частковий перегляд. Це означає, що на сервері створюється ключ блокування, а сторінку можна кешувати як звичайний. Динамічне завантаження часткового перегляду, отже знак все ще є унікальним для кожного з запитів.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

Зважаючи на частини, ми все ще маємо просту форму з анти-продуктивним жетоном.

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

За допомогою цього пункту можна вписати всі коди для пошуку на typeahead, а потім, коли їх буде надіслано, програма натягне ключ і додасть його до запиту (так само, як і раніше).

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

# Включення

Це дуже простий спосіб отримати "карчування в норі" HTMX. Чудовий спосіб отримати користь від кешування без складності додаткового пакета. Сподіваюся, вам це буде корисно. Дайте мені знати, чи є у вас запитання у коментарях нижче.