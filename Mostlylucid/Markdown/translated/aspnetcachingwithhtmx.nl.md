# ASP.NET Core Caching met HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Inleiding

Caching is een belangrijke techniek om zowel de gebruikerservaring te verbeteren door de inhoud sneller te laden als de belasting op uw server te verminderen. In dit artikel laat ik u zien hoe u de ingebouwde cachingfuncties van ASP.NET Core met HTMX kunt gebruiken om de inhoud van de client te cachen.

[TOC]

## Instellen

In ASP.NET Core, zijn er twee soorten Caching aangeboden

- Reageren Cache - Dit zijn gegevens die worden gecached op de client of in intermediaire procy servers (of beide) en wordt gebruikt om het volledige antwoord voor een verzoek cache.
- Output Cache - Dit zijn gegevens die gecached worden op de server en gebruikt worden om de output van een controller actie te cachen.

Om deze op te zetten in ASP.NET Core moet u een paar diensten toevoegen in uw`Program.cs`bestand

### Response Caching

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Output Caching

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Response Caching

Terwijl het mogelijk is om het opzetten van Response Caching in uw`Program.cs`Het is vaak een beetje onflexibel (vooral bij het gebruik van HTMX verzoeken zoals ik ontdekte). U kunt het instellen van Response Caching in uw controller acties door het gebruik van de`ResponseCache`attribuut.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Dit zal het antwoord cache gedurende 300 seconden en variëren de cache door de`hx-request`kop en de`page`en`pageSize`query parameters. We zijn ook het instellen van de`Location`tot`Any`wat betekent dat het antwoord kan worden gecached op de client, op intermediaire proxy servers, of beide.

Hier de`hx-request`header is de header die HTMX bij elk verzoek verstuurt. Dit is belangrijk omdat het u toelaat om het antwoord anders te cachen op basis van of het een HTMX-verzoek is of een normaal verzoek.

Dit is onze huidige`Index`action method. Yo ucan zien dat we accepteren een pagina en paginaMaat parameter hier en we voegden deze als variëren per query keys in de`ResponseCache`attribuut. Betekenis dat antwoorden 'geindexeerd' worden door deze toetsen en verschillende inhoud op te slaan op basis van deze.

In het kader van de actie hebben wij ook`if(Request.IsHtmx())`Dit is gebaseerd op de[HTMX.Net-pakket](https://github.com/khalidabuhakmeh/Htmx.Net)en in wezen controles voor dezelfde`hx-request`header die we gebruiken om de cache te variëren. Hier geven we een gedeeltelijke weergave terug als het verzoek van HTMX is.

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

## Output Caching

Output Caching is de serverzijde equivalent van Response Caching. Het caches de uitvoer van een controller actie. In wezen slaat de webserver het resultaat van een verzoek op en dient het voor latere verzoeken.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Hier cachen we de output van de controller actie gedurende 3600 seconden en variëren de cache door de`hx-request`kop en de`page`en`pageSize`query parameters.
Aangezien we de dataserver kant voor een significante tijd (de berichten alleen updaten met een docker push) dit is ingesteld op langer dan de Response Cache; het kan eigenlijk oneindig zijn in ons geval, maar 3600 seconden is een goed compromis.

Zoals met de Response Cache gebruiken we de`hx-request`header om de cache te variëren op basis van of het verzoek van HTMX is of niet.

## Statische bestanden

ASP.NET Core heeft ook ingebouwde ondersteuning voor het cachen van statische bestanden. Dit wordt gedaan door het instellen van de`Cache-Control`kop in het antwoord. U kunt dit instellen in uw`Program.cs`bestand.
Merk op dat de volgorde is belangrijk hier, als uw statische bestanden nodig autorisatie ondersteuning moet u de`UseAuthorization`middleware voor de`UseStaticFiles`middleware. THe UseHttpsRedirection middleware moet ook voor de UseStaticFiles middleware als u op deze functie vertrouwt.

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

## Conclusie

Caching is een krachtig hulpmiddel om de prestaties van uw toepassing te verbeteren. Door gebruik te maken van de ingebouwde caching functies van ASP.NET Core kunt u gemakkelijk inhoud cache op de client of server kant. Door gebruik te maken van HTMX kunt u inhoud cache aan de client kant en dienen gedeeltelijk weergaven om de gebruikerservaring te verbeteren.