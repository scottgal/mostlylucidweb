# Un système de commentaires super simple avec balisage

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

NOTE: TRAVAUX EN PROGRÈS

J'ai été à la recherche d'un simple système de commentaires pour mon blog qui utilise Markdown. Je n'ai pas trouvé celui que j'ai aimé, donc j'ai décidé d'écrire le mien. Il s'agit d'un système de commentaires simple qui utilise Markdown pour le formatage. La deuxième partie de cela ajoutera des notifications par e-mail au système qui m'enverrai un courriel avec un lien vers le commentaire, me permettant de 'approuver' avant qu'il ne soit affiché sur le site.

Encore une fois pour un système de production qui utiliserait normalement une base de données, mais pour cet exemple je vais simplement utiliser balisage.

## Le système de commentaires

Le système de commentaires est incroyablement simple. J'ai juste un fichier de balisage en cours d'enregistrement pour chaque commentaire avec le nom de l'utilisateur, le courriel et le commentaire. Les commentaires sont ensuite affichés sur la page dans l'ordre où ils ont été reçus.

Pour entrer le commentaire J'utilise SimpleMDE, un éditeur de Markdown basé sur Javascript.
Ceci est inclus dans mon_Layout.cshtml comme suit:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

J'initialise ensuite l'éditeur SimpleMDE sur la charge de page et la charge HTMX :

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

Ici, je précise que ma zone de texte de commentaire est appelée 'comment' et initialise seulement une fois qu'elle est détectée. Ici, je enveloppe le formulaire dans un 'IsAuthenticated' (que je passe dans le ViewModel). Cela signifie que je peux assurer que seuls ceux qui se sont connectés (actuellement avec Google) peuvent ajouter des commentaires.

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

Vous remarquerez également que j'utilise HTMX ici pour l'affichage des commentaires. Lorsque j'utilise l'attribut hx-vals et un appel JS pour obtenir la valeur pour le commentaire. Ceci est ensuite posté au contrôleur du blog avec l'action 'Commentaire'. Ceci est ensuite échangé avec le nouveau commentaire.

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