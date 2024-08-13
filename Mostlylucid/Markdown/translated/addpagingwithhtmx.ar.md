# إضافة ملحق مع HTMX و ASP.net مع مُحِجّز

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09-TT12: 50</datetime>

## أولاً

الآن بما أن لدي مجموعة من مقالات المدونات فإن صفحة الاستقبال كانت تطول لذا قررت إضافة آلية استدعاء لمواقع المدونات.

هذا يتماشى مع إضافة كبسولة كاملة لنشرات المدونات لجعل هذا الموقع سريعاً وفعالاً.

- - - - - - - - - - - - - - - [مصدر الخدمة](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) بالنسبة لكيفية تنفيذ هذا، انها في الواقع بسيطة جدا باستخدام IMMory CACH.

[رابعاً -

### 

قررت أن أستخدم واغ بلايدر لتنفيذ آلية الاستدعاء. هذه طريقة عظيمة لتغليف منطق الطقوس وجعله قابلاً لإعادة الاستخدام.
هذا استخدامات [من Darel O' ](https://github.com/darrel-oneil/PaginationTagHelper) أُدرج في المشروع كحزمة معدنية.

ثم يضاف هذا إلى _ملفّ sownmports.cshtml file as itإيطالياهو متوفّر إلى الكل.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### الـ مُجل المُلْف

في الـ _(BlogumaryList.cshtml) وجهة نظر جزئية أضفت الرمز التالي لجعل آلية الاستدعاء.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

بعض الأشياء الملحوظة هنا:

1. `link-url` هذا إلى توليد صحيح URl لـ. وفي طريقة مؤشر المراقبة الداخلية، يُتخذ هذا الإجراء.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

وفي متحكم المدونات

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

هذا هو set إلى الURl. وهذا يضمن أن مساعد المهبل يمكن أن يعمل لأي من طريقتي المستوى الأعلى.

### خصائص HTMXSSSS

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` هذه هي كل خصائص HTMX التي تسمح للناقل بالعمل مع HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

نستعمل هنا `hx-boost="true"` هذا يسمح لـ إلى لا حاجة إلى أي تعديلات بالاعتراض هو عادي URL توليد و استخدام الحالي URL.

`hx-push-url="true"` إلى ضمان URL هو بوصة المتصفح URL URL التاريخ الذي يسمح مباشرة ربط إلى صفحات.

`hx-target="#content"` هذا هو الهدف div الذي سيتم استبداله مع المحتوى الجديد.

`hx-swap="show:none"` هذا هو مقايضة تأثير الذي سيستخدم عند استبدال المحتوى. في هذه الحالة يمنع تأثير 'القفز` العادي الذي يستخدم HTMX على المحتوى المقايضة.

#### CSSS

أضفت أيضاً أساليب إلى الرئيسيات.cs في دليل / src الخاص بي تسمح لي باستخدام فصول CSS التيل ويند للوصلات المائلة.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### المراقب المالي

`page`, `page-size`, `total-items` هي الخصائص التي يستعملها مساعد الوسم المائل لتوليد الوصلات الإشباعية.
يتم تمرير هذه إلى وجهة النظر الجزئية من المتحكم.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### لا يوجد خدمة

هنا يتم تمرير الصفحة و حجم الصفحة من العنوان وتحسب العناصر الإجمالية من خدمة المدونات.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

هنا ببساطة نحصل على الاعمدة من المخبأ، ونطلبها بالتاريخ ثم نتخطى ونأخذ العدد الصحيح من الاعمدة للصفحة.

### ثالثاً - استنتاج

كانت هذه إضافة بسيطة للموقع لكنها تجعله أكثر قابلية للاستخدام. إن دمج HTMX يجعل الموقع يشعر بأنه أكثر استجابة بينما لا يضيف المزيد من جافاسكربت إلى الموقع.