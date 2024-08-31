# Een commentaarsysteem toevoegen Deel 2 - Reacties opslaan

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Inleiding

In het vorige [deel uit maken van deze serie](/blog/addingacommentsystempt1), Ik heb de database voor het commentaar systeem opgezet. In dit bericht, Ik zal behandelen hoe het opslaan van de reacties worden beheerd client kant en in ASP.NET Core.

[TOC]

## Nieuwe opmerking toevoegen

### `_CommentForm.cshtml`

Dit is een Razor gedeeltelijke weergave die het formulier bevat voor het toevoegen van een nieuw commentaar. U kunt zien bij de eerste lading die het oproept naar `window.mostlylucid.comments.setup()` waarin de editor wordt geïnitialiseerd. Dit is een eenvoudig tekstgebied dat gebruik maakt van de `SimpleMDE` editor om een rijke tekstbewerking mogelijk te maken.

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

Hier gebruiken we de Alpine.js `x-init` aanroep om de editor te initialiseren. Dit is een eenvoudig tekstgebied dat gebruik maakt van de `SimpleMDE` editor om een rijke tekstbewerking mogelijk te maken (omdat waarom niet:).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Dit leeft in de `comment.js` en is verantwoordelijk voor het initialiseren van de simpleMDE editor.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Dit is een eenvoudige functie die controleert of de `simplemde` object is geïnitialiseerd en zo ja, aanroept de `initialize` functie erop.

## Commentaar opslaan

Om het commentaar op te slaan gebruiken we HTMX om een POST te doen aan de `CommentController` die vervolgens het commentaar opslaat in de database.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Dit maakt gebruik van de [HTMX-taghulp](https://www.nuget.org/packages/Htmx.TagHelpers) om terug te sturen naar de `CommentController` en dan wisselt het formulier met de nieuwe opmerking.

Dan sluiten we ons aan bij de `mostlylucid.comments.setValues($event)` die we gebruiken om de bevolking te bevolken `hx-values` atribute (dit is alleen nodig omdat simplemde handmatig moet worden bijgewerkt).

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

De comment controller's `save-comment` actie is verantwoordelijk voor het opslaan van het commentaar in de database. Het stuurt ook een e-mail naar de blog eigenaar (me) wanneer een commentaar wordt toegevoegd.

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

Je zult zien dat dit een paar dingen doet:

1. Voegt het commentaar toe aan de DB (dit doet ook een MarkDig transformatie om markdown te converteren naar HTML).
2. Als er een fout is, geeft het het formulier terug met de fout. (Opmerking Ik heb nu ook een tracking activiteit die de fout logt naar Seq).
3. Als het commentaar wordt opgeslagen stuurt het een e-mail naar mij met het commentaar en de post URL.

Dit bericht URL laat me dan klikken op de post, als ik ben ingelogd als mij (met behulp van [mijn Google Auth ding](/blog/addingidentityfreegoogleauth)). Dit controleert alleen op mijn Google ID en stelt dan de eigenschap 'IsAdmin' in waarmee ik de commentaren kan zien en ze indien nodig kan verwijderen.

# Conclusie

Dus dat is deel 2, hoe ik de opmerkingen bewaar. Er ontbreekt nog een paar stukjes; threading (zodat u kunt reageren op een reactie), een lijst van uw eigen opmerkingen en het verwijderen van opmerkingen. Ik zal die in de volgende post dekken.