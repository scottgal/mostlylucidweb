# Htmx med Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Inledning

Att använda HTMX med ASP.NET Core är ett bra sätt att bygga dynamiska webbapplikationer med minimal JavaScript. Med HTMX kan du uppdatera delar av din sida utan att ladda om hela sidan, vilket gör din applikation mer lyhörd och interaktiv.

Det är vad jag brukade kalla "hybrid" webbdesign där du gör sidan helt med server-side-kod och sedan använda HTMX för att uppdatera delar av sidan dynamiskt.

I den här artikeln ska jag visa dig hur du kommer igång med HTMX i en ASP.NET Core-applikation.

[TOC]

## Förutsättningar

HTMX - Htmx är ett JavaScript-paket som det lättaste sättet att inkludera det i ditt projekt är att använda en CDN. (Se också [här](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Du kan naturligtvis också ladda ner en kopia och inkludera den "manuellt" (eller använda LibMan eller npm).

## ASP.NET-bitar

Jag rekommenderar också att installera Htmx Tag Helper från [här](https://github.com/khalidabuhakmeh/Htmx.Net)

Dessa är båda från den underbara [Khalid Abuhakmeh Ordförande
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Och Htmx Nuget-paketet från [här](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Tagghjälparen låter dig göra detta:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Alternativt tillvägagångssätt.

**OBS: Detta tillvägagångssätt har en stor nackdel; det producerar inte en href för postlänken. Detta är ett problem för SEO och tillgänglighet. Det betyder också att dessa länkar kommer att misslyckas om HTMX av någon anledning inte laddar (CDNs DO går ner).**

En alternativ metod är att använda ` hx-boost="true"` Attribut och vanliga Asp.net-kärntagghjälpare. Se också  [här](https://htmx.org/docs/#hx-boost) för mer information om hx-boost (även om dokumenten är lite sparsamma).
Detta kommer att ge en normal href men kommer att fångas upp av HTMX och innehållet laddas dynamiskt.

Så här:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Delvis

HTMX fungerar bra med partiella vyer. Du kan använda HTMX för att ladda en delvy i en behållare på din sida. Detta är bra för att ladda delar av din sida dynamiskt utan en hel sida ladda om.

I denna app har vi en behållare i Layout.cshtml-filen som vi vill ladda en partiell vy i.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normalt återger det serversidans innehåll, men med hjälp av HTMX-taggen kan du se att vi riktar in oss på `hx-target="#contentcontainer"` som kommer att lasta den partiella sikten i behållaren.

I vårt projekt har vi bloggenView delvyn som vi vill lasta in i behållaren.

![img.png](project.png)

Sedan i bloggen Controller vi har

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

Du kan se här att vi har HTMX Request.IsHtmx() metod, detta kommer att återvända sant om begäran är en HTMX begäran. Om det är vi returnerar den partiella vyn, om inte vi returnerar den fullständiga vyn.

Med hjälp av detta kan vi se till att vi också stöder direkta frågor med liten verklig ansträngning.

I detta fall hänvisar vi till denna del:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Och så har vi nu ett super enkelt sätt att ladda partiella vyer på vår sida med hjälp av HTMX.