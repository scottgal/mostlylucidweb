# نص كامل متصفح (Ptt 1 1)

<!--category-- Postgres, Alpine -->
<datetime class="hidden">2024-08-21TT20:30</datetime>

## أولاً

في الـ [المادة 4 من المادة 4](/blog/textsearchingpt1) لقد أريتكم كيف تجهزون بحثاً كاملاً عن النص باستخدام قدرات البحث النصي الكامل للبريد بينما كنتُ أكشف عن بحث لم يكن لديّ طريقة لأستخدمه فعلاً لذا... لقد كان نوعاً ما مُثيراً. في هذه المقالة سأريكم كيف تستخدمون البحث Api للبحث عن نص في قاعدة بياناتكم.

الأجزاء السابقة من هذه السلسلة:

- [جاري البحث مع الموضعات](/blog/textsearchingpt1)

الأجزاء التالية من هذه السلسلة:

- [مقدمة إلى OpenSearch](/blog/textsearchingpt2)
- [ابحث مع C](/blog/textsearchingpt3)

سيضيف هذا صندوق بحث صغير إلى عنوان الموقع مما سيسمح للمستخدمين بالبحث عن النصوص في المدوّنات.

![](searchbox.png?format=webp&quality=25)

**ملاحظة: الفيل في الغرفة هو أنني لا أعتبر أن أفضل طريقة للقيام بذلك. لدعم اللغات المتعددة هي معقدة جداً (أحتاج إلى عمود مختلف لكل لغة) وأحتاج إلى التعامل مع التشابك وأشياء لغوية أخرى محددة. سأتجاهل هذا في الوقت الراهن وأركز فقط على اللغة الإنجليزية. بعد ذلك سنظهر كيفية التعامل مع هذا في OpenSearch.**

[رابعاً -

## 

إلى إضافة a بحث قدرة أنا كان لا بُدَّ أنْ أَجْعلَ بَعْض التغييرات إلى بحث api. أضفت مناولة لعبارة تستخدم `EF.Functions.WebSearchToTsQuery("english", processedQuery)`

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

هذا مُستخدم اختيارياً عندما يكون هناك فراغ في الدالة

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

وإلا سأستخدم طريقة البحث الحالية التي تذييل حرف البادئة.

```csharp
EF.Functions.ToTsQuery("english", query + ":*")

```

## 

& بوصة [الألابين](https://alpinejs.dev/) لقد قمت بسيطرة جزئية بسيطة والتي توفر صندوق بحث بسيط جداً

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

هذا يحتوي على مجموعة من فصول CSS لتشكل بشكل صحيح إما لـ الظلام أو وضع الضوء. رمز (ألبين جس) بسيط جداً إنها تحكم بسيط من النوع الذي يدعو البحث Api عندما يقوم المستخدم بالأنواع في صندوق البحث.
لدينا أيضا رمز صغير للتعامل مع عدم التركيز لإغلاق نتائج البحث.

```html
   x-on:click.outside="results = []"
```

ملاحظة لدينا منفذ هنا لتجنب الضغط على الخادم مع الطلبات.

## نوع الرأس المُسْطَة

وهذا ما يدعونا إلى القيام بوظيفتنا (المحددة في `src/js/main.js`)

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

كما ترون هذا بسيط جداً (الكثير من التعقيد هو مناولة المفاتيح العلوية والأسفلية لاختيار النتائج).
هذه الوظائـف إلى `SearchApi`
عندما يتم اختيار نتيجة فإننا نبحر إلى أوريل النتيجة.

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

### XXXX

قمت أيضاً بتغيير الجلب للعمل مع HTMX، هذا ببساطة يغير `search` إلى استخدام a HTMX:

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

ملاحظة أننا نتبادل داخل HHTML من `contentcontainer` مع نتيجة البحث. وهذه طريقة بسيطة لتحديث محتوى الصفحة بنتيجة البحث دون ان تنعش الصفحة.
كما أننا نغير الطور في التاريخ إلى الطور الجديد.

## في الإستنتاج

وهذا يضيف قدرة بحثية قوية ولكنها بسيطة إلى الموقع. إنها طريقة عظيمة لمساعدة المستخدمين في العثور على ما يبحثون عنه.
يعطي هذا الموقع شعور أكثر احترافية ويجعله أسهل للتنقل.