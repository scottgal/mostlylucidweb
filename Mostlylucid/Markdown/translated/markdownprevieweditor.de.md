# Ein SimpleMDE Markdown Preview Editor mit serverseitigem Rendering.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Einleitung

Eine Sache, die ich dachte, dass es Spaß machen würde, hinzuzufügen, ist eine Möglichkeit, den Markdown für die Artikel auf der Website mit einer Live-Rendering des Markdowns zu betrachten. Dies ist ein einfacher Markdown-Editor, der SimpleMDE und eine serverseitige Rendering des Markdowns mit der Markkdig-Bibliothek verwendet, mit der ich diese Blog-Posts rendern kann.

In der Überschrift der Blog-Posts neben der Kategorien-Liste sehen Sie jetzt einen 'edit'-Button
![Schaltfläche bearbeiten](editbutton.png)......................................................................................................... Wenn Sie auf diese klicken, erhalten Sie eine Seite, die einen Markdown-Editor und eine Vorschau des Markdowns hat. Sie können den Markdown bearbeiten und die Änderungen in Echtzeit sehen (Strg-Alt-R (oder Ctrl-Alt-R auf Mac) oder Enter zum Aktualisieren). Sie können auch die <i class="bx bx-save"></i> Schaltfläche, um die Markdown-Datei auf Ihrem lokalen Rechner zu speichern.

OF COURSE dies speichert die Datei nicht auf dem Server, es lädt nur die Datei auf Ihrem lokalen Rechner herunter. Ich werde nicht zulassen, dass du meine Blog-Beiträge editierst!

[TOC]

# Der Code

## Mein beschissenes JavaScript

Das Javascript ist ziemlich einfach und ich habe es gerade in der `scripts` Abschnitt der `Edit.cshtml` Seite im Moment.

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

Wie Sie sehen werden, diese Auslöser auf der `load` Veranstaltung, und dann hört für die `keydown` Veranstaltung im SimpleMDE Editor. Wenn die Taste gedrückt ist `Ctrl-Alt-R` oder `Enter` dann sendet es den Inhalt des Editors an einen WebAPI-Endpunkt, der den Markdown darstellt und das HTML zurückgibt. Dies wird dann in die `renderedcontent` - div. - Nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein.

Da meine Blog-Beiträge werden in einem `BlogPostViewModel` dann parsiert er den zurückgegebenen JSON und bevölkert den Titel, das veröffentlichte Datum und die Kategorien. Es führt auch die `mermaid` und `highlight.js` Bibliotheken, um beliebige Diagramme und Codeblöcke zu rendern.

## Der C#-Code

### Der Edit-Controller

Erstens habe ich einen neuen Controller namens `EditorController` die eine einzige Aktion namens `Edit` die die `Edit.cshtml` ................................................................................................................................

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

Sie werden sehen, dies ist ziemlich einfach, es verwendet neue Methoden auf dem IMarkdownBlogService, um den Blog-Post aus der Schnecke zu bekommen und dann die `Editor.cshtml` Ansicht mit der `EditorModel` die den Markdown und die `BlogPostViewModel`.

Das `Editor.cshtml` Ansicht ist eine einfache Seite mit einem `textarea` für den Markdown und eine `div` für den gerenderten Markdown. Es hat auch eine `button` um den Markdown auf der lokalen Maschine zu speichern.

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

Dies versucht, den gerenderten Blog-Post so nah wie möglich an den eigentlichen Blog-Post aussehen zu lassen. Es hat auch eine `script` Abschnitt unten, der den JavaScript-Code enthält, den ich zuvor gezeigt habe.

### Der WebAPI-Endpunkt

Der WebAPI-Endpunkt dafür nimmt einfach den Markdown-Inhalt und gibt den gerenderten HTML-Inhalt zurück. Es ist ziemlich einfach und verwendet nur die `IMarkdownService` um den Markdown zu rendern.

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

Dies ist ziemlich einfach und gibt nur die `BlogPostViewModel` die dann durch das JavaScript parsiert und in die `renderedcontent` - div. - Nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein, nein.

# Schlussfolgerung

Dies ist ein einfacher Weg, um Markdown-Inhalte Vorschau und ich denke, es ist eine schöne Ergänzung der Website. Ich bin sicher, es gibt bessere Wege, das zu tun, aber das funktioniert für mich. Ich hoffe, Sie finden es nützlich und wenn Sie irgendwelche Vorschläge für Verbesserungen haben, lassen Sie es mich wissen.