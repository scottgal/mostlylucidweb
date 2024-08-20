# ASP.NET-ydinsivuston tekeminen PWA:ksi

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

Tässä artikkelissa näytän, kuinka voit tehdä ASP.NET Core -sivustostasi PWA:n (Progressive Web App).

## Edeltävät opinnot

Se on todella aika yksinkertainen katso https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET-bileet

Asenna Nuget-paketti

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

Ohjelmassasi.cs lisätään:

```csharp
builder.Services.AddProgressiveWebApp();
```

Luo sitten favikoneja, jotka vastaavat alla olevia kokoja [täällä](https://realfavicongenerator.net/) on työkalu, jota voit käyttää niiden luomiseen. Nämä voivat todella olla mikä tahansa kuvake (käytin emojia)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Sitten tarvitset manifestin.

```json
{
  "name": "mostlylucid",
  "short_name": "mostlylucid",
  "description": "The web site for mostlylucid limited",
  "icons": [
    {
      "src": "/android-chrome-192x192.png",
      "sizes": "192x192"
    },
    {
      "src": "/android-chrome-512x512.png",
      "sizes": "512x512"
    }
  ],
  "display": "standalone",
  "start_url": "/"
}
```