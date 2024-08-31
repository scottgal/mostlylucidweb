# Додавання частини системи коментарів 2 - Збереження коментарів

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024- 08- 31T09: 00</datetime>

# Вступ

У попередньому [частина цієї серії](/blog/addingacommentsystempt1)Я заснував базу даних для системи коментарів. В этом отчете, я пойму, как сохраняют комментарии, и как они управляют клиентами, и в яме ASPNET.

[TOC]

## Додати новий коментар

### `_CommentForm.cshtml`

Це частковий Razor, який містить форму для додавання нового коментаря. Ви можете бачити під час першого завантаження, що викликає `window.mostlylucid.comments.setup()` започатковує редактор. Це проста текстова область, що використовує `SimpleMDE` Редактор, який надає змогу змінювати текст з форматуванням.

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

Тут ми використовуємо альпійські.js `x-init` виклик для ініціалізації редактора. Це проста текстова область, що використовує `SimpleMDE` редактор, який надає змогу змінювати текст з форматуванням (оскільки, чому не:).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Це живе в `comment.js` і відповідає за ініціалізацію редактора simpleMDE.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Це проста функція, яка перевіряє чи є значення `simplemde` об' єкт ініціалізовано, якщо такий виклик `initialize` функціонує на ньому.

## Збереження коментаря

Щоб зберегти коментар, ми використовуємо HTMX, щоб зробити POST до `CommentController` який потім зберігає коментар до бази даних.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

This using the [Помічник міток HTMX](https://www.nuget.org/packages/Htmx.TagHelpers) щоб надіслати назад до `CommentController` а потім змінює форму на новий коментар.

Потім ми втягнемося в `mostlylucid.comments.setValues($event)` яку ми використовуємо для заповнення `hx-values` attribute (це потрібно лише для того, щоб програма оновлювала програму вручну).

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

### CommentController

Контролер коментарів `save-comment` дія відповідає за збереження коментаря до бази даних. Крім того, програма надсилає повідомлення електронної пошти власнику блогу (мене) після додавання коментаря.

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

Ви побачите, що це робить кілька речей:

1. Додає коментар до DB (це також виконує перетворення MarkDig для перетворення markdown до HTML).
2. Якщо помилка, вона повертає форму з помилкою. (Зауважте, що тепер у мене також є служба стеження, яка реєструє помилку на Seq).
3. Якщо коментар буде збережено, програма надішле авторові повідомлення електронної пошти з адресою коментаря і адресою URL повідомлення.

Ця адреса URL дозволяє мені клацнути на дописі, якщо я увійшов до системи як я (за допомогою [моє завдання з Google Auth](/blog/addingidentityfreegoogleauth)). Це просто перевіряє мій ідентифікатор Google, потім встановлює властивість 'IsAdmin', яка дозволяє мені побачити коментарі і видалити їх, якщо потрібно.

# Включення

Це частина 2, як я зберігаю коментарі. У програмі все ще бракує декількох елементів; накладки повідомлень (так ви можете відповісти на коментар), список ваших власних коментарів і вилучення коментарів. Я покрию їх на наступному посту.