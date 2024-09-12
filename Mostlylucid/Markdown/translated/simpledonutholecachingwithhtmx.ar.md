# بسيطة "حفرة الدونات" مع HTMX

# أولاً

يمكن أن تكون كثبة الثقب دونوت تقنية مفيدة حيث تريد أن تخبئ عناصر معينة من صفحة ولكن ليس كلها. ومع ذلك يمكن أن يكون من الصعب تنفيذها. في هذا المنصب سأريكم كيف تنفذون تقنية بسيطة لحفرة الدونات باستخدام HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">الساعة24/2024- 09-12T 16:00</datetime>
[TOC]

# المشكلة

أحد القضايا التي كنت أواجهها مع هذا الموقع هو أنني أردت استخدام مضادات التزوير مع استماراتي. وهذه ممارسة أمنية جيدة للحيلولة دون وقوع هجمات لتزوير الطلب عبر الساحل. ومع ذلك، فإنه يسبب مشكلة مع كش الصفحات. رمز مكافحة التزوّد هو فريد لكل صفحة طلب، لذا إذا خزّنت الصفحة، الرمز سيكون نفسه لجميع المستخدمين. وهذا يعني أنه إذا قدم مستخدم استمارة، فإن الرمز سيكون باطلا وسيفشل تقديم الاستمارة. ويحول هذا الأساس دون ذلك عن طريق تعطيل جميع الكثبان عند الطلب عند استخدام رمز مكافحة التزوير. وهذه ممارسة أمنية جيدة، ولكنها تعني أن الصفحة لن تختزن على الإطلاق. هذا ليس مثالياً لموقع مثل هذا حيث المحتوى ثابت في الغالب.

# الإحلال

الطريقة الشائعة حول هذا هو "حفرة الدونات" حيث تخبئ غالبية الصفحة ولكن عناصر معينة. هناك مجموعة من الطرق لتحقيق هذا في ASP.net corre باستخدام إطار العرض الجزئي أردت حلاً أبسط

كما أنا بالفعل استخدام ممتاز [XXXX](https://htmx.org/examples/lazy-load/) في هذا المشروع هناك طريقة بسيطة جدا للحصول على ديناميكية 'ثقب الدونات' وظيفة عن طريق ديناميكية تحميل الجزئيات مع HTMX.
لقد قمت بالفعل بمدونات حول [مُستخدِم مع مُعْزِز](/blog/addingxsrfforjavascript) غير أن المسألة مرة أخرى هي أن هذه الصفحة كانت معوقة فعلياً.

الآن يمكنني إعادة تشغيل هذه الوظيفة عند استخدام HTMX لتحميل الجزئيات بطريقة ديناميكية.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

هذا واضح تماماً، أليس كذلك؟ كل ما يفعله هذا هو الاتصال بالخط الواحد للشفرة في المتحكم الذي يرجع العرض الجزئي. وهذا يعني أن الرمز المضاد للتزوير يتم توليده على الخادم ويمكن إخفاء الصفحة على أنها عادية. والرؤية الجزئية محشوة بطريقة ديناميكية بحيث أن الرمز لا يزال فريدا من نوعه بالنسبة لكل طلب.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

في الجزء الجزئي لا يزال لدينا الشكل البسيط البسيط مع رمز مكافحة التزوير.

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

ثم يلخص هذا جميع الرموز للبحث عن النوع الأول وعندما يقدم فإنه يسحب الرمز ويضيفه إلى الطلب (تماماً كما كان من قبل).

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

# في الإستنتاج

هذه طريقة بسيطة جدا للحصول على "حفرة الدونات" مع HTMX. إنها طريقة رائعة للحصول على فوائد النجارة بدون تعقيد حزمة إضافية آمل أن تجد هذا مفيداً أعلمني إذا كانت لديكم أي أسئلة في التعليقات الواردة أدناه.