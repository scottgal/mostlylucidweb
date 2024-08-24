# Aggiunta di Paging con HDMX e ASP.NET Core con TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Introduzione

Ora che ho un sacco di post sul blog la home page era sempre piuttosto lunga così ho deciso di aggiungere un meccanismo di pagamento per i post sul blog.

Questo va avanti con l'aggiunta di cache complete per i post del blog per rendere questo un sito veloce ed efficiente.

Vedere il [Sorgente di servizio del blog](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) per come questo è implementato; è davvero abbastanza semplice utilizzando l'IMEMORYCache.

[TOC]

### TagHelper

Ho deciso di usare un TagHelper per implementare il meccanismo di ricerca. Questo è un ottimo modo per incapsulare la logica del paging e renderla riutilizzabile.
In questo modo si utilizza il [Taghelper paginazione di Darrel O'Neill ](https://github.com/darrel-oneil/PaginationTagHelper) questo è incluso nel progetto come pacchetto nuget.

Questo viene poi aggiunto al _VisualizzaImports.cshtml file in modo che sia disponibile per tutte le viste.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Il tagHelper

Nella _BlogSummaryList.cshtml vista parziale Ho aggiunto il seguente codice per rendere il meccanismo di paginatura.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Alcune cose importanti qui:

1. `link-url` Questo permette al taghelper di generare l'url corretto per i collegamenti di ricerca. Nel metodo HomeController Index questo è impostato a quell'azione.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

E nel controller del blog

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Questo è impostato su quell'URL. Questo assicura che l'aiutante di paginazione può funzionare per entrambi i metodi di livello superiore.

### Proprietà HTMX

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` queste sono tutte proprietà HTMX che permettono al paging di lavorare con HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Qui usiamo `hx-boost="true"` Questo permette al taghelper paginazione di non aver bisogno di alcuna modifica intercettando la sua normale generazione di URL e utilizzando l'URL corrente.

`hx-push-url="true"` per garantire che l'URL venga scambiato nella cronologia dell'URL del browser (che permette il collegamento diretto alle pagine).

`hx-target="#content"` questo è il div di destinazione che sarà sostituito con il nuovo contenuto.

`hx-swap="show:none"` questo è l'effetto swap che verrà utilizzato quando il contenuto viene sostituito. In questo caso impedisce il normale effetto "salto" che HTMX utilizza per lo scambio di contenuti.

#### CSS

Ho anche aggiunto gli stili alla main.css nella mia directory /src che mi permette di usare le classi CSS Tailwind per i link di paginazione.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Controllore

`page`, `page-size`, `total-items` sono le proprietà che il taghelper paginazione utilizza per generare i collegamenti di ricerca.
Questi vengono passati nella vista parziale dal controller.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Servizio blog

Qui pagina e paginaLe dimensioni sono passate dall'URL e gli elementi totali sono calcolati dal servizio blog.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Qui abbiamo semplicemente ottenere i messaggi dalla cache, ordinarli per data e poi saltare e prendere il numero corretto di messaggi per la pagina.

### Conclusione

Questa era una semplice aggiunta al sito, ma lo rende molto più utilizzabile. L'integrazione HTMX rende il sito più reattivo senza aggiungere più JavaScript al sito.