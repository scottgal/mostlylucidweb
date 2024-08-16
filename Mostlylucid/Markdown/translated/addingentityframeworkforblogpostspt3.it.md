# Aggiunta del quadro dell'entità per i post del blog (parte 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Potete trovare tutto il codice sorgente per i post del blog su [GitHubCity name (optional, probably does not need a translation)](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Parti 1 e 2 della serie relativa all'aggiunta di Entity Framework a un progetto.NET Core.**

Si può trovare la parte 1 [qui](/blog/addingentityframeworkforblogpostspt1).

Si può trovare la parte 2 [qui](/blog/addingentityframeworkforblogpostspt2).

## Introduzione

Nelle parti precedenti abbiamo creato il database e il contesto per i nostri post sul blog, e aggiunto i servizi per interagire con il database. In questo post, spiegheremo come questi servizi funzionano ora con i controllori e le opinioni esistenti.

[TOC]

## Controllori

I controller per i Blog sono davvero molto semplici; in linea con l'evitare l'antipattern 'Fat Controller' (un pattern che abbiamo ideato all'inizio dei giorni ASP.NET MVC).

### Il modello Fat Controller in ASP.NET MVC

I MVC inquadra una buona pratica è quello di fare il meno possibile nei vostri metodi di controllo. Questo perché il responsabile del trattamento è responsabile per la gestione della richiesta e la restituzione di una risposta. Non dovrebbe essere responsabile della logica aziendale della domanda. Questa è la responsabilità del modello.

L'antipattern 'Fat Controller' è dove il controller fa troppo. Ciò può portare a una serie di problemi, tra cui:

1. Duplicazione del codice in più azioni:
   Un'azione dovrebbe essere un'unica unità di lavoro, semplicemente popolando il modello e restituendo il punto di vista. Se vi trovate a ripetere il codice in più azioni, è un segno che si dovrebbe refactoring questo codice in un metodo separato.
2. Codice che è difficile da testare:
   Avere "controller grassi" potrebbe rendere difficile testare il codice. I test dovrebbero cercare di seguire tutti i percorsi possibili attraverso il codice, e questo può essere difficile se il codice non è ben strutturato e focalizzato su una sola responsabilità.
3. Codice che è difficile da mantenere:
   La sostenibilità è una preoccupazione fondamentale quando si costruiscono applicazioni. Avere 'lavello cucina' metodi di azione può facilmente portare a voi così come altri sviluppatori utilizzando il codice per apportare modifiche che rompono altre parti dell'applicazione.
4. Codice che è difficile da capire:
   Questa è una preoccupazione fondamentale per gli sviluppatori. Se si sta lavorando a un progetto con una grande base di codice, può essere difficile capire cosa sta accadendo in un'azione controller se sta facendo troppo.

### Il controllore del blog

Il blog controller è relativamente semplice. Ha 4 azioni principali (e una 'azione compatibile' per i vecchi collegamenti del blog). Questi sono:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

A loro volta queste azioni chiamano il `IBlogService` per ottenere i dati di cui hanno bisogno. La `IBlogService` è dettagliata nel [Post precedente](/blog/addingentityframeworkforblogpostspt2).

A loro volta, queste azioni sono le seguenti:

- Indice: Questo è l'elenco dei post del blog (defaults to English Language; possiamo estendere questo in seguito per consentire più lingue). Vedrai che ci vorra' un po'. `page` e `pageSize` come parametri. Questo è per la paginazione. dei risultati.
- Mostra: Questo è il post del blog individuale. Ci vuole un po' di tempo. `slug` del posto e del `language` come parametri. THis è il metodo che state usando attualmente per leggere questo post sul blog.
- Categoria: Questo è l'elenco dei post del blog per una determinata categoria. Ci vuole un po' di tempo. `category`, `page` e `pageSize` come parametri.
- Lingua: Questo mostra un post sul blog per una data lingua. Ci vuole un po' di tempo. `slug` e `language` come parametri.
- Compat: Questa è un'azione compatibile per i vecchi link del blog. Ci vuole un po' di tempo. `slug` e `language` come parametri.

### CachingCity name (optional, probably does not need a translation)

Come indicato in un [posto precedente](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) implementiamo `OutputCache` e `ResponseCahce` per nascondere i risultati dei post del blog. Questo migliora l'esperienza utente e riduce il carico sul server.

Queste sono implementate utilizzando i decoratori di azione appropriati che specificano i parametri utilizzati per l'azione (così come `hx-request` per le richieste HTMX). Per l'esame con `Index` Noi li precisiamo:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Vista

Le viste per il blog sono relativamente semplici. Sono per lo più solo una lista di post sul blog, con alcuni dettagli per ogni post. I punti di vista sono nella `Views/Blog` Cartella. I punti di vista principali sono:

### `_PostPartial.cshtml`

Questa è la vista parziale per un singolo post sul blog. E 'utilizzato all'interno della nostra `Post.cshtml` vista.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Questa è la vista parziale per una lista di post sul blog. E 'utilizzato all'interno della nostra `Index.cshtml` vista così come nella homepage.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

In questo modo si utilizza il `_ListPost` vista parziale per visualizzare i singoli post del blog insieme con il [aiutante tag paging](/blog/addpagingwithhtmx) che ci permette di leggere i post del blog.

### `_ListPost.cshtml`

La _La vista parziale di Listpost è usata per visualizzare i singoli post del blog nell'elenco. Viene utilizzato all'interno del `_BlogSummaryList` vista.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

Come si se qui abbiamo un link al singolo post del blog, le categorie per il post, le lingue in cui il post è disponibile, il riassunto del post, la data pubblicata e l'ora di lettura.

Abbiamo anche i tag link HTMX per le categorie e le lingue per permetterci di visualizzare i post localizzati e i post per una determinata categoria.

Abbiamo due modi di utilizzare HTMX qui, uno che fornisce l'URL completo e uno che è 'HTML solo' (cioè. URL). Questo perché vogliamo utilizzare l'URL completo per le categorie e le lingue, ma non abbiamo bisogno dell'URL completo per il post del singolo blog.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Questo approccio popola un URL completo per il singolo post del blog e utilizza `hx-boost` per "rafforzare" la richiesta di utilizzare HTMX (questo imposta la `hx-request` intestazione a `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

In alternativa questo approccio utilizza i tag HTMX per ottenere le categorie per i post del blog. In questo modo si utilizza il `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` e `hx-route-category` tags per ottenere le categorie per i post del blog mentre `hx-push-url` è impostato a `true` per spingere l'URL alla cronologia del browser.

E 'utilizzato anche all'interno della nostra `Index` Metodo di azione per le richieste HTMX.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Dove ci permette di restituire la visione completa o solo la visione parziale per le richieste HTMX, dando una 'SPA' come esperienza.

## Pagina web

Nella `HomeController` ci riferiamo anche a questi servizi di blog per ottenere gli ultimi post del blog per la home page. Questo è fatto nel `Index` metodo d'azione.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

Come vedrete qui, usiamo il `IBlogService` per ottenere gli ultimi post del blog per la home page. Noi usiamo anche il `GetUserInfo` metodo per ottenere le informazioni dell'utente per la home page.

Ancora una volta questo ha una richiesta HTMX di restituire la vista parziale per i post del blog per permetterci di pagina i post del blog nella home page.

## In conclusione

Nella nostra prossima parte andremo nel dettaglio straziante di come usiamo il `IMarkdownBlogService` per popolare il database con i post del blog dai file markdown. Questa è una parte chiave dell'applicazione in quanto ci permette di utilizzare i file markdown per popolare il database con i post del blog.