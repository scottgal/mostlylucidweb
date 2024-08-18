# Un Editor di anteprima di SempliceMDE con rendering laterale del server.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Introduzione

Una cosa che ho pensato sarebbe stato divertente da aggiungere è un modo per guardare il markdown per gli articoli sul sito con un rendering live del markdown. Questo è un semplice editor di markdown che utilizza SimpleMDE e un rendering lato server del markdown utilizzando la libreria markkdig che uso per rendere questi post sul blog.

Nell'intestazione dei post del blog accanto alla lista delle categorie vedrete ora un pulsante 'edit'
![Modifica pulsante](editbutton.png). Se fai clic su questo ottieni una pagina che ha un editor di markdown e un'anteprima del markdown. Puoi modificare il markdown e vedere i cambiamenti in tempo reale (hit Ctrl-Alt-R (o Alt-R su Mac) o Invio per aggiornare). Si può anche colpire il <i class="bx bx-save"></i> pulsante per salvare il file markdown sulla macchina locale.

OF COURSE questo non salva il file sul server, basta scaricare il file sulla vostra macchina locale. Non ti permetterò di modificare i miei post sul blog!

[TOC]

# Il codice

## Il mio JavaScript schifoso

Il Javascript è piuttosto semplicistico e l'ho appena lasciato nel `scripts` sezione del `Edit.cshtml` pagina al momento.

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

Come vedrete questo si innesca sul `load` evento, e poi ascolta per il `keydown` evento sull'editor SimpleMDE. Se il tasto premuto è `Ctrl-Alt-R` oppure `Enter` poi invia il contenuto dell'editor ad un endpoint WebAPI che rende il markdown e restituisce l'HTML. Questo viene poi reso nel `renderedcontent` Div.

Come i miei post sul blog sono gestiti in un `BlogPostViewModel` poi analizza il JSON restituito e popola il titolo, la data pubblicata e le categorie. Esso gestisce anche il `mermaid` e `highlight.js` librerie per visualizzare eventuali diagrammi e blocchi di codice.

## Il codice C#

### Il controllore di modifica

In primo luogo ho aggiunto un nuovo controller chiamato `EditorController` che ha una sola azione chiamata `Edit` che restituisce il `Edit.cshtml` vista.

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

Vedrete questo è abbastanza semplice, utilizza nuovi metodi sul ImarkdownBlogService per ottenere il post del blog dal proiettile e poi restituisce il `Editor.cshtml` vista con la `EditorModel` che contiene il markdown e il `BlogPostViewModel`.

La `Editor.cshtml` vista è una pagina semplice con un `textarea` per il markdown e un `div` per il markdown reso. Ha anche un `button` per salvare il markdown alla macchina locale.

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

Questo cerca di rendere il reso post Blog guardare il più vicino possibile al post del blog reale. Ha anche un `script` sezione in basso che contiene il codice JavaScript che ho mostrato in precedenza.

### L'endpoint WebAPI

L'endpoint WebAPI per questo prende solo il contenuto markdown e restituisce il contenuto HTML reso. E 'abbastanza semplice e utilizza solo il `IMarkdownService` per rendere il markdown.

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

Questo è abbastanza semplice e semplicemente restituisce il `BlogPostViewModel` che viene poi analizzato dal JavaScript e reso nel `renderedcontent` Div.

# In conclusione

Questo è un modo semplice per visualizzare in anteprima i contenuti markdown e penso che sia una bella aggiunta al sito. Sono sicuro che ci sono modi migliori per farlo, ma questo funziona per me. Spero che lo troviate utile e se avete qualche suggerimento per miglioramenti vi prego di farmelo sapere.