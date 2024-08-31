# Añadir un sistema de comentarios Parte 2 - Guardar comentarios

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Introducción

En el anterior [parte de esta serie](/blog/addingacommentsystempt1), he creado la base de datos para el sistema de comentarios. En este post, voy a cubrir cómo guardar los comentarios se gestionan lado cliente y en ASP.NET Core.

[TOC]

## Añadir un nuevo comentario

### `_CommentForm.cshtml`

Esta es una vista parcial de Razor que contiene el formulario para agregar un nuevo comentario. Puedes ver en la primera carga a la que llama `window.mostlylucid.comments.setup()` que inicializa el editor. Este es un área de texto simple que utiliza el `SimpleMDE` editor para permitir la edición de texto enriquecido.

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

Aquí usamos el Alpine.js `x-init` llamar para inicializar el editor. Este es un área de texto simple que utiliza el `SimpleMDE` editor para permitir la edición de texto rico (porque no :)).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Esto vive en el `comment.js` y es responsable de inicializar el simple editor de MDE.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Esta es una función simple que comprueba si el `simplemde` objeto se inicializa y si así se llama el `initialize` función en él.

## Guardando el comentario

Para guardar el comentario utilizamos HTMX para hacer un mensaje a la `CommentController` que luego guarda el comentario en la base de datos.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Esto utiliza la [Ayudante de etiquetas HTMX](https://www.nuget.org/packages/Htmx.TagHelpers) para enviar de nuevo a la `CommentController` y luego cambia el formulario con el nuevo comentario.

Entonces nos enganchamos en el `mostlylucid.comments.setValues($event)` que usamos para poblar el `hx-values` atribute (esto solo es necesario ya que simplemde necesita ser actualizado manualmente).

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

El controlador de comentarios `save-comment` acción es responsable de guardar el comentario en la base de datos. También envía un correo electrónico al propietario del blog (yo) cuando se agrega un comentario.

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

Verás que esto hace algunas cosas:

1. Añade el comentario a la DB (esto también hace una transformación de MarkDig para convertir markdown a HTML).
2. Si hay un error devuelve el formulario con el error. (Nota I también tiene una actividad de rastreo que registra el error en Seq).
3. Si el comentario se guarda, me envía un correo electrónico con el comentario y la URL de la publicación.

Este post URL entonces me permite hacer clic en el post, si estoy conectado como yo (usando [mi cosa de Google Auth](/blog/addingidentityfreegoogleauth)). Esto sólo comprueba para mi ID de Google a continuación, establece la propiedad 'IsAdmin' que me permite ver los comentarios y eliminarlos si es necesario.

# Conclusión

Así que esa es la parte 2, cómo guardo los comentarios. Todavía faltan un par de piezas; enhebrar (para que puedas responder a un comentario), enumerar tus propios comentarios y eliminar comentarios. Cubriré eso en el próximo post.