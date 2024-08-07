# Het maken van uw ASP.NET Core Website een PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

In dit artikel laat ik je zien hoe je je ASP.NET Core website een PWA (Progressive Web App) kunt maken.

## Vereisten

Het is echt vrij eenvoudig zie https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET Bits

Installeer het Nuget-pakket

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

In uw programma.cs toevoegen:

```csharp
builder.Services.AddProgressiveWebApp();
```

Creëer vervolgens wat faviconen die overeenkomen met de onderstaande maten[Hier.](https://realfavicongenerator.net/)is een tool die u kunt gebruiken om ze te maken. Deze kunnen echt elk pictogram (Ik gebruikte een emoji 

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Dan heb je een manifest.json nodig

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