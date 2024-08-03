# Htmx avec Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Présentation

L'utilisation de HTMX avec ASP.NET Core est un excellent moyen de construire des applications web dynamiques avec un minimum de JavaScript. HTMX vous permet de mettre à jour des parties de votre page sans recharger une page complète, ce qui rend votre application plus réactive et interactive.

C'est ce que j'appelais la conception web "hybride" où vous rendez la page entièrement en utilisant le code côté serveur, puis utilisez HTMX pour mettre à jour des parties de la page dynamiquement.

Dans cet article, je vais vous montrer comment commencer avec HTMX dans une application ASP.NET Core.

[TOC]

## Préalables

HTMX - Htmx est un paquet JavaScript que la manière la plus simple de l'inclure dans votre projet est d'utiliser un CDN. (Voir [Ici.](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Vous pouvez bien sûr également télécharger une copie et l'inclure « manuellement » (ou utiliser LibMan ou npm).

## Bits ASP.NET

Je recommande également d'installer le Htmx Tag Helper à partir de [Ici.](https://github.com/khalidabuhakmeh/Htmx.Net)

Ils sont tous les deux de la merveilleuse [Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Et le paquet Htmx Nuget de [Ici.](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

L'assistant de tag vous permet de faire ceci:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Une autre approche.

**NOTE: Cette approche a un inconvénient majeur; elle ne produit pas de href pour le lien post. C'est un problème pour le référencement et l'accessibilité. Cela signifie également que ces liens échoueront si HTMX pour une raison quelconque ne se charge pas (les CDNs DO descendent).**

Une autre approche consiste à utiliser ` hx-boost="true"` attribut et les helpers de base asp.net normaux. Voir  [Ici.](https://htmx.org/docs/#hx-boost) pour plus d'informations sur hx-boost (bien que les docs soient un peu clairsemés).
Cela affichera un href normal mais sera intercepté par HTMX et le contenu chargé dynamiquement.

Ainsi, comme suit:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Partiellement

HTMX fonctionne bien avec des vues partielles. Vous pouvez utiliser HTMX pour charger une vue partielle dans un conteneur sur votre page. C'est idéal pour charger des parties de votre page dynamiquement sans recharger une page complète.

Dans cette application, nous avons un conteneur dans le fichier Layout.cshtml dans lequel nous voulons charger une vue partielle.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normalement, il rend le contenu côté serveur, mais en utilisant l'aide de la balise HTMX à propos de vous pouvez voir que nous ciblez `hx-target="#contentcontainer"` qui chargera la vue partielle dans le conteneur.

Dans notre projet, nous avons la vue partielle BlogView que nous voulons charger dans le conteneur.

![img.png](project.png)

Alors dans le contrôleur de blog nous avons

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

Vous pouvez voir ici que nous avons la méthode HTMX Request.IsHtmx(), ceci retournera true si la requête est une requête HTMX. Si c'est nous retournons la vue partielle, sinon nous retournons la vue complète.

En utilisant cela, nous pouvons nous assurer que nous soutenons également la requête directe avec peu d'effort réel.

Dans ce cas, notre point de vue complet se réfère à ce partiel:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Et donc nous avons maintenant un moyen super simple de charger des vues partielles dans notre page en utilisant HTMX.