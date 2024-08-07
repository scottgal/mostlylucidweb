# Htmx con Asp.Net Core

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Introducción

El uso de HTMX con ASP.NET Core es una gran manera de construir aplicaciones web dinámicas con JavaScript mínimo. HTMX le permite actualizar partes de su página sin una recarga de página completa, haciendo que su aplicación se sienta más sensible e interactiva.

Es lo que solía llamar diseño web 'híbrido' donde se representa la página completamente utilizando el código del lado del servidor y luego utilizar HTMX para actualizar partes de la página de forma dinámica.

En este artículo, te mostraré cómo empezar con HTMX en una aplicación ASP.NET Core.

[TOC]

## Requisitos previos

HTMX - Htmx es un paquete JavaScript de la manera más fácil de incluirlo en su proyecto es utilizar un CDN. (Vea[aquí](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Por supuesto, también puede descargar una copia e incluirla'manualmente' (o utilizar LibMan o npm).

## Bits ASP.NET

También recomiendo instalar el Htmx Tag Helper desde[aquí](https://github.com/khalidabuhakmeh/Htmx.Net)

Estos dos son de la maravillosa[Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Y el paquete Htmx Nuget de[aquí](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

El ayudante de etiqueta le permite hacer esto:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Enfoque alternativo.

**NOTA: Este enfoque tiene un inconveniente importante; no produce un href para el enlace post. Esto es un problema para el SEO y la accesibilidad. También significa que estos enlaces fallarán si HTMX por alguna razón no se carga (CDNS DO go down).**

Un enfoque alternativo es el uso de la` hx-boost="true"`atributo y asp.net normal core tag helpers. Ver[aquí](https://htmx.org/docs/#hx-boost)para más información sobre hx-boost (aunque los documentos son un poco escasos).
Esto producirá una href normal, pero será interceptado por HTMX y el contenido cargado dinámicamente.

Por lo tanto, lo siguiente:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Parcial

HTMX funciona bien con vistas parciales. Puede utilizar HTMX para cargar una vista parcial en un contenedor de su página. Esto es ideal para cargar partes de su página dinámicamente sin una recarga completa de la página.

En esta aplicación tenemos un contenedor en el archivo Layout.cshtml en el que queremos cargar una vista parcial.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normalmente renderiza el contenido del lado del servidor, pero usando el ayudante de etiquetas HTMX sobre usted puede ver que nos dirigimos`hx-target="#contentcontainer"`que cargará la vista parcial en el contenedor.

En nuestro proyecto tenemos la vista parcial de BlogView que queremos cargar en el contenedor.

![img.png](project.png)

Entonces en el Controlador de Blog que tenemos

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

Puede ver aquí que tenemos el método HTMX Request.IsHtmx(), esto regresará true si la solicitud es una solicitud HTMX. Si es así, devolvemos la vista parcial, si no devolvemos la vista completa.

Utilizando esto podemos asegurarnos de que también apoyamos la consulta directa con poco esfuerzo real.

En este caso nuestro punto de vista completo se refiere a este parcial:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Y ahora tenemos una manera súper simple de cargar vistas parciales en nuestra página usando HTMX.