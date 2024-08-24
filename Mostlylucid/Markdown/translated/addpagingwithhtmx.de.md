# Paging mit HTMX und ASP.NET Core mit TagHelper hinzufügen

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Einleitung

Nun, da ich eine Reihe von Blog-Posts die Homepage war immer eher Länge, so dass ich beschlossen, einen Paging-Mechanismus für Blog-Posts hinzuzufügen.

Dies geht zusammen mit dem Hinzufügen voller Caching für Blog-Beiträge, um dies eine schnelle und effiziente Website zu machen.

Siehe [Quelle des Blog-Services](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) für wie dies umgesetzt wird; es ist wirklich ziemlich einfach mit dem IMemoryCache.

[TOC]

### TagHelper

Ich beschloss, einen TagHelper zu verwenden, um den Paging-Mechanismus zu implementieren. Dies ist ein großartiger Weg, um die Logik zu verkapseln und wiederverwendbar zu machen.
Dabei wird die [Pagination Taghelper von Darrel O'Neill ](https://github.com/darrel-oneil/PaginationTagHelper) Dies ist im Projekt als Nuget-Paket enthalten.

Dies wird dann zu den _ViewImports.cshtml-Datei, so dass es für alle Ansichten zur Verfügung steht.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Der TagHelper

In der _BlogSummaryList.cshtml Teilansicht Ich habe den folgenden Code hinzugefügt, um den Paging-Mechanismus zu rendern.

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

Ein paar bemerkenswerte Dinge hier:

1. `link-url` Dies ermöglicht es dem Taghelper, die richtige URL für die Paging-Links zu generieren. In der HomeController Index Methode ist dies auf diese Aktion eingestellt.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Und im Blog Controller

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

Das ist auf diesen Url eingestellt. Dadurch wird sichergestellt, dass der Paginationshelfer für beide Top-Level-Methoden arbeiten kann.

### HTMX-Eigenschaften

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` Dies sind alle HTMX-Eigenschaften, die es dem Server ermöglichen, mit HTMX zu arbeiten.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Hier benutzen wir `hx-boost="true"` Dies ermöglicht es dem Pagination Taghelper, keine Änderungen zu benötigen, indem es die normale URL-Generierung abfängt und die aktuelle URL verwendet.

`hx-push-url="true"` um sicherzustellen, dass die URL im URL-Verlauf des Browsers ausgetauscht wird (was eine direkte Verknüpfung mit Seiten ermöglicht).

`hx-target="#content"` Dies ist der Ziel-div, der durch den neuen Inhalt ersetzt wird.

`hx-swap="show:none"` Dies ist der Swap-Effekt, der verwendet wird, wenn der Inhalt ersetzt wird. In diesem Fall verhindert es den normalen 'Sprung'-Effekt, den HTMX beim Austausch von Inhalten nutzt.

#### CSS

Ich habe auch Stile zu den main.css in meinem /src-Verzeichnis hinzugefügt, so dass ich die Tailwind CSS-Klassen für die Pagination-Links verwenden kann.

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

### Steuergerät

`page`, `page-size`, `total-items` sind die Eigenschaften, die der Pagination Taghelper verwendet, um die Paging-Links zu generieren.
Diese werden vom Controller in die Teilansicht überführt.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Blog-Service

Hier werden Seite und SeiteSize von der URL übergeben und die gesamten Elemente aus dem Blog-Dienst berechnet.

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

Hier bekommen wir einfach die Beiträge aus dem Cache, bestellen sie nach Datum und dann überspringen und nehmen Sie die richtige Anzahl von Beiträgen für die Seite.

### Schlußfolgerung

Dies war eine einfache Ergänzung der Website, aber es macht es viel mehr nutzbar. Die Integration von HTMX macht die Website reaktionsfreudiger und fügt nicht mehr JavaScript zu der Website hinzu.