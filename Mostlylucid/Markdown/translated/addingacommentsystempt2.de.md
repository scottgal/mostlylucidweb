# Fügen Sie einen Kommentar System Teil 2 - Speichern Kommentare

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Einleitung

In den Vorjahren [Teil dieser Reihe](/blog/addingacommentsystempt1), Ich habe die Datenbank für das Kommentar-System eingerichtet. In diesem Beitrag werde ich abdecken, wie das Speichern der Kommentare werden Client-Seite verwaltet und in ASP.NET Core.

[TOC]

## Neuen Kommentar hinzufügen

### `_CommentForm.cshtml`

Dies ist eine Teilansicht von Razor, die das Formular zum Hinzufügen eines neuen Kommentars enthält. Sie können beim ersten Laden sehen, es ruft zu `window.mostlylucid.comments.setup()` die den Editor initialisiert. Dies ist ein einfacher Textbereich, der die `SimpleMDE` Editor, um eine reiche Textbearbeitung zu ermöglichen.

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

Hier benutzen wir die Alpine.js `x-init` Aufruf zur Initialisierung des Editors. Dies ist ein einfacher Textbereich, der die `SimpleMDE` Editor für reiche Textbearbeitung zu erlauben (weil warum nicht :)).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Das Leben in der `comment.js` und ist verantwortlich für die Initialisierung des simpleMDE-Editors.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Dies ist eine einfache Funktion, die überprüft, ob die `simplemde` Objekt wird initialisiert und wenn ja ruft die `initialize` Funktion darauf.

## Speichern des Kommentars

Um den Kommentar zu speichern, verwenden wir HTMX, um einen POST auf die `CommentController` was dann den Kommentar in die Datenbank speichert.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Dabei wird die [HTMX-Tag-Helfer](https://www.nuget.org/packages/Htmx.TagHelpers) zurück zu den `CommentController` und tauscht dann das Formular mit dem neuen Kommentar aus.

Dann haken wir uns in die `mostlylucid.comments.setValues($event)` die wir benutzen, um die `hx-values` atribute (dies ist nur notwendig, da simplemde manuell aktualisiert werden muss).

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

### KommentarController

Der Kommentar-Controller `save-comment` action ist verantwortlich für das Speichern des Kommentars in der Datenbank. Es sendet auch eine E-Mail an den Blog-Besitzer (me), wenn ein Kommentar hinzugefügt wird.

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

Sie werden sehen, dass dies ein paar Dinge tut:

1. Fügt den Kommentar zur DB hinzu (dies geschieht auch eine MarkDig-Transformation, um Markdown in HTML zu konvertieren).
2. Wenn es einen Fehler gibt, gibt es das Formular mit dem Fehler zurück. (Beachten Sie, dass ich jetzt auch eine Nachverfolgungsaktivität habe, die den Fehler bei Seq protokolliert).
3. Wenn der Kommentar gespeichert wird, sendet er mir eine E-Mail mit dem Kommentar und der Post-URL.

Dieser Beitrag URL dann kann ich den Beitrag klicken, wenn ich als ich eingeloggt bin (mit [meine Google-Auth-Sache](/blog/addingidentityfreegoogleauth))== Einzelnachweise == Dies überprüft nur auf meine Google-ID dann setzt die 'IsAdmin'-Eigenschaft, die mir die Kommentare zu sehen und löschen, wenn nötig.

# Schlussfolgerung

Also, das ist Teil 2, wie ich die Kommentare speichern. Es fehlt noch ein paar Stücke; Threading (so können Sie auf einen Kommentar antworten), Auflistung Ihrer eigenen Kommentare und Löschen von Kommentaren. Ich kümmere mich um die nächste Post.