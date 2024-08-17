# Manejo de errores (no manejados) en el núcleo de ASP.NET

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Introducción

En cualquier aplicación web es importante manejar los errores con gracia. Esto es especialmente cierto en un entorno de producción donde desea proporcionar una buena experiencia de usuario y no exponer ninguna información sensible. En este artículo veremos cómo manejar los errores en una aplicación ASP.NET Core.

[TOC]

## El problema

Cuando se produce una excepción no manipulada en una aplicación ASP.NET Core, el comportamiento predeterminado es devolver una página de error genérica con un código de estado de 500. Esto no es ideal por una serie de razones:

1. Es feo y no proporciona una buena experiencia de usuario.
2. No proporciona ninguna información útil al usuario.
3. A menudo es difícil depurar el problema porque el mensaje de error es tan genérico.
4. Es feo; la página de error del navegador genérico es sólo una pantalla gris con un poco de texto.

## La solución

En ASP.NET Core hay una compilación de características ordenada en la que nos permite manejar este tipo de errores.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Ponemos esto en nuestro `Program.cs` archivo desde el principio en la tubería. Esto captura cualquier código de estado que no es un 200 y redirigir a la `/error` ruta con el código de estado como parámetro.

Nuestro controlador de errores se verá algo así:

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

Este controlador manejará el error y devolverá una vista personalizada basada en el código de estado. También podemos registrar la URL original que causó el error y pasarlo a la vista.
Si tuviéramos un servicio central de registro / análisis podríamos registrar este error en ese servicio.

Nuestros dictámenes son los siguientes:

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

Bastante simple, ¿verdad? También podemos registrar el error en un servicio de registro como Application Insights o Serilog. De esta manera podemos hacer un seguimiento de los errores y solucionarlos antes de que se conviertan en un problema.
En nuestro caso lo registramos como un evento en nuestro servicio de análisis de Umami. De esta manera podemos hacer un seguimiento de cuántos 404 errores tenemos y de dónde vienen.

Esto también mantiene su página de acuerdo con su diseño y diseño elegido.

![404 Página](new404.png)

## Conclusión

Esta es una forma sencilla de manejar errores en una aplicación ASP.NET Core. Proporciona una buena experiencia de usuario y nos permite hacer un seguimiento de los errores. Es una buena idea registrar errores en un servicio de registro para que pueda realizar un seguimiento de ellos y solucionarlos antes de que se conviertan en un problema.