# Редактор попереднього перегляду SimpleMDE з показом на стороні сервера.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024- 08- 18T17: 45</datetime>

# Вступ

Одна річ, яку я думав, було б цікаво додати, - це подивитися на відмітку для статей на сайті, на якому зображено підпис. Це простий редактор markdown, який використовує SimpleMDE і серверне відображення спадного списку за допомогою бібліотеки markkdig, яку я використовую для показу цих дописів у блогі.

У заголовку дописів блогу поряд зі списком категорій ви побачите кнопку "edit "
![Кнопка редагування](editbutton.png). Якщо ви натиснете цю кнопку, програма відкриє сторінку, на якій буде показано спадний список і попередній перегляд розмітки. Ви можете змінити режим зрізу і переглянути зміни у реальному часі (або скористатися комбінацією клавіш Ctrl-Alt- R на Mac) або Enter для оновлення). Ви також можете вдарити <i class="bx bx-save"></i> button для збереження файла markdown до вашого локального комп' ютера.

Це не зберігає файл на сервері, він просто звантажує файл на ваш локальний комп' ютер. Я не дозволю вам редагувати мої блогові дописи!

[TOC]

# Код

## Мій паршивий JavaScript

Javascript досить спрощений і я щойно залишив його в `scripts` section of the `Edit.cshtml` На данный момент.

```javascript
        window.addEventListener('load', function () {
            console.log('Page loaded without refresh');

            // Trigger on change event of SimpleMDE editor
            window.simplemde.codemirror.on("keydown", function(instance, event) {
            let triggerUpdate= false;
                // Check if the Enter key is pressed
                if ((event.ctrlKey || event.metaKey) && event.altKey && event.key.toLowerCase() === "r") {
                    event.preventDefault(); // Prevent the default behavior (e.g., browser refresh)
                    triggerUpdate = true;
                    }
                    if (event.key === "Enter")
                    {
                        triggerUpdate = true;
                    }

                if (triggerUpdate) {
        
                    var content = simplemde.value();

                    // Send content to WebAPI endpoint
                    fetch('/api/editor/getcontent', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({ content: content })  // JSON object with 'content' key
                    })
                        .then(response => response.json())  // Parse the JSON response
                        .then(data => {
                            // Render the returned HTML content into the div
                            document.getElementById('renderedcontent').innerHTML = data.htmlContent;
                            document.getElementById('title').innerHTML  = data.title;// Assuming the returned JSON has an 'htmlContent' property
                            const date = new Date(data.publishedDate);

                            const formattedDate = new Intl.DateTimeFormat('en-GB', {
                                weekday: 'long',  // Full weekday name
                                day: 'numeric',   // Day of the month
                                month: 'long',    // Full month name
                                year: 'numeric'   // Full year
                            }).format(date);
                            
                            document.getElementById('publishedDate').innerHTML = formattedDate;
                           populateCategories(data.categories);
                            
                            
                            mermaid.run();
                            hljs.highlightAll();
                        })
                        .catch(error => console.error('Error:', error));
                }
            });

            function populateCategories(categories) {
                var categoriesDiv = document.getElementById('categories');
                categoriesDiv.innerHTML = ''; // Clear the div

                categories.forEach(function(category) {
                    // Create the span element
                    let span = document.createElement('span');
                    span.className = 'inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white'; // Apply the style class
                    span.textContent = category;

                    // Append the span to the categories div
                    categoriesDiv.appendChild(span);
                });
            }
        });
```

Як ви побачите, це пов'язано з `load` подія, а потім слухати `keydown` подія у редакторі SimpleMDE. Якщо натиснуто клавішу `Ctrl-Alt-R` або `Enter` після цього програма відправляє вміст редактора у кінцеву точку WebAPI, яка пересилає позначку у спадному списку і повертає HTML. Потім це буде перетворено на `renderedcontent` Лови.

Під час роботи з дописами в блогі `BlogPostViewModel` тоді він розбирає повернений ЙСОН і заповнює назву, опубліковує дату й категорії. Це також працює `mermaid` і `highlight.js` Бібліотеки для показу будь-яких діаграм і блоків коду.

## Код C# Description of a condition. Do not translate key words (# V1S #, # V1 #,...)

### Регулятор редагування

Спочатку я додав новий контролер під назвою `EditorController` який має окрему дію з назвою `Edit` що повертає `Edit.cshtml` вигляд.

