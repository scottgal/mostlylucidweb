# Umgang mit (unhandhabten) Fehlern in ASP.NET Core

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Einleitung

In jeder Web-Anwendung ist es wichtig, Fehler anmutig zu behandeln. Dies gilt vor allem in einer Produktionsumgebung, in der Sie eine gute Benutzererfahrung bieten und keine sensiblen Informationen aufdecken möchten. In diesem Artikel werden wir uns ansehen, wie man Fehler in einer ASP.NET Core-Anwendung behandelt.

[TOC]

## Das Problem

Wenn eine nicht behandelte Ausnahme in einer ASP.NET Core Anwendung auftritt, ist das Standardverhalten, eine generische Fehlerseite mit einem Statuscode von 500 zurückzugeben. Dies ist aus mehreren Gründen nicht ideal:

1. Es ist hässlich und bietet keine gute Benutzererfahrung.
2. Es liefert keine nützlichen Informationen für den Benutzer.
3. Es ist oft schwer, das Problem zu debuggen, weil die Fehlermeldung so generisch ist.
4. Es ist hässlich; die generische Browser-Fehlerseite ist nur ein grauer Bildschirm mit etwas Text.

## Die Lösung

In ASP.NET Core gibt es eine ordentliche Funktion eingebaut, die uns erlaubt, diese Art von Fehlern zu handhaben.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Wir haben das in unsere `Program.cs` Datei früh in der Pipeline. Dies wird jeden Statuscode fangen, der nicht ein 200 ist und umleiten zu den `/error` Route mit dem Statuscode als Parameter.

Unser Fehler-Controller wird so aussehen:

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

Dieser Controller wird mit dem Fehler umgehen und eine benutzerdefinierte Ansicht basierend auf dem Statuscode zurückgeben. Wir können auch die ursprüngliche URL, die den Fehler verursacht hat, protokollieren und an die Ansicht übergeben.
Wenn wir einen zentralen Logging/Analytics-Dienst hätten, könnten wir diesen Fehler bei diesem Dienst protokollieren.

Unsere Ansichten sind wie folgt:

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

Ziemlich einfach, oder? Wir können den Fehler auch bei einem Logging-Dienst wie Application Insights oder Serilog protokollieren. Auf diese Weise können wir Fehler verfolgen und beheben, bevor sie zu einem Problem werden.
In unserem Fall protokollieren wir dies als Ereignis bei unserem Umami Analytics Service. Auf diese Weise können wir verfolgen, wie viele 404 Fehler wir haben und woher sie kommen.

Dies hält auch Ihre Seite in Übereinstimmung mit Ihrem gewählten Layout und Design.

![404 Seite](new404.png)

## Schlussfolgerung

Dies ist ein einfacher Weg, um Fehler in einer ASP.NET Core-Anwendung zu handhaben. Es bietet eine gute Benutzererfahrung und ermöglicht es uns, Fehler im Überblick zu behalten. Es ist eine gute Idee, Fehler bei einem Logging-Service zu protokollieren, damit Sie sie verfolgen und beheben können, bevor sie zu einem Problem werden.