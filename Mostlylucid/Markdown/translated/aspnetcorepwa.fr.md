# Faire de votre site ASP.NET un PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

Dans cet article, je vais vous montrer comment faire de votre site ASP.NET Core un PWA (Progressive Web App).

## Préalables

C'est vraiment assez simple voir https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## Bits ASP.NET

Installez le paquet Nuget

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

Dans votre programme.cs ajouter:

```csharp
builder.Services.AddProgressiveWebApp();
```

Puis créez quelques favicons qui correspondent aux tailles ci-dessous[Ici.](https://realfavicongenerator.net/)est un outil que vous pouvez utiliser pour les créer. Ceux-ci peuvent vraiment être n'importe quelle icône (j'ai utilisé un emoji)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Alors vous avez besoin d'un manifeste.json

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