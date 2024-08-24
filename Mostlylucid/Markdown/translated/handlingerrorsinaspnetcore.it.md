# Errori di gestione (senza gestione) in ASP.NET Core

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Introduzione

In qualsiasi applicazione web è importante gestire gli errori con grazia. Ciò è particolarmente vero in un ambiente di produzione in cui si desidera fornire una buona esperienza utente e non esporre alcuna informazione sensibile. In questo articolo vedremo come gestire gli errori in un'applicazione ASP.NET Core.

[TOC]

## Il problema

Quando un'eccezione non gestita si verifica in un'applicazione ASP.NET Core, il comportamento predefinito è quello di restituire una pagina di errore generico con un codice di stato di 500. Questo non è l'ideale per una serie di ragioni:

1. E 'brutto e non fornisce una buona esperienza utente.
2. Non fornisce alcuna informazione utile all'utente.
3. Spesso è difficile debug il problema perché il messaggio di errore è così generico.
4. E 'brutto; la pagina generica errore del browser è solo una schermata grigia con un po 'di testo.

## La soluzione

In ASP.NET Core c'è una bella funzione build in cui ci permette di gestire questo tipo di errori.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Abbiamo messo questo nel nostro `Program.cs` Archiviare all'inizio della pipeline. Questo catturerà qualsiasi codice di stato che non è un 200 e reindirizzare alla `/error` route con il codice di stato come parametro.

Il nostro controller degli errori assomiglierà a questo:

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

Questo controller gestirà l'errore e restituirà una vista personalizzata in base al codice di stato. Possiamo anche registrare l'URL originale che ha causato l'errore e passarlo alla vista.
Se avessimo un servizio centrale di registrazione / analisi potremmo registrare questo errore a quel servizio.

Le nostre viste sono le seguenti:

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

Semplice, vero? Possiamo anche registrare l'errore a un servizio di registrazione come Application Insights o Serilog. In questo modo possiamo tenere traccia degli errori e correggerli prima che diventino un problema.
Nel nostro caso lo registriamo come evento al nostro servizio di analisi Umami. In questo modo possiamo tenere traccia di quanti errori 404 abbiamo e da dove vengono.

Questo mantiene anche la tua pagina in conformità con il layout e il design scelto.

![404 Pagina](new404.png)

## In conclusione

Questo è un modo semplice per gestire gli errori in un'applicazione ASP.NET Core. Fornisce una buona esperienza utente e ci permette di tenere traccia degli errori. E 'una buona idea per registrare gli errori di un servizio di registrazione in modo da poter tenere traccia di loro e risolverli prima che diventino un problema.