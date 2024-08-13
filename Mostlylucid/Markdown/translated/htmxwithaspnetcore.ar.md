# هوتمكس مع مع مع مع

<datetime class="hidden">2024-08-08-01-TT03: 42</datetime>

<!--category-- ASP.NET, HTMX -->
## أولاً

استخدام HTMX مع ASP.NET هي وسيلة كبيرة لبناء تطبيقات شبكية ديناميكية مع الحد الأدنى من جافاسكربت. تسمح لك HTMX بتحديث أجزاء من صفحتك دون إعادة تحميل صفحة كاملة، مما يجعل تطبيقك أكثر استجابة وتفاعلا.

هذا ما كنت أسميه تصميم الويب 'Hibrid' حيث تقوم بترجمة الصفحة بالكامل باستخدام رمز جانب الخادم ومن ثم استخدام HTMX لتحديث أجزاء الصفحة ديناميكيا.

في هذه المقالة، سأريكم كيف تبدأون مع HTMX في تطبيق ASP.net الأساسي.

[رابعاً -

## النفقات قبل الاحتياجات

HTMX - Htmx هو a جافاسكربت حزمة طريقة easist لإدراجه في مشروعك هو إلى استخدام a CDN. (انظر: [هنا هنا](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

يمكنك بطبيعة الحال أيضاً تحميل نسخة وإدراجها'manually' (أو استخدام LibMan أو npm).

## SPSP.net

أوصي أيضاً أيضاً بتثبيت مُساعد Htmx Tacher من [هنا هنا](https://github.com/khalidabuhakmeh/Htmx.Net)

هذان كلاهما من الـ [(السكين)
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

والـ Htmx حزمة من [هنا هنا](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

الـ شارة مساعدة اسمح لـ:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### النهج البديل.

**ملاحظة: لهذا النهج عيب رئيسي واحد؛ فهو لا ينتج هريف لوصلة البريد. وهذه مشكلة تتعلق بتكافؤ فرص الحصول على الخدمات وإمكانية الوصول إليها. هذا يعني أيضاً أن هذه الوصلات ستفشل إذا كان HTMX لسبب ما لا يحمل (CDNs DO يهبط).**

النهج البديل هو استخدام ` hx-boost="true"` و الصفات و المُساعدات الأساسيّة العادية للعلامة. انظر S انظر  [هنا هنا](https://htmx.org/docs/#hx-boost) لمزيد من المعلومات عن Hx-Bostst (بالرغم من أن الأطباء قليلون قليلاً).
هذا سينتج هرف عادي لكن سيتم اعتراضه بواسطة HTMX والمحتوى الذي يتم تحميله بشكل دينامي.

وبناء على ما يلي:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### 

يعمل HTMX بشكل جيد مع إلى. يمكنك استخدام HTMX لتحميل وجهة نظر جزئية في حاوية على صفحتك. هذا عظيم لأجزاء التحميل من صفحتك بشكل ديناميكي بدون إعادة تحميل صفحة كاملة.

في هذا التطبيق لدينا حاوية في ملف التصميم.cshtml التي نريد تحميل عرض جزئي فيها.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

عادة ما يُعطي محتوى جانب الخادم لكن باستخدام مُعَامِل مُعَامِل بطاقة HTMx فيمكنك أن ترى أننا نُوَفّر `hx-target="#contentcontainer"` الذي سيحمّل المنظر الجزئي في الحاوية.

في مشروعنا لدينا نظرة BlogView الجزئية التي نريد تحميلها في الحاوية.

![img.png](project.png)

ثمّ في المدوّنة نحن عِنْدَنا

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

يمكنك أن ترى هنا لدينا طلب HTMX. ISHTmx( طريقة، هذا سيعود صحيحاً إذا كان الطلب هو طلب HTMX. وإذا كان الأمر يتعلق بعكس وجهة النظر الجزئية، فإن لم يكن بعكس وجهة النظر الكاملة.

وباستخدام هذا، يمكننا أن نضمن أننا نؤيد أيضاً الاستعلام المباشر مع بذل جهد حقيقي ضئيل.

وفي هذه الحالة، تشير وجهة نظرنا الكاملة إلى هذا الحكم الجزئي:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

ولدينا الآن طريقة بسيطة جداً لتحميل وجهات النظر الجزئية في صفحتنا باستخدام HTMX.