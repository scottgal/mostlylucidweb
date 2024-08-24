# ASP.NET Core Caching HTMX:llä

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Johdanto

Välimuisti on tärkeä tekniikka, jolla voidaan sekä parantaa käyttökokemusta lastaamalla sisältöä nopeammin että vähentää palvelimen kuormitusta. Tässä artikkelissa näytän, miten ASP.NET Coren sisäänrakennettuja välimuistiominaisuuksia käytetään HTMX:n avulla sisällön kätkössä asiakaspuolella.

[TÄYTÄNTÖÖNPANO

## Asetukset

ASP.NET Coressa on tarjolla kahdenlaisia välilyöntejä

- Reponse Cache - Tämä on tietoa, joka välitetään asiakkaalle tai välittäjän procy-palvelimille (tai molemmille) ja jota käytetään koko vastauksen tallentamiseen pyyntöä varten.
- Output Cache - Tämä on dataa, joka on välimuistissa palvelimella ja jota käytetään välimuistina ohjaintoiminnon ulostulolle.

Voit perustaa nämä ASP.NET Core sinun täytyy lisätä pari palvelua `Program.cs` tiedosto

### Vaste välimuistiin

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Lähtö välimuistiin

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Vaste välimuistiin

Vaikka on mahdollista asettaa Response Caching `Program.cs` Se on usein hieman joustamatonta (etenkin silloin, kun käytän HTMX-pyyntöjä, kuten huomasin). Voit asettaa Response Cachingin ohjaintoiminnoissasi käyttämällä `ResponseCache` Ominaisuus.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Tämä tallentaa vastauksen 300 sekunniksi ja muuttaa välimuistia `hx-request` Otsikko ja objekti `page` sekä `pageSize` query parametreja. Asetamme myös `Location` Kohtiin `Any` Tämä tarkoittaa, että vastaus voidaan siirtää asiakkaalle, välityspalvelimelle tai molempiin.

Tässä. `hx-request` Header on se otsikko, jonka HTMX lähettää jokaisella pyynnöllä. Tämä on tärkeää, koska sen avulla vastaus voidaan tallentaa eri tavalla riippuen siitä, onko kyseessä HTMX-pyyntö vai normaali pyyntö.

Tämä on meidän nykyhetkemme `Index` toimintatapa. Yu voi nähdä, että hyväksymme sivun ja sivunSize-parametrin tässä ja lisäsimme nämä vaihtelevasti kyselynäppäiminä `ResponseCache` Ominaisuus. Merkitys siihen, että vastaukset "indeksoidaan" näillä avaimilla ja tallennetaan eri sisältöjä niiden perusteella.

Ulkopuolella Toimintaa meillä on myös `if(Request.IsHtmx())` Tämä perustuu [HTMX.Net-paketti](https://github.com/khalidabuhakmeh/Htmx.Net)  ja pääasiassa tarkastaa saman `hx-request` Otsikko, jolla vaihdamme välimuistia. Tässä palautamme osittaisen näkymän, jos pyyntö on HTMX:ltä.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## Lähtö välimuistiin

Reput Caching on Response Cachingin palvelinpuoli. Se tallentaa ohjaintoiminnon ulostulon. Pohjimmiltaan web-palvelin tallentaa pyynnön tuloksen ja palvelee sitä seuraavissa pyynnöissä.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Tässä säilömme ohjaimen toiminnan ulostuloa 3600 sekuntia ja vaihtelemme välimuistia `hx-request` Otsikko ja objekti `page` sekä `pageSize` query parametreja.
Koska tallennamme datapalvelinpuolta merkittäväksi ajaksi (virat vain päivittyvät dockerin työnnöllä), tämä on asetettu pidemmäksi aikaa kuin Response Cache; se voi itse asiassa olla ääretön meidän tapauksessamme, mutta 3600 sekuntia on hyvä kompromissi.

Kuten Reaction Cachessa, käytämme `hx-request` Otsikko vaihtaa välimuistia sen mukaan, onko pyyntö HTMX:ltä vai ei.

## Staattinen tiedosto

ASP.NET Corella on myös sisäänrakennettu tuki staattisten tiedostojen välilyönteihin. Tämä tapahtuu asettamalla `Cache-Control` Header in the reaction. Voit laittaa tämän omaan käsiisi. `Program.cs` Kansio.
Huomaa, että tilaus ii on tärkeä täällä, jos staattiset tiedostot tarvitsevat valtuutuksen tukea, sinun pitäisi siirtää `UseAuthorization` Middleware ennen `UseStaticFiles` Middleware. THe UseHttpsRedirection -väliohjelmiston pitäisi olla myös ennen UseStaticFiles -väliohjelmistoa, jos olet tämän ominaisuuden varassa.

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## Päätelmät

Välimuisti on tehokas työkalu sovelluksesi suorituskyvyn parantamiseksi. Käyttämällä ASP.NET Coren sisäänrakennettuja välimuistiominaisuuksia voit helposti piilottaa sisältöä asiakkaan tai palvelimen puolelle. Käyttämällä HTMX:ää voit tallentaa sisältöä asiakaspuolelle ja tarjota osittaisia näkymiä käyttökokemuksen parantamiseksi.