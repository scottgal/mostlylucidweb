# Htmx mit Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Einleitung

Die Verwendung von HTMX mit ASP.NET Core ist eine gute Möglichkeit, dynamische Web-Anwendungen mit minimalem JavaScript zu erstellen. HTMX ermöglicht es Ihnen, Teile Ihrer Seite zu aktualisieren, ohne dass eine ganze Seite neu geladen wird, so dass Ihre Anwendung sich responsiver und interaktiver anfühlt.

Es ist, was ich 'hybrid' Web-Design, wo Sie die Seite vollständig mit serverseitigen Code und dann HTMX verwenden, um Teile der Seite dynamisch zu aktualisieren.

In diesem Artikel zeige ich Ihnen, wie Sie mit HTMX in einer ASP.NET Core Anwendung beginnen.

[TOC]

## Voraussetzungen

HTMX - Htmx ist ein JavaScript-Paket, auf dem Sie es einfach in Ihr Projekt aufnehmen können, indem Sie ein CDN verwenden. (siehe [Hierher](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Sie können natürlich auch eine Kopie downloaden und sie'manuell' (oder LibMan oder npm) einschließen.

## ASP.NET-Bits

Ich empfehle auch die Installation der Htmx Tag Helper von [Hierher](https://github.com/khalidabuhakmeh/Htmx.Net)

Diese sind beide von der wunderbaren [Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Und das Htmx Nuget-Paket von [Hierher](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Mit dem Tag-Helfer können Sie Folgendes tun:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Alternativer Ansatz.

**HINWEIS: Dieser Ansatz hat einen großen Nachteil; er erzeugt kein Href für den Postlink. Dies ist ein Problem für SEO und Zugänglichkeit. Es bedeutet auch, dass diese Links fehlschlagen, wenn HTMX aus irgendeinem Grund nicht geladen wird (CDNs DO gehen nach unten).**

Ein alternativer Ansatz besteht darin, die ` hx-boost="true"` Attribut und normale asp.net Core Tag Helfer. Siehe  [Hierher](https://htmx.org/docs/#hx-boost) für weitere Informationen über hx-boost (obwohl die docs etwas spärlich sind).
Dies wird einen normalen href ausgeben, wird aber von HTMX und dem dynamisch geladenen Inhalt abgefangen.

So wie folgt:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Teile

HTMX funktioniert gut mit Teilansichten. Sie können HTMX verwenden, um eine Teilansicht in einen Container auf Ihrer Seite zu laden. Dies ist ideal zum Laden von Teilen Ihrer Seite dynamisch ohne einen vollständigen Seitenneuladen.

In dieser App haben wir einen Container in der Datei Layout.cshtml, in den wir eine Teilansicht laden möchten.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normalerweise wird der Server-Seiteninhalt gerendert, aber mit dem HTMX-Tag-Helfer können Sie sehen, dass wir Ziel `hx-target="#contentcontainer"` die die Teilansicht in den Container laden wird.

In unserem Projekt haben wir die Teilansicht BlogView, die wir in den Container laden wollen.

![img.png](project.png)

Dann haben wir im Blog Controller

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

Sie können hier sehen, dass wir die HTMX Request.IsHtmx() Methode haben, dies wird true zurückgeben, wenn die Request eine HTMX Request ist. Wenn es ist, geben wir die partielle Ansicht zurück, wenn nicht, geben wir die vollständige Ansicht zurück.

Damit können wir sicherstellen, dass wir auch direkte Abfragen mit wenig Aufwand unterstützen.

In diesem Fall bezieht sich unsere volle Meinung auf diesen Teil:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Und so haben wir jetzt eine super einfache Möglichkeit, Teilansichten mit HTMX in unsere Seite zu laden.