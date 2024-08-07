# Rendere il vostro ASP.NET Core Website un PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

In questo articolo vi mostrerò come rendere il vostro sito web ASP.NET Core una PWA (Progressive Web App).

## Prerequisiti

È molto semplice vedere https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET Bits

Installa il pacchetto Nuget

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

Nel vostro programma.cs aggiungere:

```csharp
builder.Services.AddProgressiveWebApp();
```

Poi creare alcuni favicon che corrispondono alle dimensioni qui sotto[qui](https://realfavicongenerator.net/)è uno strumento che puoi usare per crearli. Questi possono essere davvero qualsiasi icona (ho usato un emoji)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Allora hai bisogno di un manifesto.Json

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