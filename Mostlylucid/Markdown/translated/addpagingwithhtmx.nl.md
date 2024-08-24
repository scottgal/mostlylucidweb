# Paging toevoegen met HTMX en ASP.NET Core met TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Inleiding

Nu dat ik heb een heleboel blog posts de homepage was het krijgen van nogal lengte, dus ik besloot om een paging mechanisme voor blog berichten toe te voegen.

Dit gaat samen met het toevoegen van volledige caching voor blog berichten om dit een snelle en efficiënte site.

Zie de [Blog Service bron](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) voor hoe dit wordt geïmplementeerd; het is echt vrij eenvoudig met behulp van de IMemoryCache.

[TOC]

### TagHelper

Ik besloot een TagHelper te gebruiken om het paging mechanisme te implementeren. Dit is een geweldige manier om de paging logica te inkapselen en herbruikbaar te maken.
Dit maakt gebruik van de [pagination taghelper van Darrel O'Neill ](https://github.com/darrel-oneil/PaginationTagHelper) Dit is opgenomen in het project als een nuget pakket.

Dit wordt vervolgens toegevoegd aan de _ViewImports.cshtml-bestand zodat het beschikbaar is voor alle weergaven.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### De TagHelper

In de _BlogSummaryList.cshtml gedeeltelijke weergave Ik heb de volgende code toegevoegd om het paging mechanisme te renderen.

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

Een paar opmerkelijke dingen hier:

1. `link-url` Dit staat de taghelper toe om de juiste url voor de paging links te genereren. In de HomeController Index methode is dit ingesteld op die actie.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

En in de Blog controller

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

Dit is ingesteld op die Url. Dit zorgt ervoor dat de pagination helper kan werken voor beide top level methode.

### HTMX-eigenschappen

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` Dit zijn allemaal HTMX eigenschappen die het mogelijk maken om de paging te werken met HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Hier gebruiken we `hx-boost="true"` Hiermee kan de pagination taghelper geen aanpassingen nodig hebben door de normale URL-generatie te onderscheppen en de huidige URL te gebruiken.

`hx-push-url="true"` om ervoor te zorgen dat de URL wordt geruild in de URL-geschiedenis van de browser (waardoor direct naar pagina's kan worden gelinkt).

`hx-target="#content"` Dit is het doel div dat zal worden vervangen door de nieuwe inhoud.

`hx-swap="show:none"` Dit is het swapeffect dat zal worden gebruikt wanneer de inhoud wordt vervangen. In dit geval voorkomt het het normale 'jump' effect dat HTMX gebruikt op swapping content.

#### CSS

Ik heb ook stijlen toegevoegd aan de main.css in mijn /src directory zodat ik de Tailwind CSS klassen kan gebruiken voor de pagination links.

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

### Controller

`page`, `page-size`, `total-items` zijn de eigenschappen die de pagination taghelper gebruikt om de paging links te genereren.
Deze worden via de controller in het gedeeltelijke zicht doorgegeven.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Blogdienst

Hier pagina en paginaMaat worden doorgegeven van de URL en de totale items worden berekend uit de blog service.

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

Hier krijgen we gewoon de berichten uit de cache, bestellen ze op datum en dan overslaan en neem het juiste aantal berichten voor de pagina.

### Conclusie

Dit was een eenvoudige toevoeging aan de site, maar het maakt het veel bruikbaarder. De HTMX integratie maakt de site meer responsief en voegt niet meer JavaScript toe aan de site.