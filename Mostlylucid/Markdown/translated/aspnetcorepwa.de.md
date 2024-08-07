# Ihre ASP.NET Core Website zu einem PWA machen

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

In diesem Artikel zeige ich Ihnen, wie Sie Ihre ASP.NET Core-Website zu einer PWA (Progressive Web App) machen.

## Voraussetzungen

Es ist wirklich ziemlich einfach siehe https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET-Bits

Installieren Sie das Nuget-Paket

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

In Ihrem Programm.cs hinzufügen:

```csharp
builder.Services.AddProgressiveWebApp();
```

Dann erstellen Sie einige Favicons, die den Größen unten entsprechen[Hierher](https://realfavicongenerator.net/)ist ein Werkzeug, das Sie verwenden können, um sie zu erstellen. Dies kann wirklich jedes Symbol sein (Ich habe ein emoji ∧)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Dann brauchen Sie eine manifest.json

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