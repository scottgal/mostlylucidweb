# Надпроста система коментарів з розміткою

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 08- 06T18: 50</datetime>

ЗАУВАЖЕННЯ: ПРАЦЯ В ПРОГРЕСІЇ

Я шукаю просту систему коментарів для мого блогу, що використовує Markdown. Я не могла знайти таку, яка мені подобалася, тому вирішила написати власну. Це проста система коментарів, у якій для форматування використовується позначку вниз. У другій частині вікна буде додано сповіщення електронної пошти до системи, за допомогою якої можна надіслати мені повідомлення електронної пошти з посиланням на коментар, що надасть мені змогу " перевірити " його перед тим, як його буде показано на сайті.

Знову для виробничої системи зазвичай використовується база даних, але для цього прикладу я використаю позначку вниз.

## Система коментарів

Система коментарів надзвичайно проста. У мене просто зберігається файл markdown для кожного коментаря з іменем користувача, електронною поштою і коментарем. Після цього коментарі зміщені на сторінці так, як їх отримували.

Щоб ввести коментар, я використовую SimpleMDE, заснований на Javascript редактор Markdown.
Це моє ім' я. _Компонування. cshtml у такий спосіб:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Після цього я ініціалізую редактор SimpleMDE на як завантаження сторінок, так і на завантаження HTMX:

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

Тут я визначаю, що текст мого коментаря називається "коментатор" і започатковує його лише після виявлення. Тут я загорну форму в " Розпізначену " (яку я переводжу до ViewModel). Це означає, що я можу запевнити, що тільки ті, хто увійшов у систему (у даний час з Google) можуть додати коментарі.

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

Ви також помітите, що я використовую HTMX тут для розсилки коментарів. Там, де я використовую атрибут hx-vals і виклик JS, щоб отримати значення для коментаря. Потім цю дію відправляють до контролера блогу за допомогою дії " Об' єднання." Потім новий коментар поміняється.

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