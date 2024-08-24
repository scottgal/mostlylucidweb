# Un sistema di commenti super semplice con Markdown

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

NOTA: LAVORI IN CORSO

Ho cercato un semplice sistema di commenti per il mio blog che utilizza Markdown. Non riuscivo a trovarne uno che mi piacesse, cosi' ho deciso di scriverne uno mio. Questo è un semplice sistema di commenti che utilizza Markdown per la formattazione. La seconda parte di questo aggiungerà notifiche e-mail al sistema che mi invierà una e-mail con un link al commento, permettendomi di 'approvare' prima che venga visualizzato sul sito.

Di nuovo per un sistema di produzione questo normalmente userebbe un database, ma per questo esempio sto solo andando usare markdown.

## Il sistema dei commenti

Il sistema dei commenti è incredibilmente semplice. Ho solo un file markdown che viene salvato per ogni commento con il nome, e-mail e commento dell'utente. I commenti vengono poi visualizzati sulla pagina nell'ordine in cui sono stati ricevuti.

Per inserire il commento uso SimpleMDE, un editor basato su Javascript Markdown.
Questo è incluso nel mio _Layout.cshtml come segue:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

Quindi inizializzo l'editor SimpleMDE sia sul carico pagina che su quello HTMX:

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

Qui dico che l'area di testo del mio commento è chiamata 'commento' e inizializzo solo una volta che viene rilevata. Qui avvolgo la forma in un 'IsAutenticated' (che passo nel ViewModel). Questo significa che posso garantire che solo coloro che hanno effettuato l'accesso (attualmente con Google) possono aggiungere commenti.

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

Noterai anche che uso HTMX qui per il commento. Dove uso l'attributo hx-vals e una chiamata JS per ottenere il valore per il commento. Questo viene poi inviato al controller del blog con l'azione 'Commento'. Questo viene poi scambiato con il nuovo commento.

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