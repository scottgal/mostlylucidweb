# Lägga till packning med HTMX och ASP.NET Core med TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50 ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------</datetime>

## Inledning

Nu när jag har ett gäng blogginlägg hemsidan blev ganska lång så jag bestämde mig för att lägga till en personsökning mekanism för blogginlägg.

Detta går tillsammans med att lägga till full caching för blogginlägg för att göra detta en snabb och effektiv webbplats.

Se tabellen nedan. [Källa för bloggtjänst](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) för hur detta genomförs; det är verkligen ganska enkelt att använda IMemoryCache.

[TOC]

### Tagghjälp

Jag bestämde mig för att använda en TagHelper för att genomföra personsökningsmekanismen. Detta är ett bra sätt att inkapsla personsökningslogiken och göra den återanvändbar.
Detta använder sig av [pagineringstagghelper från Darrel O'Neill ](https://github.com/darrel-oneil/PaginationTagHelper) Detta ingår i projektet som ett nugetpaket.

Detta läggs sedan till _VisaImports.cshtml-filen så den är tillgänglig för alla vyer.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Tagghjälparen

I och med att _BlogSummaryList.cshtml partiell vy Jag lade till följande kod för att göra personsökning mekanismen.

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

Några anmärkningsvärda saker här:

1. `link-url` Detta gör det möjligt för tagghjälparen att generera rätt webbadress för personsökningslänkarna. I HomeController Index metod detta är inställd på den åtgärden.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Och i bloggen styrenhet

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

Det här är inställt på den där URl. Detta säkerställer att pagineringshjälparen kan arbeta för antingen toppnivåmetoden.

### HTMX- egenskaper

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` Dessa är alla HTMX egenskaper som tillåter personsökningen att arbeta med HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Här använder vi `hx-boost="true"` Detta gör det möjligt för tagghjälparen med paginering att inte behöva några ändringar genom att avlyssna den normala URL-genereringen och använda den aktuella webbadressen.

`hx-push-url="true"` För att säkerställa att webbadressen byts ut i webbläsarens URL-historik (som tillåter direktlänkning till sidor).

`hx-target="#content"` Detta är målet div som kommer att ersättas med det nya innehållet.

`hx-swap="show:none"` Detta är den swapeffekt som kommer att användas när innehållet byts ut. I detta fall förhindrar det den normala "hopp"-effekt som HTMX använder vid byte av innehåll.

#### Försäkringstekniska avsättningar beräknade som helhet – bruttosoliditetsgradens exponeringsvärde – exponeringar enligt schablonmetoden

Jag lade också till stilar till main.css i min /src katalog så att jag kan använda Tailwind CSS klasser för paginering länkar.

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

### Styrenhet

`page`, `page-size`, `total-items` är de egenskaper som pagination taghelper använder för att generera personsökning länkar.
Dessa förs in i delvyn från den regulatorn.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Bloggtjänst

Här sida och sidaStorlek skickas in från URL och de totala objekten beräknas från bloggtjänsten.

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

Här får vi helt enkelt inläggen från cache, beställa dem efter datum och sedan hoppa över och ta rätt antal inlägg för sidan.

### Slutsatser

Detta var ett enkelt tillägg till webbplatsen men det gör det mycket mer användbart. Integrationen av HTMX gör att webbplatsen känns mer lyhörd samtidigt som den inte lägger till mer JavaScript på webbplatsen.