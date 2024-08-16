# Añadiendo marco de entidad para entradas de blog (Parte 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Usted puede encontrar todo el código fuente para las entradas del blog en [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Partes 1 y 2 de la serie sobre la adición de Entity Framework a un proyecto.NET Core.**

Parte 1 se puede encontrar [aquí](/blog/addingentityframeworkforblogpostspt1).

Parte 2 se puede encontrar [aquí](/blog/addingentityframeworkforblogpostspt2).

## Introducción

En las partes anteriores configuramos la base de datos y el contexto para nuestros posts de blog, y agregamos los servicios para interactuar con la base de datos. En este post, detallaremos cómo estos servicios ahora funcionan con los controladores y vistas existentes.

[TOC]

## Contralores

Los controladores out para Blogs son realmente bastante simples; en línea con evitar el antipatrón "Controlador de grasa" (un patrón que ideintificamos al principio de los días MVC ASP.NET).

### El patrón de Controlador de grasa en ASP.NET MVC

I MVC frameworks una buena práctica es hacer lo menos posible en sus métodos de controlador. Esto se debe a que el controlador es responsable de manejar la solicitud y devolver una respuesta. No debe ser responsable de la lógica empresarial de la aplicación. Esta es la responsabilidad del modelo.

El antipatrón "Controlador de grasa" es donde el controlador hace demasiado. Esto puede dar lugar a una serie de problemas, entre ellos:

1. Duplicación de código en múltiples acciones:
   Una acción debe ser una sola unidad de trabajo, simplemente poblando el modelo y devolviendo la vista. Si se encuentra repitiendo código en múltiples acciones, es un signo de que debe refactorizar este código en un método separado.
2. Código que es difícil de probar:
   Al tener 'controladores gordos' puede ser difícil probar el código. Las pruebas deben intentar seguir todos los caminos posibles a través del código, y esto puede ser difícil si el código no está bien estructurado y se centra en una sola responsabilidad.
3. Código que es difícil de mantener:
   La mantenibilidad es una preocupación clave cuando se construyen aplicaciones. Tener métodos de acción 'cocina fregadero' puede fácilmente conducir a usted, así como otros desarrolladores que utilizan el código para hacer cambios que rompen otras partes de la aplicación.
4. Código que es difícil de entender:
   Esta es una preocupación clave para los desarrolladores. Si usted está trabajando en un proyecto con una base de código grande, puede ser difícil entender lo que está sucediendo en una acción controladora si está haciendo demasiado.

### El controlador del blog

El controlador del blog es relativamente simple. Tiene 4 acciones principales (y una 'acción compatible' para los antiguos enlaces del blog). Se trata de:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

A su vez, estas acciones llaman a la `IBlogService` para obtener los datos que necesitan. Los `IBlogService` se detalla en el [Puesto anterior](/blog/addingentityframeworkforblogpostspt2).

A su vez, estas acciones son las siguientes:

- Índice: Esta es la lista de publicaciones de blog (por defecto a Inglés Idioma; podemos extender esto más tarde para permitir varios idiomas). Ya verás que hace falta. `page` y `pageSize` como parámetros. Esto es para la paginación. de los resultados.
- Mostrar: Esta es la entrada de blog individual. Se necesita el `slug` del puesto y de la `language` como parámetros. This es el método que estás utilizando actualmente para leer este post de blog.
- Categoría: Esta es la lista de entradas de blog para una categoría dada. Se necesita el `category`, `page` y `pageSize` como parámetros.
- Idioma: Esto muestra una entrada de blog para un idioma dado. Se necesita el `slug` y `language` como parámetros.
- Compat: Esta es una acción complaciente para los antiguos enlaces del blog. Se necesita el `slug` y `language` como parámetros.

### Caché

Como se menciona en un [Cargo anterior](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) implementamos `OutputCache` y `ResponseCahce` para ocultar los resultados de las publicaciones del blog. Esto mejora la experiencia del usuario y reduce la carga en el servidor.

Estos se implementan utilizando los decoradores de acción apropiados que especifican los parámetros utilizados para la acción (así como `hx-request` para solicitudes HTMX). Para el examen con `Index` especificamos estos:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Dictámenes

Las vistas para el blog son relativamente simples. Son en su mayoría sólo una lista de entradas de blog, con algunos detalles para cada entrada. Los puntos de vista están en el `Views/Blog` carpeta. Los puntos de vista principales son los siguientes:

### `_PostPartial.cshtml`

Esta es la vista parcial de una sola entrada de blog. Se utiliza dentro de nuestro `Post.cshtml` vista.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Esta es la vista parcial de una lista de entradas de blog. Se utiliza dentro de nuestro `Index.cshtml` vista, así como en la página principal.

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

Esto utiliza la `_ListPost` vista parcial para mostrar las entradas individuales del blog junto con el [Ayudante de etiqueta de paginación](/blog/addpagingwithhtmx) lo que nos permite paginar los posts del blog.

### `_ListPost.cshtml`

Los _La vista parcial de Listpost se utiliza para mostrar las publicaciones individuales del blog en la lista. Se utiliza dentro de la `_BlogSummaryList` vista.

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

Como usted estará aquí tenemos un enlace a la entrada de blog individual, las categorías para el post, los idiomas en los que el post está disponible, el resumen del post, la fecha publicada y la hora de lectura.

También tenemos etiquetas de enlace HTMX para las categorías y los idiomas para permitirnos mostrar los posts localizados y los posts para una categoría dada.

Tenemos dos formas de usar HTMX aquí, una que da la URL completa y otra que es 'HTML solamente' (es decir. sin URL). Esto se debe a que queremos utilizar la URL completa para las categorías y los idiomas, pero no necesitamos la URL completa para el post de blog individual.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Este enfoque pobla una URL completa para cada entrada de blog y utiliza `hx-boost` para 'impulsar' la solicitud de uso de HTMX (esto establece el `hx-request` encabezado a `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Alternativamente, este enfoque utiliza las etiquetas HTMX para obtener las categorías de los posts de blog. Esto utiliza la `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` y `hx-route-category` etiquetas para obtener las categorías para los posts del blog mientras `hx-push-url` se establece a `true` para empujar la URL al historial del navegador.

También se utiliza dentro de nuestro `Index` Método de acción para las solicitudes HTMX.

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

Donde nos permite devolver la vista completa o solo la vista parcial para solicitudes HTMX, dando una experiencia "SPA".

## Página principal

En el `HomeController` También nos referimos a estos servicios de blog para obtener los últimos posts de blog para la página de inicio. Esto se hace en el `Index` método de acción.

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

Como verán aquí, usamos el `IBlogService` para obtener los últimos posts de blog para la página de inicio. También utilizamos el `GetUserInfo` método para obtener la información del usuario para la página de inicio.

Una vez más esto tiene una solicitud de HTMX para devolver la vista parcial de los posts del blog para permitirnos página de los posts del blog en la página de inicio.

## Conclusión

En nuestra próxima parte vamos a entrar en detalles insoportables de cómo utilizamos el `IMarkdownBlogService` para poblar la base de datos con las publicaciones del blog de los archivos Markdown. Esta es una parte clave de la aplicación, ya que nos permite utilizar los archivos Markdown para poblar la base de datos con las publicaciones del blog.