```csharp
       [HttpGet]
    [Route("edit")]
    public async Task<IActionResult> Edit(string? slug = null, string language = "")
    {
        if (slug == null)
        {
            return View("Editor", new EditorModel());
        }

        var blogPost = await markdownBlogService.GetPageFromSlug(slug, language);
        if (blogPost == null)
        {
            return NotFound();
        }

        var model = new EditorModel { Markdown = blogPost.OriginalMarkdown, PostViewModel = blogPost };
        return View("Editor", model);
    }
```

Ви побачите, що це досить просто, він використовує нові методи на IMarkdownBlogService, щоб отримати допис блогу від slug, а потім повертає `Editor.cshtml` перегляд з переглядом `EditorModel` який містить відмітку і `BlogPostViewModel`.

The `Editor.cshtml` перегляд є простою сторінкою з a `textarea` для звороту і відмітки `div` у вигляді позначеного повідомлення. Вона також має `button` щоб зберегти позначку до локального комп' ютера.

```razor
@model Mostlylucid.Models.Editor.EditorModel

<div class="min-h-screen bg-gray-100">
    <p class="text-blue-dark dark:text-blue-light">This is a viewer only at the moment see the article <a asp-action="Show" asp-controller="Blog" asp-route-slug="markdownprevieweditor" class="text-blue-dark dark:text-blue-light">on how this works</a>.</p>
    <div class="container mx-auto p-0">
        <p class="text-blue-dark dark:text-blue-light">To update the preview hit Ctrl-Alt-R (or ⌘-Alt-R on Mac) or Enter to refresh. The Save <i class="bx bx-save"></i> icon lets you save the markdown file to disk </p>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <!-- Column 1 -->
            <div class="bg-white dark:bg-custom-dark-bg  p-0 rounded shadow-md">
                <textarea class="markdowneditor hidden" id="markdowneditor">@Model.Markdown</textarea>

            </div>

            <!-- Column 2 -->
            <div class="bg-white dark:bg-custom-dark-bg p-0 rounded shadow-md">
                <p class="text-blue-dark dark:text-blue-light">This is a preview from the server running through my markdig pipeline</p>
                <div class="border-b border-grey-lighter pb-2 pt-2 sm:pb-2" id="categories">
                    @foreach (var category in Model.PostViewModel.Categories)
                    {
                        <span
                            class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>

                    }
                </div>
                <h2 class="pb-2 block font-body text-3xl font-semibold leading-tight text-primary dark:text-white sm:text-3xl md:text-3xl" id="title">@Model.PostViewModel.Title</h2>
                <date id="publishedDate" class="py-2">@Model.PostViewModel.PublishedDate.ToString("D")</date>
                <div class="prose prose max-w-none border-b py-2 text-black dark:prose-dark sm:py-2" id="renderedcontent">
                    @Html.Raw(Model.PostViewModel.HtmlContent)
                </div>
            </div>
        </div>
    </div>
</div>
```

За допомогою цього пункту можна зробити вигляд допису до блогу якомога ближчим до допису блогу. Вона також має `script` розділ внизу, у якому міститься код JavaScript, який було показано раніше.

### Кінцева точка WebAPI

Кінцева точка WebAPI для виконання цієї дії просто приймає пункт зі спадного списку і повертає показаний HTML вміст. Це досить просто і просто використовує `IMarkdownService` щоб передати знак вниз.

```csharp
 [Route("api/editor")]
[ApiController]
public class Editor(IMarkdownBlogService markdownBlogService) : ControllerBase
{
    public class ContentModel
    {
        public string Content { get; set; }
    }

    [HttpPost]
    [Route("getcontent")]
    public IActionResult GetContent([FromBody] ContentModel model)
    {
        var content =  model.Content.Replace("\n", Environment.NewLine);
        var blogPost = markdownBlogService.GetPageFromMarkdown(content, DateTime.Now, "");
        return Ok(blogPost);
    }
}
```

Це досить просто і просто повертає `BlogPostViewModel` який буде оброблено JavaScript і показано у `renderedcontent` Лови.

# Включення

Це простий спосіб переглянути зміст, і я думаю, що це гарний додаток до сайту. Я впевнений, що є кращі способи це зробити, але це працює для мене. Сподіваюся, це вам знадобиться, і якщо у вас є якісь пропозиції щодо покращення, будь ласка, дайте мені знати.