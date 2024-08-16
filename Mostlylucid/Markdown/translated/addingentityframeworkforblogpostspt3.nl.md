# Het toevoegen van een entiteitskader voor blogberichten (deel 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

U vindt alle broncode voor de blog berichten op [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Deel 1 & 2 van de reeks over het toevoegen van Entity Framework aan een.NET Core project.**

Deel 1 is te vinden [Hier.](/blog/addingentityframeworkforblogpostspt1).

Deel 2 is te vinden [Hier.](/blog/addingentityframeworkforblogpostspt2).

## Inleiding

In de vorige delen hebben we de database en de context voor onze blogberichten opgezet, en de diensten toegevoegd om te communiceren met de database. In deze post zullen we in detail vertellen hoe deze diensten nu werken met de bestaande controllers en meningen.

[TOC]

## Controllers

Out controllers voor Blogs zijn echt vrij eenvoudig; in lijn met het vermijden van de 'Fat Controller' antipattern (een patroon dat we ideïntified vroeg in de ASP.NET MVC dagen).

### Het Fat Controller patroon in ASP.NET MVC

I MVC kaders een goede praktijk is om zo weinig mogelijk te doen in uw controller methoden. Dit komt omdat de controller verantwoordelijk is voor de behandeling van het verzoek en het terugsturen van een antwoord. Zij mag niet verantwoordelijk zijn voor de bedrijfslogica van de aanvraag. Dit is de verantwoordelijkheid van het model.

De 'Fat Controller' antipatroon is waar de controller te veel doet. Dit kan leiden tot een aantal problemen, waaronder:

1. Duplicatie van code in meerdere acties:
   Een actie moet één enkele eenheid van het werk zijn, eenvoudigweg het model bevolken en het beeld teruggeven. Als je vindt dat je code herhaalt in meerdere acties, is het een teken dat je deze code moet herfactoreren in een aparte methode.
2. Code die moeilijk te testen is:
   Door het hebben van'vet controllers' kunt u het moeilijk maken om de code te testen. Testen moet proberen alle mogelijke paden door de code te volgen, en dit kan moeilijk zijn als de code niet goed gestructureerd is en gericht is op één enkele verantwoordelijkheid.
3. Code die moeilijk te handhaven is:
   Instandhouding is een belangrijk punt van zorg bij het bouwen van toepassingen. Het hebben van 'keuken spoelbak' actiemethoden kan gemakkelijk leiden tot u evenals andere ontwikkelaars met behulp van de code om veranderingen die andere delen van de toepassing breken.
4. Code die moeilijk te begrijpen is:
   Dit is een belangrijke zorg voor ontwikkelaars. Als je werkt aan een project met een grote codebase, kan het moeilijk zijn om te begrijpen wat er gebeurt in een controller actie als het te veel doet.

### De blogcontroller

De blog controller is relatief eenvoudig. Het heeft 4 hoofdacties (en één 'compat action' voor de oude blog links). Dit zijn:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

Deze acties roepen op hun beurt de `IBlogService` Om de gegevens te krijgen die ze nodig hebben. De `IBlogService` is gedetailleerd in de [vorige post](/blog/addingentityframeworkforblogpostspt2).

Deze acties zijn op hun beurt als volgt:

- Index: Dit is de lijst van blogberichten (standaarden naar Engels Language; we kunnen dit later uitbreiden om meerdere talen mogelijk te maken). Je zult zien dat het nodig is. `page` en `pageSize` als parameters. Dit is voor paginatie. van de resultaten.
- Show: Dit is de individuele blogpost. Het neemt de `slug` van het ambt en de `language` als parameters. THis is de methode die u momenteel gebruikt voor het lezen van deze blog post.
- Categorie: Dit is de lijst van blogberichten voor een bepaalde categorie. Het neemt de `category`, `page` en `pageSize` als parameters.
- Taal: Dit toont een blog post voor een bepaalde taal. Het neemt de `slug` en `language` als parameters.
- Compat: Dit is een comptibilty actie voor de oude blog links. Het neemt de `slug` en `language` als parameters.

### Caching

Zoals vermeld in een [eerdere post](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) we implementeren `OutputCache` en `ResponseCahce` om de resultaten van de blogberichten te cacheren. Dit verbetert de gebruikerservaring en vermindert de belasting op de server.

Deze worden uitgevoerd met behulp van de passende actiedecoratoren die de parameters specificeren die voor de actie worden gebruikt (zowel als `hx-request` voor HTMX-verzoeken). Voor exampel met `Index` we specificeren deze:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Weergaven

De meningen voor de blog zijn relatief eenvoudig. Ze zijn meestal gewoon een lijst van blog posts, met een paar details voor elke post. De meningen zijn in de `Views/Blog` map. De belangrijkste standpunten zijn:

### `_PostPartial.cshtml`

Dit is de gedeeltelijke weergave voor een enkele blog post. Het wordt gebruikt in onze `Post.cshtml` uitzicht.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Dit is de gedeeltelijke weergave voor een lijst van blogberichten. Het wordt gebruikt in onze `Index.cshtml` uitzicht evenals op de homepage.

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

Dit maakt gebruik van de `_ListPost` gedeeltelijke weergave om de individuele blog berichten samen met de [paging tag helper](/blog/addpagingwithhtmx) die ons in staat stelt om de blog berichten te pagina.

### `_ListPost.cshtml`

De _Listpost gedeeltelijke weergave wordt gebruikt om de afzonderlijke blogberichten in de lijst weer te geven. Het wordt gebruikt binnen de `_BlogSummaryList` uitzicht.

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

Zoals u'll se hier hebben we een link naar de individuele blog post, de categorieën voor de post, de talen de post is beschikbaar in, de samenvatting van de post, de gepubliceerde datum en de leestijd.

We hebben ook HTMX link tags voor de categorieën en de talen om ons in staat te stellen de gelokaliseerde berichten en de berichten voor een bepaalde categorie weer te geven.

We hebben hier twee manieren om HTMX te gebruiken, één die de volledige URL geeft en één die alleen 'HTML' is (d.w.z. geen URL). Dit komt omdat we de volledige URL willen gebruiken voor de categorieën en de talen, maar we hebben niet de volledige URL nodig voor de individuele blogpost.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Deze aanpak bevolkt een volledige URL voor de individuele blog post en toepassingen `hx-boost` om het verzoek om HTMX te gebruiken te'versterken' (dit stelt de `hx-request` koptekst naar `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Als alternatief gebruikt deze aanpak de HTMX tags om de categorieën voor de blog berichten te krijgen. Dit maakt gebruik van de `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` en `hx-route-category` tags om de categorieën voor de blog berichten te krijgen terwijl `hx-push-url` is ingesteld op `true` om de URL naar de browsergeschiedenis te pushen.

Het wordt ook gebruikt in onze `Index` Actiemethode voor de HTMX-verzoeken.

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

Waar het ons in staat stelt om ofwel de volledige weergave terug te geven of gewoon de gedeeltelijke weergave voor HTMX-verzoeken, wat een 'SPA'-achtige ervaring geeft.

## Startpagina

In de `HomeController` we verwijzen ook naar deze blog diensten om de nieuwste blog berichten voor de startpagina te krijgen. Dit wordt gedaan in de `Index` actiemethode.

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

Zoals je hier zult zien gebruiken we de `IBlogService` om de nieuwste blog berichten voor de startpagina te krijgen. Wij maken ook gebruik van de `GetUserInfo` methode om de gebruiker informatie voor de homepage te krijgen.

Opnieuw dit heeft een HTMX verzoek om de gedeeltelijke weergave voor de blog berichten terug te geven om ons toe te staan om de blog berichten te pagina in de homepage.

## Conclusie

In ons volgende deel gaan we dieper in op ondraaglijke details over hoe we gebruik maken van de `IMarkdownBlogService` om de database te vullen met de blog berichten uit de markdown bestanden. Dit is een belangrijk onderdeel van de toepassing, omdat het ons in staat stelt om de markdown bestanden te gebruiken om de database te bevolken met de blog berichten.