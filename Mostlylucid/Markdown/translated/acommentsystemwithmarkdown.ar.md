# نظام تعليق مبسّط

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-08-06-TT 18: 50</datetime>

ملاحظة: عملية

كنت أبحث عن نظام تعليق بسيط لمدوّنتي التي تستخدم (ماركداون) لم أستطع العثور على واحدة أحببتها لذا قررت أن أكتبها بنفسي هذا نظام تعليق بسيط يستخدم العلامة التنازلية للشكل. الجزء الثاني من هذا سيضيف إخطارات بريد إلكتروني إلى النظام الذي سيرسل لي بريد إلكتروني مع وصلة إلى التعليق، يسمح لي بـ 'تأثير' قبل عرضها على الموقع.

مرة أخرى لنظام الإنتاج هذا يستخدم عادة قاعدة بيانات، ولكن لهذا المثال أنا فقط ذاهب إلى استخدام العلامة التنازلية.

## نظام التعليق

نظام التعليق بسيط جداً أنا فقط لدي ملف هدفي يتم حفظه لكل تعليق مع اسم المستخدم و بريده الإلكتروني و تعليقه. ثم تُعرض التعليقات على الصفحة حسب ترتيب ورودها.

إلى إدخال تعليق أنا استخدام بسيط MedE ، a جافاكر مستند محرّر.
هذا وارد في _تصميم.cshtml على النحو التالي:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

ثم أقوم بتأسيس محرّر MDE البسيط على كل من حمولة الصفحة و حمولة HTMX:

```javascript
    var simplemde;
    document.addEventListener('DOMContentLoaded', function () {
    
        if (document.getElementById("comment") != null)
        {
        
       simplemde = new SimpleMDE({ element: document.getElementById("comment") });
       }
        
    });
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (document.getElementById("comment") != null)
        {
        simplemde = new SimpleMDE({ element: document.getElementById("comment") });
        
        }
    });
```

هنا أحدد أن نص تعليقي يسمى 'common' وفقط يبدأ بمجرد اكتشافه. هنا أَلْفُّ الشكلَ في 'AsisAuthenticated' (الذي أَنْقلُ إلى المنظرِ. وهذا يعني أنني أستطيع أن أضمن أنه لا يمكن إضافة تعليقات إلا لمن دخلوا (في الوقت الحاضر مع غوغل).

```razor
@if (Model.Authenticated)
    {
        
  
        <div class=" max-w-none border-b border-grey-lighter py-8 dark:prose-dark sm:py-12">
            <p class="font-body text-lg font-medium text-primary dark:text-white">Welcome @Model.Name please comment below.</p>
            <textarea id="comment"></textarea>
       <button class="btn btn-primary" hx-action="Comment" hx-controller="Blog" hx-post hx-vals="js:{comment: simplemde.value()}" hx-route-slug="@Model.Slug" hx-swap="outerHTML" hx-target="#comment" onclick="prepareForSubmission()">Comment</button>
        </div>
    }
    else
    {
       ...
    }
```

ستلاحظ أيضاً أنني أستخدم HTMX هنا لنشر التعليق. حيث أستخدم خاصية Hx-vals و مكالمة مشتركة للحصول على قيمة التعليق. هذا هو إلى Blog control مع تعليق إجراء. ثم يتم مبادلة هذا مع التعليق الجديد.

```csharp
    [HttpPost]
    [Route("comment")]
    [Authorize]
    public async Task<IActionResult> Comment(string slug, string comment)
    {
        var principal = HttpContext.User;
        principal.Claims.ToList().ForEach(c => logger.LogInformation($"{c.Type} : {c.Value}"));
        var nameIdentifier = principal.FindFirst("sub");
        var userInformation = GetUserInfo();
       await commentService.AddComment(slug, userInformation, comment, nameIdentifier.Value);
        return RedirectToAction(nameof(Show), new { slug });
    }

```