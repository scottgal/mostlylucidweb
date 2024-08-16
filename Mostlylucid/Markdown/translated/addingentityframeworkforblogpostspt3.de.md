# Hinzufügen des Entity Framework für Blog-Posts (Teil 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Sie finden alle Quellcode für die Blog-Beiträge auf [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Teile 1 und 2 der Serie über das Hinzufügen von Entity Framework zu einem.NET Core-Projekt.**

Teil 1 kann gefunden werden [Hierher](/blog/addingentityframeworkforblogpostspt1).

Teil 2 kann gefunden werden [Hierher](/blog/addingentityframeworkforblogpostspt2).

## Einleitung

In den vorherigen Teilen haben wir die Datenbank und den Kontext für unsere Blog-Posts eingerichtet und die Dienste hinzugefügt, um mit der Datenbank zu interagieren. In diesem Beitrag werden wir detailliert darlegen, wie diese Dienste jetzt mit den vorhandenen Controllern und Ansichten funktionieren.

[TOC]

## Steuergeräte

Out Controller für Blogs sind wirklich ziemlich einfach; im Einklang mit der Vermeidung der 'Fat Controller' Antimuster (ein Muster, das wir in den frühen ASP.NET MVC Tage ideintified).

### Das Fat Controller Muster in ASP.NET MVC

I MVC-Frameworks eine gute Praxis ist, so wenig wie möglich in Ihren Controller-Methoden zu tun. Dies liegt daran, dass der Controller für die Bearbeitung der Anfrage und die Rücksendung einer Antwort verantwortlich ist. Sie sollte nicht für die Geschäftslogik der Anwendung verantwortlich sein. Das ist die Verantwortung des Modells.

Die 'Fat Controller' Antimuster ist, wo der Controller zu viel tut. Dies kann zu einer Reihe von Problemen führen, darunter:

1. Vervielfältigung von Code in mehreren Aktionen:
   Eine Aktion sollte eine einzige Arbeitseinheit sein, einfach das Modell bevölkern und die Ansicht zurückgeben. Wenn Sie sich in mehreren Aktionen wiederholen, ist es ein Zeichen, dass Sie diesen Code in eine separate Methode umformulieren sollten.
2. Code, der schwer zu testen ist:
   Mit 'Fett-Controller' können Sie machen es schwierig, den Code zu testen. Tests sollten versuchen, alle möglichen Wege durch den Code zu folgen, und dies kann schwierig sein, wenn der Code nicht gut strukturiert ist und sich auf eine einzige Verantwortung konzentriert.
3. Code, der schwer zu pflegen ist:
   Nachhaltigkeit ist ein zentrales Anliegen beim Bau von Anwendungen. Mit 'Küche Spüle' Action-Methoden können leicht zu Ihnen führen sowie andere Entwickler mit dem Code, um Änderungen, die andere Teile der Anwendung zu brechen.
4. Code, der schwer zu verstehen ist:
   Dies ist ein wichtiges Anliegen für Entwickler. Wenn Sie an einem Projekt mit einer großen Codebase arbeiten, kann es schwierig sein zu verstehen, was in einer Controller-Aktion passiert, wenn es zu viel tut.

### Der Blog-Controller

Der Blog-Controller ist relativ einfach. Es hat 4 Hauptaktionen (und eine "Kompat-Aktion" für die alten Blog-Links). Diese sind:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

In der Folge nennen diese Aktionen die `IBlogService` um die Daten zu erhalten, die sie benötigen. Das `IBlogService` ist detailliert in der [vorheriger Beitrag](/blog/addingentityframeworkforblogpostspt2).

Im Gegenzug sind diese Aktionen wie folgt:

- Index: Dies ist die Liste der Blog-Posts (Standards auf Englisch Sprache; wir können diese später erweitern, um für mehrere Sprachen zu ermöglichen). Du wirst sehen, dass es dauert `page` und `pageSize` als Parameter. Das ist für die Pagination. der Ergebnisse.
- Show: Dies ist der einzelne Blog-Post. Es braucht die `slug` der Stelle und der `language` als Parameter. THIS ist die Methode, die Sie derzeit verwenden, um diesen Blog-Post zu lesen.
- Kategorie: Dies ist die Liste der Blog-Beiträge für eine bestimmte Kategorie. Es braucht die `category`, `page` und `pageSize` als Parameter.
- Sprache: Dies zeigt einen Blog-Post für eine bestimmte Sprache. Es braucht die `slug` und `language` als Parameter.
- Compat: Dies ist eine Kompatibility-Aktion für die alten Blog-Links. Es braucht die `slug` und `language` als Parameter.

