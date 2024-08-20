# Htmx Asp.Net Corella

<datetime class="hidden">2024-08-01T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## Johdanto

HTMX:n käyttö ASP.NET Coren avulla on hyvä tapa rakentaa dynaamisia verkkosovelluksia minimaalisella JavaScriptilla. HTMX:n avulla voit päivittää sivusi osia ilman koko sivun uudelleenlatausta, jolloin sovelluksesi tuntuu reagoivammalta ja vuorovaikutteisemmalta.

Sitä kutsuin "hybridiverkkosuunnitteluksi", jossa sivu muutetaan täysin palvelimen sivukoodin avulla ja sitten HTMX:n avulla sivun osia päivitetään dynaamisesti.

Tässä artikkelissa näytän, miten voit aloittaa HTMX:n ASP.NET Core -sovelluksessa.

[TÄYTÄNTÖÖNPANO

## Edeltävät opinnot

HTMX - Htmx on JavaScript-paketti, jolla se voidaan helposti sisällyttää projektiisi, on CDN:n käyttö. (Ks. [täällä](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

Voit tietysti myös ladata kopion ja lisätä sen "manuaalisesti" (tai käyttää LibMan tai npm).

## ASP.NET-bileet

Suosittelen myös Htmx Tag Helper -työkalun asentamista [täällä](https://github.com/khalidabuhakmeh/Htmx.Net)

Nämä molemmat ovat ihmeellisiä. [Khalid Abuhakmeh
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Ja Htmx Nuget -paketti [täällä](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

Tagiauttaja antaa sinun tehdä tämän:

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Vaihtoehtoinen lähestymistapa.

**HUOMAUTUS: Tällä lähestymistavalla on yksi suuri haittapuoli: se ei tuota postilinkille hrefiä. Tämä on ongelma SEO:lle ja saavutettavuudelle. Se tarkoittaa myös, että nämä linkit epäonnistuvat, jos HTMX jostain syystä ei lataudu (CDNs DO menee alas).**

Vaihtoehtoinen lähestymistapa on käyttää ` hx-boost="true"` attribuutti ja normaalit asp.net-ytimen tunnisteavustajat. Katso  [täällä](https://htmx.org/docs/#hx-boost) Lisätietoja hx-boostista (vaikka dokumentteja on vähän).
Tämä tuottaa normaalin hrefin, mutta HTMX ja sisältö ladataan dynaamisesti.

Näin ollen:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### Osittain

HTMX toimii hyvin osittaisilla näkymillä. Voit ladata osittaisen näkymän sivullasi olevaan konttiin HTMX:n avulla. Tämä on hienoa, kun lataat osia sivustasi dynaamisesti ilman koko sivun uudelleenlatausta.

Tässä sovelluksessa meillä on Layout.cshtml-tiedostossa kontti, johon haluamme ladata osittaisen näkymän.

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

Normaalisti se tekee palvelimen sivusisällön, mutta HTMX-tunnisteen avulla näet, että kohdistamme `hx-target="#contentcontainer"` joka lataa osittaisen näkymän konttiin.

Projektissamme meillä on BlogView-osittainen näkymä, jonka haluamme ladata konttiin.

![img.png](project.png)

Sitten Blog Controllerissa meillä on

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

Katso täältä, että meillä on HTMX-pyyntö.IsHtmx() -menetelmä, tämä palautuu todeksi, jos pyyntö on HTMX-pyyntö. Jos se on, palautamme osittaisen näkemyksen, ellemme palauta täydellistä näkemystä.

Tämän avulla voimme varmistaa, että tuemme myös suoraa kyselyä vähäisin todellisin ponnistuksin.

Tässä tapauksessa koko näkemyksemme viittaa tähän osittaiseen:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

Ja niin meillä on nyt superyksinkertainen tapa ladata osittainen näkymä sivullemme HTMX:n avulla.