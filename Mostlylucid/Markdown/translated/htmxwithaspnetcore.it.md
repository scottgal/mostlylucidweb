# Htmx con Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Introduzione

Utilizzare HTMX con ASP.NET Core è un ottimo modo per costruire applicazioni web dinamiche con JavaScript minimale. HTMX consente di aggiornare parti della tua pagina senza ricaricare l'intera pagina, rendendo la tua applicazione più reattiva e interattiva.

E 'quello che ho usato per chiamare 'hybrid' web design in cui si rende la pagina completamente utilizzando il codice lato server e poi utilizzare HTMX per aggiornare le parti della pagina in modo dinamico.

In questo articolo, vi mostrerò come iniziare con HTMX in un'applicazione ASP.NET Core.

[TOC]

## Prerequisiti

HTMX - Htmx è un pacchetto JavaScript il modo più semplice per includerlo nel tuo progetto è usare un CDN. (Vedere [qui](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Naturalmente è possibile scaricare anche una copia e includerla'manuale' (o utilizzare LibMan o npm).

## ASP.NET Bits

Raccomando anche l'installazione del Tag Helper Htmx da [qui](https://github.com/khalidabuhakmeh/Htmx.Net)

Questi sono entrambi dal meraviglioso [Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

E il pacchetto Htmx Nuget da [qui](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Il tag helper ti permette di fare questo:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Approccio alternativo.

**NOTA: Questo approccio ha uno svantaggio maggiore; non produce un href per il link post. Questo è un problema per SEO e accessibilità. Questo significa anche che questi collegamenti falliranno se HTMX per qualche motivo non carica (CDNs DO andare giù).**

Un approccio alternativo è quello di utilizzare il ` hx-boost="true"` attributo e helper core asp.net normali. Vedi  [qui](https://htmx.org/docs/#hx-boost) per maggiori informazioni su hx-boost (anche se i documenti sono un po'scarsi).
Ciò produrrà un normale href ma sarà intercettato da HTMX e il contenuto caricato dinamicamente.

Quindi, come segue:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Partials

HTMX funziona bene con viste parziali. È possibile utilizzare HTMX per caricare una vista parziale in un contenitore sulla tua pagina. Questo è ottimo per caricare parti della tua pagina dinamicamente senza ricaricare l'intera pagina.

In questa applicazione abbiamo un contenitore nel file Layout.cshtml in cui vogliamo caricare una vista parziale.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normalmente rende il contenuto lato server, ma utilizzando l'helper di tag HTMX su di voi si può vedere che abbiamo obiettivo `hx-target="#contentcontainer"` che caricherà la vista parziale nel contenitore.

Nel nostro progetto abbiamo il BlogVisualizza vista parziale che vogliamo caricare nel contenitore.

![img.png](project.png)

Poi nel Blog Controller abbiamo

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

Potete vedere qui abbiamo il metodo HTMX Request.IsHtmx(), questo tornerà vero se la richiesta è una richiesta HTMX. Se è restituiamo la vista parziale, se non restituiamo la visione completa.

Utilizzando questo possiamo garantire che sosteniamo anche interrogare direttamente con poco sforzo reale.

In questo caso il nostro punto di vista completo si riferisce a questo parziale:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

E così ora abbiamo un modo super semplice per caricare le viste parziali nella nostra pagina utilizzando HTMX.