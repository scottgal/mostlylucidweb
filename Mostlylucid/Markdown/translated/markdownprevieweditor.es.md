# Un Editor de Previsualización SimpleMDE Markdown con renderizado del lado del servidor.

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-18T17:45</datetime>

# Introducción

Una cosa que pensé que sería divertido agregar es una manera de ver la marca hacia abajo para los artículos en el sitio con una representación en vivo de la marca hacia abajo. Se trata de un simple editor Markdown que utiliza SimpleMDE y una renderización lateral del servidor de la marcadown usando la biblioteca Markkdig que utilizo para renderizar estos posts de blog.

En el encabezado de los posts de blog al lado de la lista de categorías ahora verás un botón 'editar'
![Editar botón](editbutton.png). Si hace clic en esto obtendrá una página que tiene un editor de marca y una vista previa de la marca. Puede editar la marca hacia abajo y ver los cambios en tiempo real (hit Ctrl-Alt-R (o فارسى-Alt-R en Mac) o Intro para actualizar). Usted también puede golpear el <i class="bx bx-save"></i> para guardar el archivo Markdown en su máquina local.

Por supuesto, esto no guarda el archivo en el servidor, sólo descarga el archivo en su máquina local. ¡No voy a dejar que edites mis posts de blog!

[TOC]

# El Código

## Mi JavaScript de mierda

El Javascript es bastante simplista y acabo de dejarlo en el `scripts` sección de la `Edit.cshtml` página en este momento.

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

Como verás esto dispara en el `load` evento, y luego escucha para el `keydown` evento en el editor SimpleMDE. Si la tecla pulsada es `Ctrl-Alt-R` o `Enter` entonces envía el contenido del editor a un endpoint WebAPI que renderiza la marca hacia abajo y devuelve el HTML. Esto se traduce entonces en el `renderedcontent` div.

Como mis posts de blog se manejan en un `BlogPostViewModel` luego analiza el JSON devuelto y pobla el título, la fecha publicada y las categorías. También se ejecuta el `mermaid` y `highlight.js` bibliotecas para renderizar cualquier diagrama y bloque de código.

## El código C#

### El controlador de edición

En primer lugar he añadido un nuevo controlador llamado `EditorController` que tiene una única acción llamada `Edit` que devuelve la `Edit.cshtml` vista.

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

Verás que esto es bastante simple, utiliza nuevos métodos en el servicio IMarkdownBlog para obtener el post del blog de la bala y luego devuelve el `Editor.cshtml` vista con el `EditorModel` que contiene el marcado y el `BlogPostViewModel`.

Los `Editor.cshtml` vista es una página sencilla con una `textarea` para el marcado y una `div` para el marcado rendido. También tiene un `button` para guardar la marca en la máquina local.

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

Esto trata de hacer que la entrada de blog renderizada se vea lo más cerca posible de la entrada de blog real. También tiene un `script` sección en la parte inferior que contiene el código JavaScript que mostré anteriormente.

### El punto final de WebAPI

El endpoint WebAPI para esto sólo toma el contenido de marca hacia abajo y devuelve el contenido HTML renderizado. Es bastante simple y sólo utiliza el `IMarkdownService` para renderizar la marca.

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

Esto es bastante simple y sólo devuelve el `BlogPostViewModel` que luego es analizado por el JavaScript y se traduce en el `renderedcontent` div.

# Conclusión

Esta es una manera sencilla de previsualizar el contenido de Markdown y creo que es una adición agradable al sitio. Estoy seguro de que hay mejores maneras de hacer esto, pero esto funciona para mí. Espero que le resulte útil y si tiene alguna sugerencia para mejorar, por favor hágamelo saber.