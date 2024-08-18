# Gör din ASP.NET Core-webbplats till en PWA

<!--category-- ASP.NET -->
<datetime class="hidden">Försäkrings- och återförsäkringsföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, försäkringsholdingföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag och värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag, värdepappersföretag,</datetime>

I den här artikeln, Jag ska visa dig hur du gör din ASP.NET Core webbplats en PWA (Progressive Web App).

## Förutsättningar

Det är verkligen ganska enkelt se https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET-bitar

Installera Nuget- paketet

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

I ditt program.cs lägg till:

```csharp
builder.Services.AddProgressiveWebApp();
```

Skapa sedan några favicons som matchar storlekarna nedan [här](https://realfavicongenerator.net/) är ett verktyg som du kan använda för att skapa dem. Dessa kan verkligen vara vilken ikon som helst (Jag använde en emoji)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Då behöver du en manifest.Json

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