# Super yksinkertainen kommenttijärjestelmä Markdownilla

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

HUOMAUTUS: TYÖT EDISTYVÄT

Olen etsinyt blogiini yksinkertaista kommenttijärjestelmää, joka käyttää Markdownia. En löytänyt sellaista, josta pidin, joten päätin kirjoittaa omani. Tämä on yksinkertainen kommenttijärjestelmä, joka käyttää Markdownia muotoiluun. Toinen osa lisää sähköposti-ilmoituksia järjestelmään, joka lähettää minulle sähköpostin, jossa on linkki kommenttiin, jolloin voin "myöntää" sen ennen kuin se näytetään sivustolla.

Jälleen tuotantojärjestelmässä tämä normaalisti käyttäisi tietokantaa, mutta tässä esimerkissä aion vain käyttää markdownia.

## Kommenttijärjestelmä

Kommenttijärjestelmä on uskomattoman yksinkertainen. Minulla on vain markdown-tiedosto tallennettuna jokaiseen kommenttiin käyttäjän nimellä, sähköpostilla ja kommentilla. Tämän jälkeen kommentit näkyvät sivulla siinä järjestyksessä, joka ne on saatu.

Kommentin syöttämiseksi käytän SimpleMDE:tä, Javascript-pohjaista Markdown-editoria.
Tämä on mukana minun _Layout.cshtml seuraavasti:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Tämän jälkeen alustan SimpleMDE-editorin sekä sivulatauksen että HTMX-latauksen:

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

Tässä täsmennän, että kommenttini tekstikenttää kutsutaan "kommentiksi", ja aloitan vasta, kun se on havaittu. Käärin lomakkeen tähän muotoon "IsAuthenticated" (joka siirtyy ViewModeliin). Tämä tarkoittaa, että voin varmistaa, että vain ne, jotka ovat kirjautuneet sisään (tällä hetkellä Googlen kanssa), voivat lisätä kommentteja.

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

Huomaat myös, että käytän HTMX:ää täällä kommenttilähetyksessä. Kun käytän hx-vals-attribuuttia ja JS-kutsua saadakseni kommentin arvon. Tämä lähetetään sitten blogin valvojalle kommenttitoiminnolla. Sitten tämä vaihdetaan uuden kommentin kanssa.

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