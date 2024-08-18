# A SimpleMDE Markdown Preview Editor avec rendu latéral du serveur.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Présentation

Une chose que j'ai pensé que ce serait amusant d'ajouter est une façon de regarder le balisage pour les articles sur le site avec un rendu en direct du balisage. Il s'agit d'un éditeur de balisage simple qui utilise SimpleMDE et un rendu côté serveur du balisage en utilisant la bibliothèque markkdig que j'utilise pour rendre ces messages de blog.

Dans le titre des billets de blog à côté de la liste des catégories, vous verrez maintenant un bouton 'edit'
![Modifier le bouton](editbutton.png)C'est ce que j'ai dit. Si vous cliquez sur ceci, vous obtiendrez une page qui a un éditeur de balisage et un aperçu du balisage. Vous pouvez modifier le balisage et voir les changements en temps réel (hit Ctrl-Alt-R (ou ☆-Alt-R sur Mac) ou Entrée pour rafraîchir). Vous pouvez également frapper le <i class="bx bx-save"></i> bouton pour enregistrer le fichier de balisage sur votre machine locale.

DE COURS cela ne sauvegarde pas le fichier sur le serveur, il télécharge simplement le fichier sur votre machine locale. Je ne vais pas vous laisser éditer mes billets de blog!

[TOC]

# Le Code

## Mon JavaScript pourri

Le Javascript est assez simpliste et je l'ai laissé dans le `scripts` section de l'ordre du jour `Edit.cshtml` page en ce moment.

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

Comme vous le verrez, ça se déclenche sur le `load` event, puis écoute pour le `keydown` événement sur l'éditeur SimpleMDE. Si la touche enfoncée est `Ctrl-Alt-R` ou `Enter` puis il envoie le contenu de l'éditeur à un paramètre WebAPI qui rend le balisage et retourne le HTML. Ceci est ensuite rendu dans le `renderedcontent` Div.

Comme mes billets de blog sont traités dans un `BlogPostViewModel` il analyse ensuite le JSON retourné et remplit le titre, la date publiée et les catégories. Il gère également le `mermaid` et `highlight.js` bibliothèques pour rendre n'importe quels diagrammes et blocs de code.

## Code C#

### Le contrôleur d'édition

Tout d'abord, j'ai ajouté un nouveau contrôleur appelé `EditorController` qui a une action unique appelée `Edit` qui renvoie la `Edit.cshtml` vue.

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

Vous verrez que c'est assez simple, il utilise de nouvelles méthodes sur le service IMarkdownBlogService pour obtenir le billet de blog de la limace et ensuite retourne le `Editor.cshtml` vue avec le `EditorModel` qui contient le balisage et le `BlogPostViewModel`.

Les `Editor.cshtml` vue est une page simple avec un `textarea` pour le balisage et un `div` pour le balisage rendu. Il a également une `button` pour enregistrer le balisage sur la machine locale.

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

Cela tente de rendre le blog rendu aussi proche que possible du billet de blog réel. Il a également une `script` section en bas qui contient le code JavaScript que j'ai montré plus tôt.

### Le point d'arrivée de WebAPI

Le paramètre WebAPI pour cela prend simplement le contenu de balisage et renvoie le contenu HTML rendu. C'est assez simple et utilise juste le `IMarkdownService` pour rendre le balisage.

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

C'est assez simple et rend juste le `BlogPostViewModel` qui est ensuite analysé par le JavaScript et rendu dans le `renderedcontent` Div.

# En conclusion

C'est une façon simple de prévisualiser le contenu de balisage et je pense que c'est un bon ajout au site. Je suis sûr qu'il y a de meilleures façons de le faire, mais ça marche pour moi. J'espère que vous le trouverez utile et si vous avez des suggestions d'améliorations, faites-le moi savoir.