### Caching

Wie in einem [früherer Posten](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) wir implementieren `OutputCache` und `ResponseCahce` um die Ergebnisse der Blog-Posts zu verbergen. Dies verbessert die Benutzererfahrung und reduziert die Belastung auf dem Server.

Diese werden mit den entsprechenden Aktionsdekoratoren durchgeführt, die die für die Aktion verwendeten Parameter (sowie `hx-request` für HTMX-Anfragen). Für Examen mit `Index` wir spezifizieren diese:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Ansichten

Die Ansichten für den Blog sind relativ einfach. Sie sind meist nur eine Liste von Blog-Posts, mit ein paar Details für jeden Beitrag. Die Ansichten sind in der `Views/Blog` Ordner. Die wichtigsten Ansichten sind:

### `_PostPartial.cshtml`

Dies ist die teilweise Ansicht für einen einzigen Blog-Post. Es wird in unserem `Post.cshtml` ................................................................................................................................

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Dies ist die teilweise Ansicht für eine Liste von Blog-Posts. Es wird in unserem `Index.cshtml` sowohl auf der Homepage als auch auf der Homepage.

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

Dabei wird die `_ListPost` Teilansicht, um die einzelnen Blog-Posts zusammen mit der [Hilfe für das Paging-Tag](/blog/addpagingwithhtmx) was uns erlaubt, die Blog-Beiträge zu blättern.

### `_ListPost.cshtml`

Das _Listpost Teilansicht wird verwendet, um die einzelnen Blog-Posts in der Liste anzuzeigen. Es wird innerhalb der `_BlogSummaryList` ................................................................................................................................

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

Wie Sie hier sehen, haben wir einen Link zu den einzelnen Blog-Post, die Kategorien für den Beitrag, die Sprachen, in denen der Beitrag verfügbar ist, die Zusammenfassung des Beitrags, das veröffentlichte Datum und die Lesezeit.

Wir haben auch HTMX-Link-Tags für die Kategorien und die Sprachen, damit wir die lokalisierten Beiträge und die Beiträge für eine bestimmte Kategorie anzeigen können.

Wir haben hier zwei Möglichkeiten HTMX zu verwenden, eine, die die volle URL und eine gibt, die nur 'HTML' ist (d.h.. keine URL). Denn wir wollen die volle URL für die Kategorien und Sprachen verwenden, aber wir brauchen nicht die volle URL für den einzelnen Blog-Post.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Dieser Ansatz bevölkert eine vollständige URL für den einzelnen Blog-Post und nutzt `hx-boost` um die Anfrage nach HTMX zu 'treiben' (dies setzt die `hx-request` header zu `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Alternativ verwendet dieser Ansatz die HTMX-Tags, um die Kategorien für die Blog-Beiträge zu erhalten. Dabei wird die `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` und `hx-route-category` Tags, um die Kategorien für die Blog-Beiträge zu erhalten, während `hx-push-url` ist eingestellt auf `true` um die URL in den Browserverlauf zu schieben.

Es wird auch in unserem `Index` Aktionsmethode für die HTMX-Anfragen.

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

Wo es uns ermöglicht, entweder die volle Ansicht oder nur die partielle Ansicht für HTMX-Anfragen zurückzugeben, was eine 'SPA'-ähnliche Erfahrung gibt.

## Startseite

In der `HomeController` wir beziehen uns auch auf diese Blog-Dienste, um die neuesten Blog-Beiträge für die Homepage zu erhalten. Dies geschieht in der `Index` Aktionsmethode.

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

Wie Sie hier sehen werden, benutzen wir die `IBlogService` um die neuesten Blog-Posts für die Homepage zu erhalten. Wir verwenden auch die `GetUserInfo` Methode, um die Benutzerinformationen für die Homepage zu erhalten.

Auch dies hat eine HTMX-Anforderung, die Teilansicht für die Blog-Posts zurückzugeben, damit wir die Blog-Posts in die Homepage einbinden können.

## Schlussfolgerung

In unserem nächsten Teil werden wir in qualvolle Details gehen, wie wir die `IMarkdownBlogService` um die Datenbank mit den Blog-Posts aus den Markdown-Dateien zu bevölkern. Dies ist ein Schlüsselteil der Anwendung, da es uns erlaubt, die Markdown-Dateien zu verwenden, um die Datenbank mit den Blog-Posts zu bevölkern.