# Htmx met Asp.Net-kern

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Inleiding

Het gebruik van HTMX met ASP.NET Core is een geweldige manier om dynamische webapplicaties te bouwen met minimale JavaScript. Met HTMX kunt u delen van uw pagina bijwerken zonder een volledige pagina opnieuw te laden, waardoor uw toepassing meer responsief en interactief voelt.

Het is wat ik gebruikte om 'hybride' webdesign te noemen waar je de pagina volledig maakt met behulp van server-side code en dan gebruik HTMX om delen van de pagina dynamisch bij te werken.

In dit artikel laat ik je zien hoe je kunt beginnen met HTMX in een ASP.NET Core applicatie.

[TOC]

## Vereisten

HTMX - Htmx is een JavaScript pakket de gemakkelijke manier om het op te nemen in uw project is om een CDN te gebruiken. (Zie[Hier.](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

U kunt natuurlijk ook een kopie downloaden en deze 'handmatig' meenemen (of LibMan of npm gebruiken).

## ASP.NET Bits

Ik adviseer ook het installeren van de Htmx Tag Helper van[Hier.](https://github.com/khalidabuhakmeh/Htmx.Net)

Deze zijn allebei van de prachtige[Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

En het Htmx Nuget pakket van[Hier.](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

De tag helper laat je dit doen:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Alternatieve aanpak.

**OPMERKING: Deze aanpak heeft een groot nadeel; het produceert geen href voor de post link. Dit is een probleem voor SEO en toegankelijkheid. Het betekent ook dat deze links zullen mislukken als HTMX om een of andere reden niet laadt (CDNs DO gaan naar beneden).**

Een andere benadering is het gebruik van de` hx-boost="true"`attribuut en normale asp.net core tag helpers.[Hier.](https://htmx.org/docs/#hx-boost)voor meer info over hx-boost (hoewel de docs een beetje schaars zijn).
Dit zal een normale href uitvoeren, maar zal worden onderschept door HTMX en de inhoud dynamisch geladen.

Dus als volgt:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Gedeeltelijke

HTMX werkt goed met gedeeltelijke weergaven. U kunt HTMX gebruiken om een gedeeltelijke weergave in een container op uw pagina te laden. Dit is geweldig voor het dynamisch laden van delen van uw pagina zonder een volledige pagina opnieuw laden.

In deze app hebben we een container in het Layout.cshtml bestand waar we een gedeeltelijke weergave in willen laden.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normaal maakt het de server zijde inhoud, maar met behulp van de HTMX tag helper over u kunt zien dat we targe`hx-target="#contentcontainer"`die de gedeeltelijke weergave in de container zal laden.

In ons project hebben we de BlogView gedeeltelijke weergave die we in de container willen laden.

![img.png](project.png)

Dan in de Blog Controller hebben we

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

U kunt hier zien dat we de HTMX Request.IsHtmx() methode hebben, dit zal waar terugkeren als het verzoek een HTMX verzoek is. Als we de gedeeltelijke weergave retourneren, als we niet de volledige weergave teruggeven.

Met behulp hiervan kunnen we ervoor zorgen dat we ook direct vragen ondersteunen met weinig echte inspanning.

In dit geval verwijst onze volledige visie naar deze gedeeltelijke:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

En dus hebben we nu een super eenvoudige manier om gedeeltelijke weergaven in onze pagina te laden met behulp van HTMX.