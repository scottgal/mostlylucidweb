# Handling (unhandled) fouten in ASP.NET Core

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Inleiding

In elke webapplicatie is het belangrijk om fouten sierlijk aan te pakken. Dit is vooral waar in een productieomgeving waar u een goede gebruikerservaring wilt bieden en geen gevoelige informatie wilt onthullen. In dit artikel bekijken we hoe je fouten kunt verwerken in een ASP.NET Core applicatie.

[TOC]

## Het probleem

Wanneer een niet-afgehandelde uitzondering optreedt in een ASP.NET Core applicatie, is het standaardgedrag om een generieke foutpagina met een statuscode van 500 terug te sturen. Dit is niet ideaal om een aantal redenen:

1. Het is lelijk en biedt geen goede gebruikerservaring.
2. Het geeft geen nuttige informatie aan de gebruiker.
3. Het is vaak moeilijk om het probleem te debuggen omdat de foutmelding zo generiek is.
4. Het is lelijk; de generieke browser foutpagina is gewoon een grijs scherm met wat tekst.

## De oplossing

In ASP.NET Core is er een nette functie ingebouwd die ons in staat stelt om dit soort fouten te verwerken.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

We stoppen dit in onze `Program.cs` file vroeg op in de pijplijn. Dit zal vangen elke status code die geen 200 en omleiden naar de `/error` route met de statuscode als parameter.

Onze foutcontroller zal er ongeveer zo uitzien:

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

Deze controller zal de fout verwerken en een aangepaste weergave op basis van de statuscode retourneren. We kunnen ook de originele URL loggen die de fout veroorzaakte en doorgeven aan de weergave.
Als we een centrale logging / analytics service hadden konden we deze fout loggen naar die service.

Onze meningen zijn als volgt:

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

Vrij simpel toch? We kunnen de fout ook loggen naar een logservice zoals Application Insights of Serilog. Op deze manier kunnen we fouten bijhouden en repareren voordat ze een probleem worden.
In ons geval loggen we dit als een evenement naar onze Umami analytics service. Op deze manier kunnen we bijhouden hoeveel 404 fouten we hebben en waar ze vandaan komen.

Dit houdt ook uw pagina in overeenstemming met uw gekozen lay-out en ontwerp.

![404 Blz.](new404.png)

## Conclusie

Dit is een eenvoudige manier om fouten te verwerken in een ASP.NET Core applicatie. Het biedt een goede gebruikerservaring en stelt ons in staat om fouten bij te houden. Het is een goed idee om fouten in te loggen bij een logservice zodat je ze kunt bijhouden en repareren voordat ze een probleem worden.


