# Bloggaamiseen (osa 3) lisätyn kokonaisuuden viitekehys

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Löydät kaikki lähdekoodit blogikirjoituksista [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Osat 1 ja 2 sarjasta, joka koskee Entity Frameworkin lisäämistä.NET Core -hankkeeseen.**

Osa 1 löytyy [täällä](/blog/addingentityframeworkforblogpostspt1).

Osa 2 löytyy [täällä](/blog/addingentityframeworkforblogpostspt2).

## Johdanto

Edellisissä osissa perustimme tietokannan ja taustan blogikirjoituksillemme ja lisäsimme palvelut vuorovaikutukseen tietokannan kanssa. Tässä viestissä kerromme yksityiskohtaisesti, miten nämä palvelut toimivat nyt olemassa olevien ohjaajien ja näkökantojen kanssa.

[TÄYTÄNTÖÖNPANO

## Hallintalaitteet

Out controllers for Blogs on todella melko yksinkertainen, kuten välttää "Fat Controller" antipattern (kuvio ideinfied aikaisin ASP.NET MVC päivinä).

### Fat Controller -kuvio ASP.NET MVC:ssä

I MVC-kehysten hyvä käytäntö on tehdä mahdollisimman vähän ohjainmenetelmissä. Tämä johtuu siitä, että rekisterinpitäjä on vastuussa pyynnön käsittelystä ja vastauksen palauttamisesta. Sen ei pitäisi olla vastuussa sovelluksen liiketoimintalogiikasta. Tämä on mallin vastuulla.

"Läskiohjaimen" antipattern on paikka, jossa ohjain tekee liikaa. Tämä voi johtaa useisiin ongelmiin, kuten:

1. Koodin kopiointi useassa toimessa:
   Toiminnan tulisi olla yksi ainoa työyksikkö, jossa vain asutetaan mallia ja palautetaan näkymä. Jos huomaat toistavasi koodia useissa toiminnoissa, se on merkki siitä, että sinun pitäisi muuttaa koodi erilliseksi menetelmäksi.
2. Vaikeasti testattava koodi:
   Kun sinulla on "rasvaohjaimia", koodin testaaminen voi olla vaikeaa. Testien tulisi pyrkiä seuraamaan kaikkia mahdollisia polkuja koodin läpi, ja tämä voi olla vaikeaa, jos koodi ei ole hyvin jäsennetty ja keskittyy yhteen ainoaan vastuuseen.
3. Koodi, jota on vaikea ylläpitää:
   Ylläpidettävyys on keskeinen huolenaihe rakennussovelluksissa. Keittiön pesualtaan toimintatavat voivat helposti johtaa sinuun sekä muihin kehittäjiin, jotka käyttävät koodia tehdäkseen muutoksia, jotka rikkovat sovelluksen muita osia.
4. Koodia, jota on vaikea ymmärtää:
   Tämä on keskeinen huolenaihe kehittäjille. Jos työstät projektia, jossa on suuri koodipohja, voi olla vaikea ymmärtää, mitä ohjaintoiminnossa tapahtuu, jos se tekee liikaa.

### Bloginvalvoja

Blogiohjain on suhteellisen yksinkertainen. Siinä on neljä päätointa (ja yksi "komppania" vanhoille blogilinkeille). Nämä ovat seuraavat:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

Nämä toimet puolestaan kutsuvat `IBlogService` Saada tarvitsemansa tiedot. Erytropoietiini `IBlogService` on yksityiskohtaisesti [edellinen virka](/blog/addingentityframeworkforblogpostspt2).

Nämä toimet puolestaan ovat seuraavat:

- Hakemisto: Tämä on lista blogikirjoituksista (oletukset englannin kieleen; voimme laajentaa tätä myöhemmin, jotta voimme sallia useita kieliä). Huomaat, että se vaatii `page` sekä `pageSize` muuttujina. Tämä on paginaatiosta. Tulosten perusteella.
- Näytä: Tämä on yksittäinen blogikirjoitus. Se vaatii `slug` Euroopan parlamentin ja neuvoston asetus (EU) N:o 1316/2013, annettu 11 päivänä joulukuuta 2013, Euroopan aluekehitysrahastosta (EUVL L 347, 20.12.2013, s. 6). `language` muuttujina. THis on menetelmä, jolla luet tätä blogikirjoitusta.
- Kategoria: Tämä on lista tietyn kategorian blogikirjoituksista. Se vaatii `category`, `page` sekä `pageSize` muuttujina.
- Kieli: Tämä osoittaa tietyn kielen blogikirjoituksen. Se vaatii `slug` sekä `language` muuttujina.
- Compat: Tämä on kompatiilista toimintaa vanhoille blogilinkeille. Se vaatii `slug` sekä `language` muuttujina.

### Välilyöntejä

Kuten on mainittu [aiempaa virkaa](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) toteutamme `OutputCache` sekä `ResponseCahce` säilöä blogikirjoitusten tulokset. Tämä parantaa käyttökokemusta ja vähentää palvelimen kuormitusta.

Ne toteutetaan käyttäen asianmukaisia Action Decorators, jotka määrittelevät parametrit, joita toimintoon käytetään (sekä `hx-request` HTMX-pyynnöille). Tutkimukseen, jossa `Index` täsmennämme näitä:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Näkymät

Näkemykset blogiin ovat suhteellisen yksinkertaiset. Ne ovat enimmäkseen vain lista blogikirjoituksista, joissa on muutama yksityiskohta kutakin viestiä varten. Näkemykset ovat `Views/Blog` Kansio. Tärkeimmät näkemykset ovat seuraavat:

### `_PostPartial.cshtml`

Tämä on osittainen näkemys yhdestä blogikirjoituksesta. Sitä käytetään meidän `Post.cshtml` näkymä.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

Tämä on osittainen näkemys blogikirjoitusten listalle. Sitä käytetään meidän `Index.cshtml` näkymä sekä kotisivulla.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

Tässä käytetään `_ListPost` Osittainen näkymä yksittäisten blogikirjoitusten esittelyyn [hakutarra-auttaja](/blog/addpagingwithhtmx) jonka avulla voimme sivuta blogikirjoituksia.

### `_ListPost.cshtml`

Erytropoietiini _Listapostin osittaista katselua käytetään listan yksittäisten blogikirjoitusten näyttämiseen. Sitä käytetään `_BlogSummaryList` näkymä.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

Koska olet täällä, meillä on linkki yksittäiseen blogikirjoitukseen, postin kategorioihin, kieliin, joilla viesti on saatavilla, postin tiivistelmään, julkaistuun päivämäärään ja lukuaikaan.

Meillä on myös HTMX-linkkitunnisteet kategorioihin ja kieliin, jotta voimme näyttää lokalisoituja virkoja ja tietyn kategorian virkoja.

Meillä on tässä kaksi tapaa käyttää HTMX:ää, joista toinen antaa koko URL-osoitteen ja toinen on "vain HTML" (ts. ei URL-osoitetta). Tämä johtuu siitä, että haluamme käyttää koko URL-osoitetta kategorioihin ja kieliin, mutta emme tarvitse koko URL-osoitetta yksittäiseen blogikirjoitukseen.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Tämä lähestymistapa kokoaa täydellisen URL-osoitteen yksittäiseen blogikirjoitukseen ja käyttää `hx-boost` HTMX:n käyttöpyynnön "vahvistamiseksi" (tämä asettaa `hx-request` Otsikko `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Vaihtoehtoisesti tämä lähestymistapa käyttää HTMX-tunnisteita saadakseen kategoriat blogikirjoituksiin. Tässä käytetään `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` sekä `hx-route-category` tagit, joilla voit saada kategoriat blogikirjoituksiin, kun taas `hx-push-url` on asetettu `true` URL-osoitteen siirtämiseksi selaimen historiaan.

Sitä käytetään myös meidän `Index` Toimintatapa HTMX-pyyntöihin.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Jos se antaa meille mahdollisuuden joko palauttaa koko näkymän tai vain osittaisen näkymän HTMX-pyyntöihin ja antaa "SPA:n" kaltaisen kokemuksen.

## Etusivu

• • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • `HomeController` viittaamme myös näihin blogipalveluihin, jotta saamme tuoreimmat blogikirjoitukset etusivulle. Tämä tapahtuu `Index` toimintatapa.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
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

Kuten näette täällä, käytämme... `IBlogService` saat uusimmat blogikirjoitukset kotisivulle. Käytämme myös `GetUserInfo` tapa, jolla käyttäjätiedot saadaan kotisivulle.

Tässäkin on HTMX-pyyntö palauttaa osittainen näkymä blogikirjoituksiin, jotta voimme sivuta blogikirjoituksia kotisivulla.

## Johtopäätöksenä

Seuraavassa osassa mennään tuskalliseen yksityiskohtaan siitä, miten käytämme `IMarkdownBlogService` Kantaa tietokantaa markown-tiedostojen blogikirjoituksilla. Tämä on keskeinen osa sovellusta, koska sen avulla voimme käyttää markdown-tiedostoja tietokannan kansoittamiseen blogikirjoituksilla.