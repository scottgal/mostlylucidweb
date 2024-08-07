# Ein super einfache Kommentar-System mit Markdown

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

HINWEIS: PROGRESSARBEITEN

Ich habe nach einem einfachen Kommentar-System für meinen Blog gesucht, das Markdown verwendet. Ich konnte nicht finden, dass ich mochte, so entschied ich mich, mein eigenes zu schreiben. Dies ist ein einfaches Kommentar-System, das Markdown für die Formatierung verwendet. Der zweite Teil dieses wird E-Mail-Benachrichtigungen zum System hinzufügen, die mir eine E-Mail mit einem Link zu dem Kommentar senden, so dass ich es 'approve', bevor es auf der Website angezeigt wird.

Wieder für ein Produktionssystem würde dies normalerweise eine Datenbank verwenden, aber für dieses Beispiel werde ich nur Markdown verwenden.

## Das Kommentarsystem

Das Kommentarsystem ist unglaublich einfach. Ich habe nur eine Markdown-Datei gespeichert für jeden Kommentar mit dem Namen des Benutzers, E-Mail und Kommentar. Die Kommentare werden dann auf der Seite in der Reihenfolge, die sie empfangen wurden angezeigt.

Um den Kommentar einzugeben, benutze ich SimpleMDE, einen Javascript-basierten Markdown-Editor.
Dies ist in meinem_Layout.cshtml wie folgt:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Ich initiiere dann den SimpleMDE Editor auf beiden Seiten laden und HTMX laden:

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

Hier gebe ich an, dass mein Kommentartextbereich als 'Kommentar' bezeichnet wird und erst dann initialisiert wird, wenn er erkannt wurde. Hier wickle ich das Formular in ein 'IsAuthenticated' (das ich in das ViewModel übergebe). Das heißt, ich kann sicherstellen, dass nur diejenigen, die sich angemeldet haben (gegenwärtig bei Google) Kommentare hinzufügen können.

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

Sie werden auch bemerken, dass ich HTMX hier für den Kommentar-Posting verwende. Wo ich das hx-vals-Attribut und einen JS-Aufruf benutze, um den Wert für den Kommentar zu erhalten. Dies wird dann mit der Aktion 'Comment' an den Blog-Controller gepostet.

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