# ASP.NET Core Caching med HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Inledning

Caching är en viktig teknik för att både förbättra användarupplevelsen genom att ladda innehållet snabbare och minska belastningen på din server. I den här artikeln ska jag visa dig hur du använder de inbyggda caching funktioner ASP.NET Core med HTMX för att cache innehåll på klientens sida.

[TOC]

## Ställ in

I ASP.NET Core finns det två typer av Caching som erbjuds

- Response Cache - Detta är data som är cachad på klienten eller i mellanliggande procy servrar (eller båda) och används för att cache hela svaret för en begäran.
- Utmatningscache - Detta är data som är cached på servern och används för att cache utdata från en controller åtgärd.

För att ställa in dessa i ASP.NET Core måste du lägga till ett par tjänster i din `Program.cs` akt

### Caching för svar

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Utmatningscache

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Caching för svar

Även om det är möjligt att ställa in Response Caching i din `Program.cs` Det är ofta lite oflexibelt (särskilt när du använder HTMX-förfrågningar som jag upptäckte). Du kan ställa in Response Caching i din controller åtgärder genom att använda `ResponseCache` Egenskap.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Detta kommer att cache svar i 300 sekunder och variera cache med `hx-request` Rubrik och `page` och `pageSize` Förfrågans parametrar. Vi ställer också in `Location` till `Any` vilket innebär att svaret kan lagras på klienten, på mellanhandsproxyservrar, eller båda.

Här... `hx-request` Rubriken är det huvud som HTMX skickar med varje begäran. Detta är viktigt eftersom det låter dig cache svaret annorlunda baserat på om det är en HTMX begäran eller en normal begäran.

Det här är vår nuvarande `Index` Åtgärdsmetod. Yo ucan se att vi accepterar en sida och sidaStorlek parameter här och vi lagt till dessa som varierar av frågetangenterna i `ResponseCache` Egenskap. Det betyder att svaren är "indexerade" av dessa nycklar och lagrar olika innehåll baserat på dessa.

I handling har vi också `if(Request.IsHtmx())` Detta är baserat på [HTMX.Net-paket](https://github.com/khalidabuhakmeh/Htmx.Net)  och i huvudsak kontroller för samma `hx-request` Rubrik som vi använder för att variera cache. Här returnerar vi en partiell vy om begäran kommer från HTMX.

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

## Utmatningscache

Utmatningscache är serversidans motsvarighet till Response Caching. Den döljer resultatet av en controller-åtgärd. I huvudsak lagrar webbservern resultatet av en förfrågan och serverar den för efterföljande förfrågningar.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Här vi caching utgången av styrenheten åtgärder för 3600 sekunder och variera cachen av `hx-request` Rubrik och `page` och `pageSize` Förfrågans parametrar.
Eftersom vi lagrar dataserver sidan för en betydande tid (posterna bara uppdatera med en docker push) är detta satt till längre än Response Cache; det kan faktiskt vara oändligt i vårt fall men 3600 sekunder är en bra kompromiss.

Som med Response Cache vi använder `hx-request` Rubrik för att variera cache baserat på om begäran är från HTMX eller inte.

## Statiska filer

ASP.NET Core har också inbyggt stöd för cachelagring av statiska filer. Detta görs genom att ställa in `Cache-Control` Rubrik i svaret. Du kan ställa upp detta i din `Program.cs` En akt.
Observera att ordern är viktig här, om dina statiska filer behöver behörighetsstöd bör du flytta `UseAuthorization` middleware före `UseStaticFiles` Mellanbestick. THE UseHttpsRedirection middleware bör också vara före UseStaticFiles middleware om du förlitar dig på denna funktion.

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

## Slutsatser

Caching är ett kraftfullt verktyg för att förbättra prestandan i din applikation. Genom att använda de inbyggda cachefunktionerna i ASP.NET Core kan du enkelt cacheinnehåll på klientens eller serverns sida. Genom att använda HTMX kan du cacheinnehåll på klientsidan och tjäna upp partiella vyer för att förbättra användarupplevelsen.