# إضافة جزء من الجزء الثاني من نظام التعليق - تعليقات الحفظ

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">الساعة24/2024-00/08</datetime>

# أولاً

(أ) في الفترة السابقة [في هذه السلسلة من سلسلة](/blog/addingacommentsystempt1)لقد أنشأت قاعدة بيانات لنظام التعليقات في هذه الوظيفة، سأغطّي كيف يتمّ إدارة الحفاظ على التعليقات جانب العميل وفي ASP.NET الأساسي.

[رابعاً -

## 

### `_CommentForm.cshtml`

هذه وجهة نظر جزئية للميزان تحتوي على شكل لإضافة تعليق جديد. يمكنك أن ترى على أول تحميل فإنه يدعو إلى `window.mostlylucid.comments.setup()` الذي يُحدّدُ المحرّرَ. هذا نص بسيط يستخدم `SimpleMDE` تحرير لـ نص محرّر.

```razor
@model Mostlylucid.Models.Comments.CommentInputModel

 
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
    <section id="commentsection" ></section>
    
    <input type="hidden" asp-for="BlogPostId" />
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <p class="font-body text-lg font-medium text-primary dark:text-white pb-8">Welcome @Model.Name please comment below.</p>
    
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <!-- Username Input -->
    <div class="flex space-x-4"> <!-- Flexbox to keep Name and Email on the same line -->

        <!-- Username Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-user'></i>
            <input type="text" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Name" placeholder="Name (required)" />
        </label>

        <!-- Email Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-envelope'></i>
            <input type="email" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Email" placeholder="Email (optional)" />
        </label>

    </div>

    <textarea id="commenteditor" class="hidden w-full h-44 dark:bg-custom-dark-bg bg-white text-black dark:text-white rounded-2xl"></textarea>

    <input type="hidden" asp-for="ParentId"></input>
    <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
</div>
```

هنا نستخدم الـ Alpin.js `x-init` لـ مُبدئ مُحرّر. هذا نص بسيط يستخدم `SimpleMDE` (ب) السماح بتحرير نصوص غنية (لماذا لا:)

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

هذه الحياة في `comment.js` وهو مسؤول عن وضع محرّر MDE البسيط.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

هذا هو الدالة البسيطة التي تقوم بالفحص في حالة `simplemde` إذا كان الأمر كذلك، فإنه: `initialize` الدالة على ذلك.

## جاري تنفيذ تنفيذ

إلى حفظ التعليق الذي نستخدمه HTMx للقيام بعمل a إلى `CommentController` ثم تحفظ التعليق على قاعدة البيانات.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

هذا استخدامات [مُ](https://www.nuget.org/packages/Htmx.TagHelpers) مـن الوحـد إلـى `CommentController` ثم تستبدل الاستمارة بالتعليق الجديد

ثم نرتبط بـ `mostlylucid.comments.setValues($event)` التي نستخدمها لاستبدال `hx-values` (هذا ضروري فقط نظراً لضرورة تحديث المبسطة يدوياً).

```javascript
    function setValues (evt)  {
    const button = evt.currentTarget;
    const element = mostlylucid.simplemde.getinstance('commenteditor');
    const content = element.value();
    const email = document.getElementById("Email");
    const name = document.getElementById("Name");
    const blogPostId = document.getElementById("BlogPostId");

    const parentId = document.getElementById("ParentId")
    const values = {
        content: content,
        email: email.value,
        name: name.value,
        blogPostId: blogPostId.value,
        parentId: parentId.value
    };

    button.setAttribute('hx-vals', JSON.stringify(values));
};
}
```

### )تابع(

(التعليقات المُتحكمة) `save-comment` تكون مسؤولة عن حفظ التعليق على قاعدة البيانات. كما أنه يرسل رسالة بالبريد الإلكتروني إلى مالك المدونة (me) عند إضافة تعليق.

```csharp
    [HttpPost]
    [Route("save-comment")]
    public async Task<IActionResult> Comment([Bind(Prefix = "")] CommentInputModel model )
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CommentForm", model);
        }
        var postId = model.BlogPostId;
        ;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var comment = model.Content;

        var parentCommentId = model.ParentId;
        
      var htmlContent=  await commentService.Add(postId, parentCommentId, name, comment);
      if (string.IsNullOrEmpty(htmlContent))
      {
          ModelState.AddModelError("Content", "Comment could not be saved");
          return PartialView("_CommentForm", model);
      }
        var slug = await blogService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new {slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url??string.Empty,
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }
```

سترى أن هذا يقوم بعدة أشياء:

1. يضيف التعليق إلى DB (وهذا أيضاً يعمل تحويل رمزي إلى تحويل إلى HTML).
2. إذا كان هناك خطأ فهو يُرجع الـمُشتَرَك مع الـمُشتَرَك. (الملاحظة الأولى لديها الآن أيضاً نشاط اقتفاء يسجل الخطأ في Seq).
3. إذا كان التعليق محفوظاً فإنه يرسل بريداً إلكترونياً لي مع التعليق و URL البريدي.

URL هذا البريد URL ثم اسمحوا لي أن أنقر على البريد، إذا كنت قد دخلت كما أنا (باستخدام [الشيء الخاص بـي في موقعي على Goog Google Auth](/blog/addingidentityfreegoogleauth))ع( هذا فقط تفقّد لـ جوجل الهوية ثمّ يحدد ملكية 'IsAdmin' التي تسمح لي برؤية التعليقات وحذفها إذا لزم الأمر.

# في الإستنتاج

اذاً هذا هو القسم 2، كيف احفظ التعليقات لا يزال هناك زوجين من القطع المفقودة، الخيط (حتى تتمكن من الرد على تعليق)، سرد تعليقاتك الخاصة وحذف التعليقات. أنا سَأَغطّي أولئك في البريدِ القادمِ.