# Lägga till Entity Framework för blogginlägg (Del 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Du kan hitta alla källkoden för blogginläggen på [GitHub Ordförande](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Del 1 och 2 i serien om att lägga till Entity Framework till ett.NET Core-projekt.**

Del 1 kan hittas [här](/blog/addingentityframeworkforblogpostspt1).

Del 2 kan hittas [här](/blog/addingentityframeworkforblogpostspt2).

## Inledning

I de tidigare delarna har vi satt upp databasen och sammanhanget för våra blogginlägg, och lagt till tjänsterna för att interagera med databasen. I det här inlägget kommer vi att redogöra för hur dessa tjänster nu fungerar med de befintliga kontrollanterna och synpunkterna.

[TOC]

## Styrenheter

Ut regulatorer för bloggar är verkligen ganska enkel; i linje med att undvika "Fat Controller" antipattern (ett mönster som vi identierade tidigt i ASP.NET MVC dagar).

### Mönstret för fettkontroll i ASP.NET MVC

I MVC ramverk en bra praxis är att göra så lite som möjligt i din controller metoder. Detta beror på att den personuppgiftsansvarige är ansvarig för att hantera begäran och returnera ett svar. Det bör inte vara ansvarigt för affärslogiken i ansökan. Det är modellens ansvar.

"Fat Controller" antipattern är där regulatorn gör för mycket. Detta kan leda till ett antal problem, bland annat följande:

1. Duplicering av kod i flera åtgärder:
   En åtgärd bör vara en enda arbetsenhet, som helt enkelt befolkar modellen och återger bilden. Om du hittar dig själv upprepa kod i flera åtgärder, är det ett tecken på att du bör refaktor denna kod i en separat metod.
2. Kod som är svår att testa:
   Genom att ha "fettregulatorer" kan du göra det svårt att testa koden. Testning bör försöka följa alla möjliga vägar genom koden, och detta kan vara svårt om koden inte är välstrukturerad och fokuserad på ett enda ansvar.
3. Kod som är svår att upprätthålla:
   Underhållsbarhet är en viktig fråga när man bygger applikationer. Att ha "kökssänka" handlingsmetoder kan lätt leda till dig och andra utvecklare som använder koden för att göra ändringar som bryter andra delar av programmet.
4. Kod som är svår att förstå:
   Detta är en viktig fråga för utvecklare. Om du arbetar med ett projekt med en stor kodbas, kan det vara svårt att förstå vad som händer i en styrenhet handling om det gör för mycket.

### Bloggkontrollen

Bloggen controller är relativt enkel. Den har 4 huvudsakliga åtgärder (och en "compat action" för de gamla blogglänkarna). Dessa är:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

Dessa åtgärder innebär i sin tur att `IBlogService` för att få de data de behöver. I detta sammanhang är det viktigt att se till att `IBlogService` finns beskrivet i [tidigare inlägg](/blog/addingentityframeworkforblogpostspt2).

Dessa åtgärder är i sin tur följande:

- Index: Detta är listan över blogginlägg (defaults till engelska språket; vi kan förlänga detta senare för att tillåta flera språk). Du kommer att se att det krävs `page` och `pageSize` Som parametrar. Det här är för paginering. av resultaten.
- Visa: Detta är det individuella blogginlägget. Det krävs `slug` för tjänsten och `language` Som parametrar. Tis är den metod du för närvarande använder för att läsa detta blogginlägg.
- Kategori: Detta är listan över blogginlägg för en viss kategori. Det krävs `category`, `page` och `pageSize` Som parametrar.
- Språk: Detta visar ett blogginlägg för ett visst språk. Det krävs `slug` och `language` Som parametrar.
- Compat: Detta är en compatibilty åtgärd för de gamla blogglänkarna. Det krävs `slug` och `language` Som parametrar.

### Caching

Som nämnts i en [tidigare inlägg](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) vi genomför `OutputCache` och `ResponseCahce` för att cache resultaten av blogginläggen. Detta förbättrar användarupplevelsen och minskar belastningen på servern.

Dessa genomförs med hjälp av lämpliga åtgärdsdekoratörer som anger de parametrar som används för åtgärden (samt `hx-request` för HTMX-förfrågningar). För provspel med `Index` Vi specificerar följande:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Vyer

Utsikterna för bloggen är relativt enkla. De är oftast bara en lista över blogginlägg, med några detaljer för varje inlägg. Åsikterna finns i `Views/Blog` mapp. De viktigaste synpunkterna är följande:

### `_PostPartial.cshtml`

Detta är delvyn för ett enda blogginlägg. Det används inom vår `Post.cshtml` Visa.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Detta är delvyn för en lista med blogginlägg. Det används inom vår `Index.cshtml` visa såväl som på hemsidan.

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

Detta använder sig av `_ListPost` partiell vy för att visa de enskilda blogginläggen tillsammans med [personsökningstagshjälp](/blog/addpagingwithhtmx) vilket gör att vi kan sida blogginlägg.

### `_ListPost.cshtml`

I detta sammanhang är det viktigt att se till att _Listpost delvy används för att visa de enskilda blogginläggen i listan. Det används inom `_BlogSummaryList` Visa.

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

Som du ser här har vi en länk till det individuella blogginlägget, kategorierna för inlägget, de språk som inlägget finns tillgängligt i, sammanfattningen av inlägget, det publicerade datumet och lästiden.

Vi har även HTMX länktaggar för kategorierna och språken så att vi kan visa de lokaliserade inläggen och inläggen för en viss kategori.

Vi har två sätt att använda HTMX här, ett som ger den fullständiga webbadressen och ett som är "HTML endast" (dvs. ingen webbadress). Detta beror på att vi vill använda den fullständiga URL:en för kategorierna och språken, men vi behöver inte den fullständiga URL:en för det enskilda blogginlägget.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Detta tillvägagångssätt fyller en fullständig webbadress för det enskilda blogginlägget och använder `hx-boost` att "boost" begäran om att använda HTMX (det här ställer in `hx-request` Rubrik till `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Alternativt detta tillvägagångssätt använder HTMX taggar för att få kategorierna för blogginlägg. Detta använder sig av `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` och `hx-route-category` taggar för att få kategorierna för blogginläggen medan `hx-push-url` är inställd på `true` för att trycka på webbadressen till webbläsarens historik.

Det används också inom vår `Index` Åtgärdsmetod för HTMX-förfrågningar.

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

Där det gör det möjligt för oss att antingen returnera den fullständiga vyn eller bara den partiella vyn för HTMX-förfrågningar, vilket ger en "SPA" liknande upplevelse.

## Hemsida

I och med att `HomeController` Vi hänvisar också till dessa bloggtjänster för att få de senaste blogginläggen för hemsidan. Detta görs i `Index` Åtgärdsmetod.

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

Som ni ser här inne använder vi `IBlogService` för att få de senaste blogginläggen för hemsidan. Vi använder också `GetUserInfo` metod för att få användarinformation till hemsidan.

Återigen har detta en HTMX begäran om att returnera delvyn för blogginläggen så att vi kan sida blogginläggen på hemsidan.

## Slutsatser

I nästa del kommer vi att gå in på outgrundliga detaljer om hur vi använder `IMarkdownBlogService` för att fylla databasen med blogginlägg från markdown-filerna. Detta är en viktig del av programmet eftersom det gör att vi kan använda markdown-filer för att fylla databasen med blogginlägg.