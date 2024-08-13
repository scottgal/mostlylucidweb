# ASP.NET Core Caching mit HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Einleitung

Caching ist eine wichtige Technik, um sowohl die Benutzererfahrung zu verbessern, indem Sie Inhalte schneller laden, als auch die Belastung auf Ihrem Server zu reduzieren. In diesem Artikel werde ich Ihnen zeigen, wie Sie die integrierten Caching-Funktionen von ASP.NET Core mit HTMX verwenden, um Inhalte auf der Client-Seite zu verbergen.

[TOC]

## Einrichtung

In ASP.NET Core, gibt es zwei Arten von Caching angeboten

- Reponse Cache - Dies sind Daten, die auf dem Client oder in zwischengeschalteten procy Servern (oder beide) zwischengespeichert werden und verwendet werden, um die gesamte Antwort für eine Anfrage zu verbergen.
- Ausgabe-Cache - Dies sind Daten, die auf dem Server zwischengespeichert werden und verwendet werden, um die Ausgabe einer Controller-Aktion zu verbergen.

Um diese in ASP.NET Core einzurichten, müssen Sie ein paar Dienste in Ihrem `Program.cs` Datei

### Ansprech-Caching

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Ausgabe-Caching

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Ansprech-Caching

Während es möglich ist, das Response Caching in Ihrem `Program.cs` es ist oft ein wenig unflexibel (besonders bei der Verwendung von HTMX-Anfragen, wie ich entdeckt habe). Sie können Response-Caching in Ihren Controller-Aktionen einrichten, indem Sie die `ResponseCache` Attribut.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Dies wird die Antwort für 300 Sekunden zwischenspeichern und variieren den Cache durch die `hx-request` header und die `page` und `pageSize` Abfrageparameter. Wir stellen auch die `Location` zu `Any` was bedeutet, dass die Antwort auf dem Client, auf zwischengeschalteten Proxyservern oder beidem zwischengespeichert werden kann.

Hier die `hx-request` header ist der Header, den HTMX mit jeder Anfrage sendet. Dies ist wichtig, da es Ihnen erlaubt, die Antwort unterschiedlich zu verbergen, je nachdem, ob es sich um eine HTMX-Anfrage oder eine normale Anfrage handelt.

Das ist unsere aktuelle `Index` Aktionsmethode. Yo ucan sehen, dass wir eine Seite und pageSize Parameter hier akzeptieren und wir diese als variierend von Abfragetasten in der `ResponseCache` Attribut. Das bedeutet, dass Antworten von diesen Schlüsseln 'indexiert' werden und unterschiedliche Inhalte auf dieser Grundlage speichern.

Wir haben im Rahmen der Aktion auch `if(Request.IsHtmx())` Dies basiert auf der [HTMX.Net-Paket](https://github.com/khalidabuhakmeh/Htmx.Net)  und im Wesentlichen Kontrollen für die gleichen `hx-request` Header, die wir verwenden, um den Cache zu variieren. Hier geben wir eine Teilansicht zurück, wenn die Anfrage von HTMX stammt.

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

## Ausgabe-Caching

Ausgabe Caching ist das serverseitige Äquivalent von Response Caching. Es speichert die Ausgabe einer Controller-Aktion. Im Wesentlichen speichert der Webserver das Ergebnis einer Anfrage und bedient sie für spätere Anfragen.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Hier cachieren wir die Ausgabe der Controller Aktion für 3600 Sekunden und variieren den Cache durch die `hx-request` header und die `page` und `pageSize` Abfrageparameter.
Da wir Datenserverseite für eine signifikante Zeit speichern (die Beiträge aktualisieren nur mit einem docker Push) ist dies auf länger als der Response Cache eingestellt; es könnte tatsächlich unendlich in unserem Fall sein, aber 3600 Sekunden ist ein guter Kompromiss.

Wie beim Response Cache benutzen wir die `hx-request` header, um den Cache zu variieren, je nachdem, ob die Anforderung von HTMX stammt oder nicht.

## Statische Dateien

ASP.NET Core hat auch eingebaute Unterstützung für das Caching statischer Dateien. Dies geschieht durch die Einstellung der `Cache-Control` header in der Antwort. Sie können dies in Ihrem `Program.cs` ..............................................................................................................................
Beachten Sie, dass die Bestellung ist wichtig hier, wenn Ihre statischen Dateien benötigen Autorisierung Unterstützung sollten Sie verschieben die `UseAuthorization` middleware vor dem `UseStaticFiles` Mittelwaren. THe UseHttpsRedirection Middleware sollte auch vor der UseStaticFiles Middleware sein, wenn Sie sich auf diese Funktion verlassen.

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

## Schlußfolgerung

Caching ist ein leistungsfähiges Werkzeug, um die Leistung Ihrer Anwendung zu verbessern. Mit den eingebauten Caching-Funktionen von ASP.NET Core können Sie Inhalte auf Client- oder Serverseite einfach zwischenspeichern. Durch die Verwendung von HTMX können Sie Inhalte auf der Client-Seite zwischenspeichern und Teilansichten bereitstellen, um die Benutzererfahrung zu verbessern.