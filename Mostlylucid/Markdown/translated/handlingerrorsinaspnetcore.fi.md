# ASP.NET Coren käsittelyvirheet (käsittelemättömät)

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Johdanto

Kaikissa verkkosovelluksissa on tärkeää käsitellä virheitä sulavasti. Tämä pätee erityisesti tuotanto-ympäristössä, jossa halutaan tarjota hyvää käyttökokemusta ja olla paljastamatta arkaluonteisia tietoja. Tässä artikkelissa tarkastelemme, miten ASP.NET Core -sovelluksessa voi käsitellä virheitä.

[TÄYTÄNTÖÖNPANO

## Ongelma

Kun ASP.NET Core -sovelluksessa tapahtuu käsittelemätön poikkeus, oletuksena on palauttaa yleinen virhesivu, jonka tilakoodi on 500. Tämä ei ole ihanteellista monesta syystä:

1. Se on ruma eikä tarjoa hyvää käyttökokemusta.
2. Se ei anna käyttäjälle mitään hyödyllistä tietoa.
3. Ongelmaa on usein vaikea vioittaa, koska virheilmoitus on niin yleinen.
4. Se on ruma; yleinen selainvirhesivu on vain harmaa näyttö, jossa on tekstiä.

## Ratkaisu

ASP.NET Coressa on näppärä ominaisuus, jonka avulla voimme käsitellä tällaisia virheitä.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Me laitamme tämän meidän `Program.cs` arkistoidaan aikaisin putkeen. Tämä nappaa minkä tahansa tilakoodin, joka ei ole 200 ja suuntaa `/error` reitti, jossa parametrina on tilakoodi.

Virheohjaimemme näyttää tältä:

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

Tämä ohjain käsittelee virheen ja palauttaa tilakoodiin perustuvan mukautetun näkymän. Voimme myös kirjata virheen aiheuttaneen alkuperäisen URL-osoitteen ja siirtää sen näkymään.
Jos meillä olisi keskitetty kirjautumis- ja analytiikkapalvelu, voisimme kirjata tämän virheen kyseiseen palveluun.

Näkemyksemme ovat seuraavat:

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

Aika yksinkertaista, vai mitä? Voimme myös kirjata virheen hakupalveluun, kuten Application Insightsiin tai Serilogiin. Näin voimme seurata virheitä ja korjata ne ennen kuin niistä tulee ongelmia.
Kirjaamme tämän meidän tapauksessamme tapahtumana Umami-analytiikkapalveluumme. Näin voimme seurata, kuinka monta 404 virhettä meillä on ja mistä ne tulevat.

Tämä pitää myös sivusi valitun ulkoasun ja muotoilun mukaisesti.

![404 Sivu](new404.png)

## Johtopäätöksenä

Tämä on yksinkertainen tapa käsitellä virheitä ASP.NET Core -sovelluksessa. Se tarjoaa hyvän käyttökokemuksen ja mahdollistaa virheiden seuraamisen. On hyvä kirjata virheet hakkuupalveluun, jotta voit seurata niitä ja korjata ne ennen kuin niistä tulee ongelmia.