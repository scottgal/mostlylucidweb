# Een Super Simple Comment System met Markdown

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

OPMERKING: WERKZAAMHEDEN IN VOORUITGANG

Ik ben op zoek naar een eenvoudig commentaarsysteem voor mijn blog dat Markdown gebruikt. Ik kon er geen vinden die ik leuk vond, dus ik heb besloten om mijn eigen te schrijven. Dit is een eenvoudig commentaarsysteem dat Markdown gebruikt voor het formatteren. Het tweede deel van dit zal e-mailmeldingen toevoegen aan het systeem dat mij een e-mail zal sturen met een link naar het commentaar, zodat ik het kan 'aanpassen' voordat het wordt weergegeven op de site.

Nogmaals voor een productiesysteem zou dit normaal gesproken een database gebruiken, maar voor dit voorbeeld ga ik gewoon markdown gebruiken.

## Het commentaarsysteem

Het commentaar systeem is ongelooflijk eenvoudig. Ik heb alleen een markdown bestand wordt opgeslagen voor elke reactie met de naam van de gebruiker, e-mail en commentaar. De opmerkingen worden vervolgens weergegeven op de pagina in de volgorde die ze werden ontvangen.

Om het commentaar in te voeren gebruik ik SimpleMDE, een Javascript gebaseerde Markdown editor.
Dit is opgenomen in mijn_Layout.cshtml als volgt:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Vervolgens initialiseer ik de SimpleMDE editor op zowel pagina laden als HTMX laden:

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

Hier geef ik aan dat mijn commentaar tekstgebied 'commentaar' heet en pas initialiseer zodra het gedetecteerd is. Hier wikkel ik het formulier in een 'IsAuthenticated' (die ik doorgeef in de ViewModel). Dit betekent dat ik ervoor kan zorgen dat alleen degenen die ingelogd zijn (op dit moment met Google) opmerkingen kunnen toevoegen.

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

U zult ook merken dat ik gebruik HTMX hier voor het posten van commentaar. Waar ik gebruik van de hx-vals attribuut en een JS call om de waarde voor het commentaar te krijgen. Dit wordt vervolgens gepost naar de blog controller met de 'Comment' actie. Dit wordt vervolgens verwisseld met de nieuwe reactie.

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