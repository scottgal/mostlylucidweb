# Añadiendo Paging con HTMX y ASP.NET Core con TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Introducción

Ahora que tengo un montón de posts de blog la página de inicio estaba recibiendo bastante longitud, así que decidí añadir un mecanismo de paginación para los posts de blog.

Esto va junto con la adición de caché completo para las publicaciones de blog para hacer de este un sitio rápido y eficiente.

Ver la[Blog Fuente del servicio](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs)para cómo se implementa esto; es realmente bastante simple usando el IMemoryCache.

[TOC]

### TagHelper

Decidí usar un TagHelper para implementar el mecanismo de paginación. Esta es una gran manera de encapsular la lógica de paginación y hacerla reutilizable.
Esto utiliza la[taghelper de paginación de Darrel O'Neill](https://github.com/darrel-oneil/PaginationTagHelper)esto se incluye en el proyecto como un paquete de pepitas.

Esto se añade a continuación a la_Archivo ViewImports.cshtml por lo que está disponible para todas las vistas.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### El ayudante de etiquetas

En el_BlogSummaryList.cshtml vista parcial He añadido el siguiente código para representar el mecanismo de paginación.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Algunas cosas notables aquí:

1. `link-url`Esto permite que el taghelper genere la url correcta para los enlaces de paginación. En el método Índice de HomeController esto se ajusta a esa acción.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Y en el controlador de Blog

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Esto se ajusta a la URl. Esto asegura que el ayudante de paginación puede trabajar para cualquiera de los métodos de nivel superior.

### Propiedades HTMX

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap`Estas son todas las propiedades HTMX que permiten que la paginación funcione con HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Aquí lo usamos.`hx-boost="true"`Esto permite que el taghelper de paginación no necesite ninguna modificación interceptando su generación normal de URL y usando la URL actual.

`hx-push-url="true"`para asegurarse de que la URL se intercambie en el historial de URL del navegador (que permite enlazar directamente a las páginas).

`hx-target="#content"`este es el div destino que será reemplazado por el nuevo contenido.

`hx-swap="show:none"`Este es el efecto swap que se usará cuando se reemplace el contenido. En este caso evita el efecto'saltar' normal que HTMX usa en el intercambio de contenido.

#### CSS

También añadí estilos al main.css en mi directorio /src que me permite usar las clases CSS de Tailwind para los enlaces de paginación.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Contralor

`page`, `page-size`, `total-items`son las propiedades que el taghelper de paginación utiliza para generar los enlaces de paginación.
Estos se pasan a la vista parcial desde el controlador.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Servicio de blogs

Aquí la página y la páginaEl tamaño se pasan desde la URL y el total de elementos se calculan desde el servicio del blog.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Aquí simplemente obtenemos los mensajes de la caché, ordenarlos por fecha y luego saltar y tomar el número correcto de mensajes para la página.

### Conclusión

Esta fue una simple adición al sitio, pero lo hace mucho más utilizable. La integración HTMX hace que el sitio se sienta más sensible al tiempo que no añade más JavaScript al sitio.