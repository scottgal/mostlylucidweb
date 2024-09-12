# Caché simple 'Donut Hole' con HTMX

# Introducción

El almacenamiento en caché de agujeros de Donut puede ser una técnica útil donde desea guardar en caché ciertos elementos de una página, pero no todos. Sin embargo, puede ser difícil de implementar. En este post te mostraré cómo implementar una simple técnica de caché de agujeros de rosquilla usando HTMX.

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024-09-12T16:00</datetime>
[TOC]

# El problema

Un problema que estaba teniendo con este sitio es que quería utilizar fichas anti-falsificación con mis formularios. Se trata de una buena práctica de seguridad para prevenir los ataques de falsificación de solicitudes cruzadas (CSRF). Sin embargo, estaba causando un problema con el almacenamiento en caché de las páginas. El token Anti-falsificación es único para cada solicitud de página, así que si cacheas la página, el token será el mismo para todos los usuarios. Esto significa que si un usuario envía un formulario, el token será inválido y la presentación del formulario fallará. ASP.NET Core evita esto al desactivar todo caché bajo petición donde se utiliza el token Anti-falsificación. Esta es una buena práctica de seguridad, pero significa que la página no estará en caché en absoluto. Esto no es ideal para un sitio como este donde el contenido es mayormente estático.

# La solución

Una forma común de rodear esto es el caché de "agujero de donut" donde se cachea la mayoría de la página, pero ciertos elementos. Hay un montón de maneras de lograr esto en ASP.NET Core usando el marco de vista parcial sin embargo es complejo de implementar y a menudo requiere paquetes específicos y configuración. Quería una solución más simple.

Como ya utilizo el excelente [HTMX](https://htmx.org/examples/lazy-load/) en este proyecto hay una forma súper simple de obtener funcionalidad dinámica de 'hoyo de rosquilla' cargando dinámicamente Parcials con HTMX.
Ya he blogueado sobre [utilizando Tokens AntiForgeryRequest con Javascript](/blog/addingxsrfforjavascript) Sin embargo, de nuevo la cuestión era que esto efectivamente deshabilitó el almacenamiento en caché para la página.

Ahora puedo restablecer esta funcionalidad al usar HTMX para cargar parciales dinámicamente.

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

Muy simple, ¿verdad? Todo lo que esto hace es llamar a la única línea de código en el controlador que devuelve la vista parcial. Esto significa que el token Anti-falsificación se genera en el servidor y la página se puede guardar en caché como de costumbre. La vista parcial se carga dinámicamente por lo que el token sigue siendo único para cada petición.

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

En lo parcial todavía tenemos la forma simple con el símbolo Anti-falsificación.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement"  x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
    <label class="input input-sm dark:bg-custom-dark-bg bg-white input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.300ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow  input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-b-neutral-600 dark:border-gray-300   rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                    'dark:hover:bg-blue-dark hover:bg-blue-light': true
                }"
                class="cursor-pointer text-sm p-2 m-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

Esto entonces encapsula todo el código para la búsqueda tipoahead y cuando se envía tira del token y lo añade a la petición (exactamente como antes).

```javascript
        let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
```

# Conclusión

Esta es una manera súper simple de conseguir el "hoyo de la rosquilla" caching con HTMX. Es una gran manera de obtener los beneficios del almacenamiento en caché sin la complejidad de un paquete adicional. Espero que encuentre esto útil. Avísame si tienes alguna pregunta en los comentarios a continuación.