# Lägga till en kommentar System del 2 - Spara kommentarer

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Inledning

I det föregående [del i denna serie](/blog/addingacommentsystempt1), Jag satte upp databasen för kommentarssystemet. I det här inlägget tar jag upp hur man sparar kommentarerna på klientsidan och i ASP.NET Core.

[TOC]

## Lägg till ny kommentar

### `_CommentForm.cshtml`

Detta är en Razor partiell vy som innehåller formuläret för att lägga till en ny kommentar. Du kan se på första laddningen det ringer till `window.mostlylucid.comments.setup()` vilket initierar redaktören. Detta är ett enkelt textområde som använder `SimpleMDE` editor för att tillåta innehållsrik textredigering.

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

Här använder vi Alpine.js `x-init` Ring för att initiera redaktören. Detta är ett enkelt textområde som använder `SimpleMDE` editor för att tillåta för rik textredigering (eftersom varför inte :).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Detta lever i `comment.js` och ansvarar för initiering av simpleMDE-redigeraren.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Detta är en enkel funktion som kontrollerar om `simplemde` objekt initieras och i så fall kallar `initialize` Den fungerar på det.

## Spara kommentaren

För att spara kommentaren använder vi HTMX för att göra en POST till `CommentController` som sedan sparar kommentaren till databasen.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Detta använder sig av [HTMX- tagghjälp](https://www.nuget.org/packages/Htmx.TagHelpers) att posta tillbaka till `CommentController` och sedan byter formuläret med den nya kommentaren.

Sen tar vi oss in i `mostlylucid.comments.setValues($event)` som vi använder för att befolka `hx-values` atribute (detta är bara nödvändigt eftersom simplemde behöver uppdateras manuellt).

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

### KommentarKontrollator

Den kommentarsansvariges `save-comment` Åtgärden är ansvarig för att spara kommentaren till databasen. Det skickar också ett e-postmeddelande till bloggägaren (mig) när en kommentar läggs till.

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

Du kommer att se att detta gör några saker:

1. Lägger till kommentaren till DB (detta gör också en MarkDig omvandling för att konvertera markdown till HTML).
2. Om det är ett fel returnerar det formuläret med felet. (Notera Jag har också nu en spårningsaktivitet som loggar felet till Seq).
3. Om kommentaren sparas skickar den ett e-postmeddelande till mig med kommentaren och postadressen.

Detta inlägg URL sedan låter mig klicka på inlägget, om jag är inloggad som jag (med [min Google Auth-grej](/blog/addingidentityfreegoogleauth)).............................................................................................. Detta bara kontrollerar för mitt Google-ID sedan sätter "IsAdmin" egenskapen som låter mig se kommentarerna och ta bort dem om det behövs.

# Slutsatser

Så det är del 2, hur jag sparar kommentarerna. Det finns fortfarande ett par bitar saknas; gängning (så att du kan svara på en kommentar), lista dina egna kommentarer och ta bort kommentarer. Jag täcker dem i nästa inlägg.