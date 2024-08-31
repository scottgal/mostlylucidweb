# Ajout d'un système de commentaires Partie 2 - Enregistrement de commentaires

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Présentation

Dans la précédente [partie de cette série](/blog/addingacommentsystempt1), j'ai mis en place la base de données pour le système de commentaires. Dans ce post, je vais couvrir comment enregistrer les commentaires sont gérés côté client et dans ASP.NET Core.

[TOC]

## Ajouter un nouveau commentaire

### `_CommentForm.cshtml`

C'est une vue partielle de Razor qui contient le formulaire pour ajouter un nouveau commentaire. Vous pouvez voir sur la première charge il appelle à `window.mostlylucid.comments.setup()` qui initialise l'éditeur. Il s'agit d'une simple zone de texte qui utilise le `SimpleMDE` éditeur pour permettre l'édition de texte riche.

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

Ici nous utilisons le Alpine.js `x-init` appel pour initialiser l'éditeur. Il s'agit d'une simple zone de texte qui utilise le `SimpleMDE` éditeur pour permettre l'édition de texte riche (parce que pourquoi pas :)).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Cela vit dans le `comment.js` et est responsable de l'initialisation de l'éditeur MDE simple.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Il s'agit d'une fonction simple qui vérifie si la `simplemde` objet est initialisé et si c'est le cas appelle le `initialize` Fonctionne dessus.

## Sauver le commentaire

Pour enregistrer le commentaire, nous utilisons HTMX pour faire un POST `CommentController` qui enregistre ensuite le commentaire dans la base de données.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Il s'agit de [Aide aux étiquettes HTMX](https://www.nuget.org/packages/Htmx.TagHelpers) pour poster de nouveau à la `CommentController` puis échange le formulaire avec le nouveau commentaire.

Ensuite, nous nous accrochons à la `mostlylucid.comments.setValues($event)` que nous utilisons pour peupler le `hx-values` atribute (ce n'est nécessaire que si simplemde doit être mis à jour manuellement).

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

### CommentContrôleur

Le contrôleur de commentaires `save-comment` l'action est responsable de la sauvegarde du commentaire dans la base de données. Il envoie également un e-mail au propriétaire du blog (moi) quand un commentaire est ajouté.

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

Vous verrez que cela fait quelques choses :

1. Ajoute le commentaire à la DB (ceci effectue également une transformation de MarkDig pour convertir le balisage en HTML).
2. S'il y a une erreur, elle retourne le formulaire avec l'erreur. (Note J'ai également maintenant une activité de traçage qui enregistre l'erreur à Seq).
3. Si le commentaire est enregistré, il m'envoie un email avec le commentaire et l'URL du message.

Ce message URL me permet alors de cliquer sur le message, si je suis connecté comme moi (en utilisant [mon truc Google Auth](/blog/addingidentityfreegoogleauth))............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................. Il suffit de vérifier mon identifiant Google puis de définir la propriété 'IsAdmin' qui me permet de voir les commentaires et de les supprimer si nécessaire.

# En conclusion

Donc c'est la deuxième partie, comment je sauvegarde les commentaires. Il manque encore quelques morceaux; threading (pour que vous puissiez répondre à un commentaire), énumérant vos propres commentaires et supprimant les commentaires. Je les couvrirai dans le prochain poste.