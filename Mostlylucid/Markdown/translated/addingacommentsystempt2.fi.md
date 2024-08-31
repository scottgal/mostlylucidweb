# Kommenttijärjestelmän lisääminen Osa 2 - Saving Comments

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Johdanto

Edellisessä [osa tätä sarjaa](/blog/addingacommentsystempt1)Perustin tietokantaa kommenttijärjestelmää varten. Tässä viestissä selvitän, kuinka kommenttien tallentaminen hoidetaan asiakaspuolella ja ASP.NET Coressa.

[TOC]

## Lisää uusi kommentti

### `_CommentForm.cshtml`

Tämä on Razorin osittainen näkymä, joka sisältää uuden kommentin lisäämisen lomakkeen. Voit nähdä ensimmäisellä latauksella, johon se kutsuu `window.mostlylucid.comments.setup()` joka alustaa päätoimittajaa. Tämä on yksinkertainen tekstialue, joka käyttää `SimpleMDE` Päätoimittaja mahdollistaa runsaan tekstin muokkauksen.

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

Täällä käytetään Alpine.js:tä. `x-init` kehota alustamaan päätoimittajaa. Tämä on yksinkertainen tekstialue, joka käyttää `SimpleMDE` Päätoimittaja mahdollistaa runsaan tekstin muokkauksen (koska miksi ei :).

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

Tämä elää `comment.js` ja on vastuussa yksinkertaisen MDE-editorin alustamisesta.

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

Tämä on yksinkertainen toiminto, joka tarkistaa, jos `simplemde` Esine on alustettu ja jos niin kutsuu `initialize` toimi siinä.

## Kommentin tallentaminen

Kommentin tallentamiseksi käytämme HTMX:ää tehdäksemme POST:n `CommentController` joka sitten tallentaa kommentin tietokantaan.

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

Tässä käytetään [HTMX-tagien auttaja](https://www.nuget.org/packages/Htmx.TagHelpers) Palatakseen takaisin Euroopan parlamenttiin. `CommentController` ja sitten vaihtaa lomakkeen uuden kommentin kanssa.

Sitten koukkaamme `mostlylucid.comments.setValues($event)` Jota käytämme kansoittaaksemme `hx-values` ominaisuus (tämä on tarpeen vain, koska simppeliä pitää päivittää manuaalisesti).

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

### Kommenttiohjain

Kommenttiohjaimen `save-comment` toiminta on vastuussa kommentin tallentamisesta tietokantaan. Se lähettää sähköpostia myös blogin omistajalle (meille), kun kommentti lisätään.

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

Huomaat, että tämä tekee muutaman asian:

1. Lisää kommentin DB:hen (tämä tekee myös MarkDig-muunnoksen muuntaakseen markownin HTML:ksi).
2. Jos on virhe, se palauttaa lomakkeen virheineen. (Huomautus I:llä on nyt myös jäljitystoiminto, joka kirjaa virheen Seqiin).
3. Jos kommentti on tallennettu, se lähettää minulle sähköpostia kommentin ja postin URL-osoitteen kanssa.

Tämä viesti URL antaa minun sitten klikata viestiä, jos olen kirjautunut sisään minuna (käyttäen [Google Auth -juttuni](/blog/addingidentityfreegoogleauth)). Tämä vain tarkistaa Google ID:ni ja asettaa Isadmin-ominaisuuden, jonka avulla voin katsoa kommentit ja tarvittaessa poistaa ne.

# Johtopäätöksenä

Joten se on osa 2, miten säästän kommentit. Vielä puuttuu pari kappaletta, kierteitys (joten voit vastata kommenttiin), omien kommenttien listaaminen ja kommenttien poistaminen. Hoidan ne seuraavassa viestissä.