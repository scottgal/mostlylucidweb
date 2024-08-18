# Hantering (ohanterade) fel i ASP.NET Core

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Inledning

I alla webbprogram är det viktigt att hantera fel på ett graciöst sätt. Detta gäller särskilt i en produktionsmiljö där du vill ge en bra användarupplevelse och inte avslöja någon känslig information. I den här artikeln ska vi titta på hur man hanterar fel i en ASP.NET Core-applikation.

[TOC]

## Problemet

När ett ohanterat undantag inträffar i ett ASP.NET Core-program är standardbeteendet att returnera en generisk felsida med en statuskod på 500. Detta är inte idealiskt av flera skäl:

1. Det är fult och ger ingen bra användarupplevelse.
2. Det ger inte någon användbar information till användaren.
3. Det är ofta svårt att felsöka problemet eftersom felmeddelandet är så generiskt.
4. Det är fult; den generiska webbläsaren felsida är bara en grå skärm med viss text.

## Lösningen

I ASP.NET Core finns en snygg funktion inbyggd som gör att vi kan hantera denna typ av fel.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Vi lägger detta i vår `Program.cs` Arkivera tidigt i rörledningen. Detta kommer att fånga någon statuskod som inte är en 200 och omdirigera till `/error` rutt med statuskoden som parameter.

Vår felkontroll kommer att se ut ungefär så här:

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

Denna styrenhet kommer att hantera felet och returnera en egen vy baserat på statuskoden. Vi kan också logga den ursprungliga webbadressen som orsakade felet och skicka den till vyn.
Om vi hade en central loggning/analystjänst kunde vi logga detta fel till den tjänsten.

Våra synpunkter är följande:

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

Ganska enkelt, eller hur? Vi kan också logga felet till en loggningstjänst som Application Insights eller Serilog. På så sätt kan vi hålla reda på fel och rätta till dem innan de blir ett problem.
I vårt fall loggar vi detta som en händelse till vår Umami analytics service. På så sätt kan vi hålla reda på hur många 404 fel vi har och var de kommer ifrån.

Detta håller också din sida i enlighet med din valda layout och design.

![404 Översikt](new404.png)

## Slutsatser

Detta är ett enkelt sätt att hantera fel i en ASP.NET Core-applikation. Det ger en bra användarupplevelse och gör att vi kan hålla reda på fel. Det är en bra idé att logga fel till en loggning tjänst så att du kan hålla reda på dem och fixa dem innan de blir ett problem.