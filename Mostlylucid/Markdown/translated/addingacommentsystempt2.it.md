# Aggiungere un Commento al Sistema Parte 2 - Salvare i Commenti

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Introduzione

Nella precedente [parte di questa serie](/blog/addingacommentsystempt1)Ho creato il database per il sistema dei commenti. In questo post, coprirò come salvare i commenti sono gestiti lato client e in ASP.NET Core.

[TOC]

## Aggiungi nuovo commento

### `_CommentForm.cshtml`

Questa è una vista parziale Razor che contiene il modulo per aggiungere un nuovo commento. Puoi vedere sul primo carico che chiama a `window.mostlylucid.comments.setup()` che inizializza l'editore. Questa è una semplice area di testo che utilizza il `SimpleMDE` editor per consentire l'editing di testo ricco.

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

Qui usiamo il Alpine.js `x-init` chiamata per inizializzare l'editor. Questa è una semplice area di testo che utilizza il `SimpleMDE` editor per consentire l'editing di testo ricco (perché no:)).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Questo vive nel `comment.js` ed è responsabile dell'inizializzazione dell'editor simpleMDE.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Questa è una funzione semplice che controlla se `simplemde` oggetto è inizializzato e se così chiama il `initialize` Funziona su di esso.

## Salvare il commento

Per salvare il commento usiamo HTMX per fare un POST al `CommentController` che poi salva il commento al database.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

In questo modo si utilizza il [Aiuto per i tag HTMX](https://www.nuget.org/packages/Htmx.TagHelpers) per inviare di nuovo al `CommentController` e poi scambia il modulo con il nuovo commento.

Poi ci agganciamo al `mostlylucid.comments.setValues($event)` che usiamo per popolare il `hx-values` atribute (questo è necessario solo perché simplemde deve essere aggiornato manualmente).

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

Il controller del commento `save-comment` action è responsabile del salvataggio del commento al database. Inoltre invia una email al proprietario del blog (me) quando un commento è aggiunto.

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

Vedrai che questo fa un paio di cose:

1. Aggiunge il commento al DB (questo fa anche una trasformazione di MarkDig per convertire markdown in HTML).
2. Se c'è un errore restituisce il modulo con l'errore. (Nota Ho anche ora un'attività di tracciamento che registra l'errore a Seq).
3. Se il commento viene salvato mi invia un'email con il commento e l'URL del post.

Questo URL post quindi mi permette di cliccare il post, se ho effettuato l'accesso come me (utilizzando [la mia cosa di Google Auth](/blog/addingidentityfreegoogleauth)). Questo controlla solo il mio ID di Google quindi imposta la proprietà 'IsAdmin' che mi permette di vedere i commenti e eliminarli, se necessario.

# In conclusione

Quindi questa è la parte 2, come salvo i commenti. Mancano ancora un paio di pezzi; threading (in modo da poter rispondere ad un commento), elencando i propri commenti ed eliminando i commenti. Le copriro' nel prossimo post.