# إطار الهيئة المضافة للوظائـ الوظائف (الجزء 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-2024-00/08-16 ر الساعة 18:00</datetime>

يمكنك أن تجد كل رموز البيانات الخاصة بكتابات المدونات على: [لا يُحَجْجَه](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**الجزءان 1 و2 من السلسلة المتعلقة بإضافة إطار الكيان إلى مشروع أساسي من مشاريع الشبكة.**

يمكن العثور على جزء من الجزء الأول [هنا هنا](/blog/addingentityframeworkforblogpostspt1).

يمكن العثور على جزء من 2 [هنا هنا](/blog/addingentityframeworkforblogpostspt2).

## أولاً

في الأجزاء السابقة قمنا بإنشاء قاعدة البيانات والسياق لمدوناتنا، وأضفنا خدمات التفاعل مع قاعدة البيانات. وسنقوم في هذه الوظيفة بتفصيل كيفية عمل هذه الخدمات الآن مع أجهزة المراقبة والآراء القائمة.

[رابعاً -

## )المراقب

المتحكمون الخارجيون للمدوّنات هي في الحقيقة بسيطة جداً؛ تمشياً مع تجنب "ممارسة "مراقب القيمة" المضادة للمناخ (وهو نمط قمنا بتذييله في وقت مبكر في أيام ASP.NET MVC).

### نمط المراقب المالي الساطن في نظام الإبلاغ الموحد (ASP.net MVC)

أُطر I MVC ممارسة جيدة هي أن تفعل أقل قدر ممكن في أساليب التحكم الخاصة بك. والسبب في ذلك هو أن المراقب مسؤول عن معالجة الطلب وإعادة الرد. ولا ينبغي أن تكون مسؤولة عن المنطق التجاري للتطبيق. وهذه هي مسؤولية النموذج.

"المراقبة الوطنية" المضادة للمنطقة هو المكان الذي يقوم فيه المراقب بالكثير. ويمكن أن يؤدي ذلك إلى عدد من المشاكل، منها ما يلي:

1. جاري تنفيذ من تشفير بوصة مُتَعِدّد إجراءات:
   وينبغي أن يكون الإجراء وحدة عمل واحدة، وأن يقتصر على التعبير عن النموذج وعكس وجهة النظر. إذا وجدت نفسك تكرّر الشفرة في إجراءات متعددة، فهي علامة على أنه يجب عليك إعادة تحويل هذا الشفرة إلى طريقة منفصلة.
2. الرموز التي يصعب اختبارها:
   من خلال إمتلاك "متحكمات الدهون" قد تجعل من الصعب اختبار الشفرة. وينبغي أن يحاول الاختبار اتباع جميع المسارات الممكنة من خلال المدونة، ويمكن أن يكون ذلك صعباً إذا لم تكن المدونة منظمة تنظيماً جيداً ومركزة على مسؤولية واحدة.
3. القانون الذي يصعب الحفاظ عليه:
   وتشكل قابلية الصيانة شاغلاً رئيسياً عند تقديم تطبيقات البناء. وجود أساليب عمل 'Kitchen بالوعة' يمكن أن يؤدي بسهولة إلى لك فضلا عن المطورين الآخرين الذين يستخدمون الرمز لإجراء تغييرات التي كسر أجزاء أخرى من التطبيق.
4. القانون الذي يصعب فهمه:
   وهذا شاغل رئيسي للمطورين. إذا كنت تعمل على مشروع مع قاعدة شفرات كبيرة، قد يكون من الصعب فهم ما يحدث في عمل متحكم إذا كان يفعل الكثير.

### المراقب المالي

سيطرة المدونات بسيطة نسبياً. وينطوي على 4 أعمال رئيسية (وواحدة 'عمل متوافق` لروابط المدونات القديمة). وهي:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

وفي المقابل، فإن هذه الإجراءات تستدعي ما يلي: `IBlogService` للحصول على البيانات التي يحتاجونها. الـ `IBlogService` بيان مفصل في تقرير الأمين العام [م م م م م م م م م م م م م م م م ع م م م م م م م م م م م م م م م م م م م م م م م م م م م م م م م م](/blog/addingentityframeworkforblogpostspt2).

وفيما يلي هذه الإجراءات بدورها:

- الفهرس: هذه هي قائمة التدوينات (التقصيرات في اللغة الإنجليزية، ويمكننا تمديدها لاحقاً لإتاحة لغات متعددة). سوف ترى أنه يأخذ `page` وقد عقد مؤتمراً بشأن `pageSize` كبارامترات. هذا من أجل المُتَزَوِّجِ. (ب) نتائج المؤتمر العالمي الرابع المعني بأقل البلدان نمواً.
- هذا هو المدون المفرد. - - - - - - - - - - - - - - - - - - - - - `slug` الوظـف والبعثــة فــي `language` كبارامترات. هذه هي الطريقة التي تستخدمها حالياً لقراءة هذه التدوينة.
- الفئة: هذه هي قائمة المدوّنات الخاصة بفئة معينة. - - - - - - - - - - - - - - - - - - - - - `category`, `page` وقد عقد مؤتمراً بشأن `pageSize` كبارامترات.
- اللغة: يُظهر هذا تدويناً بلغة معينة. - - - - - - - - - - - - - - - - - - - - - `slug` وقد عقد مؤتمراً بشأن `language` كبارامترات.
- متوافق: هذا عمل متوافق لروابط المدونات القديمة. - - - - - - - - - - - - - - - - - - - - - `slug` وقد عقد مؤتمراً بشأن `language` كبارامترات.

### كساق

كما ذُكِر في [الوظائف السابقة](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) ننفذ `OutputCache` وقد عقد مؤتمراً بشأن `ResponseCahce` لإخفاء نتائج المدوّنات. هذا يحسّن تجربة المستخدم ويقلّل التحميل على الخادم.

وتُنفَّذ هذه المعايير باستخدام موازنات العمل المناسبة التي تحدد البارامترات المستخدمة في العمل (فضلاً عن `hx-request` (لطلبات HTMX). للاختبار مع `Index` نحدد ما يلي:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## لا شيء

وجهات نظر المدونات بسيطة نسبياً. هي في الغالب مجرد قائمة من التدوينات، مع بعض التفاصيل لكل تدوينة. الآراء واردة في `Views/Blog` مجلد. وتتمثل الآراء الرئيسية فيما يلي:

### `_PostPartial.cshtml`

هذه هي وجهة النظر الجزئية لكتابة مدونة واحدة. هو مستخدم داخلنا `Post.cshtml` وجهة نظر.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

هذه هي وجهة النظر الجزئية لقائمة من مقالات المدونات. هو مستخدم داخلنا `Index.cshtml` وكذلك في صفحة الاستقبال.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

هذا استخدامات `_ListPost` إلى عرض المدوّات الفردية إلى جانب [منفذ منفذ منفذ](/blog/addpagingwithhtmx) مما يسمح لنا بتصفح المدونات.

### `_ListPost.cshtml`

الـ _يُستخدم المنظر الجزئي للقائمة لعرض المدونات الفردية في القائمة. تستخدم داخل `_BlogSummaryList` وجهة نظر.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

كما أنكم هنا، لدينا صلة مع موقع التدوين الفردي، فئات البريد، اللغات المتاحة في البريد، موجز المقال، تاريخ النشر ووقت القراءة.

ولدينا أيضاً بطاقات اتصال HTMX للفئات واللغات للسماح لنا بعرض الوظائف المحلية والوظائف لفئة معينة.

لدينا طريقتان لاستخدام HTMX هنا، واحدة تعطي العنوان الكامل وواحدة هي 'HTML فقط' (أي. لا يوجد URL.) هذا لأننا نريد استخدام العنوان الكامل للفئات واللغات، لكننا لا نحتاج إلى العنوان الكامل للمدونة الفردية.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

هذا النهج يُعبّر عن عنوان كامل لكتابة واستخدامات المدوّرة المنفردة. `hx-boost` إلى "تفعي" طلب إلى استخدام HTMX (هذا s `hx-request` مقدم إلى `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

بدلاً من ذلك يستخدم هذا النهج علامات HTMX للحصول على فئات لمواقع المدونات. هذا استخدامات `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` وقد عقد مؤتمراً بشأن `hx-route-category` علامات للحصول على فئات المدوّات بينما `hx-push-url` هو إلى `true` لدفع العنوان إلى تاريخ المتصفح.

وهي تستخدم أيضاً في إطار `Index` طريقة العمل لطلبات HTMX.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

حيث يمكننا إما أن نعيد الرؤية الكاملة أو مجرد وجهة النظر الجزئية لطلبات HTMX، مع إعطاء "SPA" مثل التجربة.

## الصفحة الصفحة

في الـ `HomeController` ونشير أيضاً إلى خدمات المدونات للحصول على آخر نشرات المدونات لصفحة الاستقبال. هذا ما تم القيام به في `Index` طريقة العمل.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

كما سترون هنا سنستخدم `IBlogService` للحصول على آخر نشرات المدونات لصفحة الاستقبال. نستعمل أيضاً `GetUserInfo` للحصول على معلومات المستخدم لصفحة الاستقبال.

مرة أخرى هذا هو طلب HTMX لإعادة العرض الجزئي لمواقع المدونات للسماح لنا بتصفح عناوين المدونات في صفحة الاستقبال.

## في الإستنتاج

في جزئنا التالي سندخل في تفاصيل مفروضة عن كيفية استخدام `IMarkdownBlogService` إلى كتابة قاعدة البيانات مع المدوّنات من ملفات. هذا هو جزء رئيسي من التطبيق حيث أنه يسمح لنا باستخدام ملفات العلامة النهائية لكتابة قاعدة البيانات مع المدوّنات.