# Lisää haku HTMX:llä ja ASP.NET Corella TagHelperillä

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Johdanto

Nyt kun minulla on joukko blogikirjoituksia, kotisivu oli melko pitkä, joten päätin lisätä hakumekanismin blogikirjoituksiin.

Tämä liittyy siihen, että blogikirjoituksiin lisätään täysi välimuisti, jotta tästä tulee nopea ja tehokas sivusto.

Katso [Blogipalvelun lähde](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) Miten tämä toteutetaan, se on aika yksinkertaista IMemoryCachea käyttäen.

[TÄYTÄNTÖÖNPANO

### TagHelper

Päätin käyttää TagHelper-palvelua hakujärjestelmän käyttöön. Tämä on loistava tapa kiteyttää hakulogiikka ja tehdä siitä käyttökelpoinen.
Tässä käytetään [Darrel O'Neillin paginaatiotagittaja ](https://github.com/darrel-oneil/PaginationTagHelper) tämä on mukana hankkeessa nugettipakettina.

Tämä lisätään tämän jälkeen _ViewImports.cshtml-tiedosto, joten se on kaikkien näkymien käytettävissä.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Tag Helper

• • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • • _Blog YhteenvetoList.cshtml osittainen näkemys Lisäsin seuraavan koodin tehdä hakumekanismi.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Muutama merkittävä asia tässä:

1. `link-url` Näin taghelper voi luoda oikean urlin hakulinkkeihin. HomeController Index -menetelmässä tämä on asetettu kyseiseen toimeen.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Ja blogin ohjaimessa

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Tämä on asetettu URL:n tasolle. Tämä varmistaa, että paginointiavustin voi toimia kummassa tahansa huipputason menetelmässä.

### HTMX- ominaisuudet

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` nämä ovat kaikki HTMX-ominaisuuksia, joiden avulla haku toimii HTMX:n kanssa.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Tässä sitä käytetään `hx-boost="true"` Tämä mahdollistaa sen, että pagination taghelper ei tarvitse muutoksia sieppaamalla sen normaalin URL-sukupolven ja käyttämällä nykyistä URL-osoitetta.

`hx-push-url="true"` Varmistaaksesi, että URL vaihdetaan selaimen URL-historiaan (joka mahdollistaa suoran linkityksen sivuille).

`hx-target="#content"` tämä on tavoitediv, joka korvataan uudella sisällöllä.

`hx-swap="show:none"` tämä on se swap-efekti, jota käytetään, kun sisältö korvataan. Tässä tapauksessa se estää normaalin hyppyvaikutuksen, jota HTMX käyttää sisällön vaihtamiseen.

#### CSS

Lisäsin myös tyylejä main.css-hakemistooni, jonka avulla voin käyttää Tailwind CSS -tunteja paginaatiolinkkeihin.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Hallintalaite

`page`, `page-size`, `total-items` ovat ominaisuuksia, joita pagination taghelper käyttää hakulinkkien luomiseen.
Nämä välittyvät ohjaimen osittaiseen näkymään.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Blogipalvelu

Tässä sivu ja sivuKoko siirtyy URL-osoitteesta ja kaikki kohteet lasketaan blogipalvelusta.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Täällä yksinkertaisesti saamme viestit välimuistista, tilaamme ne päivämääriin mennessä ja sitten ohitamme ja otamme oikean määrän viestejä sivulle.

### Päätelmät

Tämä oli yksinkertainen lisäys sivustoon, mutta se tekee siitä paljon käyttökelpoisemman. HTMX-integraatio saa sivuston tuntemaan olonsa reagoivammaksi, kun lisää JavaScriptia ei lisätä sivustoon.