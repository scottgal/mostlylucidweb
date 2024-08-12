# Caché de núcleo ASP.NET con HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Introducción

Caching es una técnica importante tanto para mejorar la experiencia del usuario cargando contenido más rápido como para reducir la carga en su servidor. En este artículo le mostraré cómo utilizar las características de caché integradas de ASP.NET Core con HTMX para almacenar contenido en caché en el lado del cliente.

[TOC]

## Configuración

En ASP.NET Core, hay dos tipos de Caching ofrecidos

- Reponse Cache - Se trata de datos que se guardan en caché en el cliente o en servidores intermediarios de procy (o ambos) y se utilizan para guardar en caché toda la respuesta a una solicitud.
- Caché de salida - Se trata de datos que se guardan en caché en el servidor y se utiliza para guardar en caché la salida de una acción de controlador.

Para configurar estos en ASP.NET Core necesita añadir un par de servicios en su`Program.cs`archivo

### Caché de respuesta

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Caché de salida

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Caché de respuesta

Si bien es posible configurar la Caché de respuesta en su`Program.cs`a menudo es un poco inflexible (especialmente cuando se utilizan solicitudes HTMX como he descubierto). Puede configurar la Caché de respuesta en las acciones de su controlador mediante el uso de la`ResponseCache`atributo.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Esto ocultará la respuesta durante 300 segundos y variará la caché por el`hx-request`encabezado y el`page`y`pageSize`parámetros de consulta. También estamos configurando el`Location`a`Any`lo que significa que la respuesta se puede guardar en caché en el cliente, en servidores proxy intermediarios, o en ambos.

Aquí la`hx-request`encabezado es el encabezado que HTMX envía con cada solicitud. Esto es importante, ya que le permite guardar en caché la respuesta de forma diferente en función de si se trata de una solicitud HTMX o una petición normal.

Esta es nuestra corriente`Index`método de acción. Yo puede ver que aceptamos un parámetro de tamaño de página y página aquí y hemos añadido estos como variables por las claves de consulta en el`ResponseCache`Atributo. Significa que las respuestas son 'indexadas' por estas claves y almacenan contenido diferente basado en éstas.

Dentro de la acción que también tenemos`if(Request.IsHtmx())`esto se basa en el[Paquete HTMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)y, esencialmente, comprueba la misma`hx-request`cabecera que estamos usando para variar la caché. Aquí devolvemos una vista parcial si la petición es de HTMX.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## Caché de salida

Caché de salida es el servidor equivalente a Caché de respuesta. Cachea la salida de una acción controladora. En esencia, el servidor web almacena el resultado de una solicitud y la sirve para solicitudes posteriores.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Aquí estamos cacheando la salida de la acción del controlador durante 3600 segundos y variando la caché por el`hx-request`encabezado y el`page`y`pageSize`parámetros de consulta.
Como estamos almacenando el lado del servidor de datos durante un tiempo significativo (los mensajes sólo se actualizan con un impulso Docker) esto es más largo que el Caché de Respuesta; en realidad podría ser infinito en nuestro caso, pero 3600 segundos es un buen compromiso.

Al igual que con el Caché Respuesta que estamos utilizando el`hx-request`cabecera para variar la caché en función de si la solicitud es de HTMX o no.

## Conclusión

Caching es una poderosa herramienta para mejorar el rendimiento de su aplicación. Al utilizar las funciones de caché integradas de ASP.NET Core, puede fácilmente cachear contenido en el lado del cliente o del servidor. Al utilizar HTMX puede cachear contenido en el lado del cliente y servir vistas parciales para mejorar la experiencia del usuario.