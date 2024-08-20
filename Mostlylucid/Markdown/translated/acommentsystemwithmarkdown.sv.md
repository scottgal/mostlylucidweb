# En super enkel kommentar system med markering

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50 ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------</datetime>

ANMÄRKNING: ARBETE I FRAMTIDEN

Jag har letat efter ett enkelt kommentarssystem för min blogg som använder Markdown. Jag kunde inte hitta en som jag gillade, så jag bestämde mig för att skriva min egen. Detta är ett enkelt kommentarsystem som använder Markdown för formatering. Den andra delen av detta kommer att lägga till e-postmeddelanden till systemet som kommer att skicka mig ett e-postmeddelande med en länk till kommentaren, så att jag kan "godkänna" det innan det visas på webbplatsen.

Återigen för ett produktionssystem skulle detta normalt använda en databas, men för detta exempel jag bara kommer att använda markdown.

## Kommentarssystemet

Kommentarssystemet är otroligt enkelt. Jag har bara en markdown-fil som sparas för varje kommentar med användarens namn, e-post och kommentar. Kommentarerna visas sedan på sidan i den ordning de mottogs.

För att ange kommentaren använder jag SimpleMDE, en Javascript-baserad markdown-editor.
Detta ingår i min _Layout.cshtml enligt följande:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Jag initierar sedan SimpleMDE-redigeraren för både sidbelastning och HTMX-belastning:

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

Här anger jag att min kommentar textområde kallas "kommentar" och bara initiera när det upptäcks. Här sveper jag in formen i en 'IsAutenticated' (som jag passerar in i ViewModel). Detta innebär att jag kan se till att endast de som har loggat in (för närvarande med Google) kan lägga till kommentarer.

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

Du kommer också märka att jag använder HTMX här för kommentar inlägg. Där jag använder attributet hx-vals och ett JS-anrop för att få värdet för kommentaren. Detta postas sedan till bloggen styrenhet med "Kommentaren" åtgärd. Detta byts sedan ut mot den nya kommentaren.

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