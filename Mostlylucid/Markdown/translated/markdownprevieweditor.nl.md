# Een SimpleMDE Markdown Preview Editor met serverzijde rendering.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Inleiding

Een ding dat ik dacht dat het leuk zou zijn om toe te voegen is een manier om te kijken naar de markdown voor de artikelen op de site met een live weergave van de markdown. Dit is een eenvoudige markdown editor die gebruik maakt van SimpleMDE en een server kant rendering van de markdown met behulp van de markkdig bibliotheek die ik gebruik om deze blog berichten te maken.

In de rubriek van blogberichten naast de categorieënlijst ziet u nu een 'edit' knop
![Knop bewerken](editbutton.png). Als u hierop klikt krijgt u een pagina met een markdown editor en een voorbeeld van de markdown. U kunt de markdown bewerken en de wijzigingen in real-time zien (hit Ctrl-Alt-R (of  616/2007-Alt-R op Mac) of Enter to refresh). U kunt ook op de <i class="bx bx-save"></i> knop om het markdown bestand op te slaan naar uw lokale machine.

Natuurlijk slaat dit het bestand niet op op de server, het downloadt alleen het bestand naar uw lokale machine. Ik ben niet van plan om je te laten bewerken mijn blog berichten!

[TOC]

# De code

## Mijn waardeloze JavaScript

Het Javascript is vrij simplistisch en ik heb het net in de `scripts` afdeling van de `Edit.cshtml` Pagina op dit moment.

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

Zoals u zult zien deze triggers op de `load` en dan luistert naar de `keydown` event op de SimpleMDE editor. Als de toets ingedrukt is `Ctrl-Alt-R` of `Enter` dan stuurt het de inhoud van de editor naar een WebAPI eindpunt dat de markdown maakt en de HTML teruggeeft. Dit wordt vervolgens weergegeven in de `renderedcontent` Div.

Als mijn blog berichten worden behandeld in een `BlogPostViewModel` Het ontleedt vervolgens de teruggekeerde JSON en populeert de titel, gepubliceerde datum en categorieën. Het loopt ook de `mermaid` en `highlight.js` bibliotheken om diagrammen en codeblokken weer te geven.

## De C#-code

### De bewerkingscontroller

Ten eerste heb ik een nieuwe controller toegevoegd genaamd `EditorController` die één enkele actie heeft genaamd `Edit` die het `Edit.cshtml` uitzicht.

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

Je zult zien dat dit is vrij eenvoudig, het maakt gebruik van nieuwe methoden op de IMarkdownBlogService om de blog post van de kogel te krijgen en vervolgens de `Editor.cshtml` overzicht met de `EditorModel` waarin de afwaardering en de `BlogPostViewModel`.

De `Editor.cshtml` weergave is een eenvoudige pagina met een `textarea` voor de afwaardering en a `div` voor de afgedrukte afwaardering. Het heeft ook een `button` om de markdown op te slaan in de lokale machine.

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

Dit probeert om de weergegeven Blog post zo dicht mogelijk bij de werkelijke blogpost mogelijk te maken. Het heeft ook een `script` sectie onderaan die de JavaScript code bevat die ik eerder liet zien.

### Het WebAPI eindpunt

Het WebAPI-eindpunt hiervoor neemt alleen de markdown-content en geeft de weergegeven HTML-content terug. Het is vrij eenvoudig en maakt gebruik van de `IMarkdownService` om de markdown terug te geven.

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

Dit is vrij eenvoudig en geeft gewoon de `BlogPostViewModel` die vervolgens wordt ontleed door het JavaScript en wordt weergegeven in de `renderedcontent` Div.

# Conclusie

Dit is een eenvoudige manier om markdown content te bekijken en ik denk dat het een mooie toevoeging aan de site. Ik weet zeker dat er betere manieren zijn om dit te doen, maar dit werkt voor mij. Ik hoop dat u het nuttig vindt en als u suggesties voor verbeteringen heeft, laat het me dan weten.