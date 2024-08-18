# En SimpleMDE Markdown Förhandsgranskningseditor med serversidesåtergivning.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45.........................................................................................................</datetime>

# Inledning

En sak jag trodde att det skulle vara roligt att lägga till är ett sätt att titta på markdown för artiklarna på webbplatsen med en live-återgivning av markdown. Detta är en enkel markdown editor som använder SimpleMDE och en server sida rendering av markdown med markkdig biblioteket som jag använder för att ge dessa blogginlägg.

I rubriken blogginlägg bredvid kategorilistan ser du nu en "redigera" knapp
![Redigera knapp](editbutton.png)....................................... Om du klickar på detta får du en sida som har en markdown editor och en förhandsgranskning av markdown. Du kan redigera markeringen och se ändringarna i realtid (hit Ctrl-Alt-R (eller  till-Alt-R på Mac) eller Enter för att uppdatera). Du kan också träffa <i class="bx bx-save"></i> knappen för att spara markdown-filen till din lokala maskin.

AV KURS detta sparar inte filen till servern, det laddar bara ner filen till din lokala maskin. Jag tänker inte låta dig redigera mina blogginlägg!

[TOC]

# Koden

## Mitt skitiga JavaScript

Javascript är ganska förenklat och jag har bara lämnat det i `scripts` sektionen för `Edit.cshtml` sidan för tillfället.

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

Som du kommer att se detta triggers på `load` händelse, och sedan lyssnar på `keydown` Event på SimpleMDE-redigeraren. Om tangenten trycks på är `Ctrl-Alt-R` eller `Enter` sedan skickar det innehållet i editorn till en WebAPI endpoint som gör markdown och returnerar HTML. Detta görs sedan i `renderedcontent` Div. Jag vet inte.

Eftersom mina blogginlägg hanteras i en `BlogPostViewModel` den sedan tolkar den returnerade JSON och befolkar titeln, publicerade datum och kategorier. Det driver också `mermaid` och `highlight.js` bibliotek för att göra eventuella diagram och kodblock.

## C#-koden

### Redigera- styrenheten

Först lade jag till en ny controller som heter `EditorController` som har en enda åtgärd som kallas `Edit` som returnerar `Edit.cshtml` Visa.

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

Du kommer att se detta är ganska enkelt, det använder nya metoder på IMarkdownBlogService för att få blogginlägget från snigel och sedan returnerar `Editor.cshtml` med beaktande av följande: `EditorModel` som innehåller markeringen och `BlogPostViewModel`.

I detta sammanhang är det viktigt att se till att `Editor.cshtml` visa är en enkel sida med en `textarea` för nedräkningen och en `div` för den återgivna markeringen. Det har också en `button` för att spara markeringen till den lokala maskinen.

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

Detta försöker att göra det renderade blogginlägget ser så nära själva blogginlägget som möjligt. Det har också en `script` sektion längst ner som innehåller JavaScript-koden jag visade tidigare.

### Slutpunkten för WebAPI

WebAPI-slutpunkten för detta tar bara markdown-innehållet och returnerar det renderade HTML-innehållet. Det är ganska enkelt och bara använder `IMarkdownService` För att göra markeringen.

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

Detta är ganska enkelt och bara returnerar `BlogPostViewModel` som sedan tolkas av JavaScript och återges i `renderedcontent` Div. Jag vet inte.

# Slutsatser

Detta är ett enkelt sätt att förhandsgranska markdown innehåll och jag tycker att det är ett trevligt tillägg till webbplatsen. Jag är säker på att det finns bättre sätt att göra detta, men detta fungerar för mig. Jag hoppas att ni tycker att det är användbart och om ni har några förslag till förbättringar, var snäll och meddela mig.