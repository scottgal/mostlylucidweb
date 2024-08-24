# Un sistema de comentarios súper simple con Markdown

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

NOTA: TRABAJO EN PROGRESOS

He estado buscando un simple sistema de comentarios para mi blog que utiliza Markdown. No pude encontrar uno que me gustara, así que decidí escribir el mío. Este es un simple sistema de comentarios que usa Markdown para formatear. La segunda parte de esto añadirá notificaciones de correo electrónico al sistema que me enviará un correo electrónico con un enlace al comentario, permitiéndome 'aprobar' antes de que se muestre en el sitio.

Una vez más para un sistema de producción esto normalmente utilizaría una base de datos, pero para este ejemplo sólo voy a utilizar Markdown.

## El sistema de comentarios

El sistema de comentarios es increíblemente simple. Sólo tengo un archivo Markdown que se guarda para cada comentario con el nombre del usuario, correo electrónico y comentario. Los comentarios se muestran en la página en el orden en que fueron recibidos.

Para introducir el comentario uso SimpleMDE, un editor Markdown basado en Javascript.
Esto está incluido en mi _Layout.cshtml de la siguiente manera:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

A continuación, inicializo el editor SimpleMDE en carga de página y carga HTMX:

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

Aquí especifico que mi área de texto de comentario se llama 'comentario' y sólo inicializar una vez que se detecta. Aquí envuelvo la forma en un 'IsAuthenticated' (que paso en el modelo de vista). Esto significa que puedo asegurar que solo aquellos que han iniciado sesión (en la actualidad con Google) pueden agregar comentarios.

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

También notarás que uso HTMX aquí para la publicación de comentarios. Donde uso el atributo hx-vals y una llamada JS para obtener el valor del comentario. Esto se publica luego en el controlador del Blog con la acción 'Comentario'. Esto se intercambia entonces con el nuevo comentario.

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