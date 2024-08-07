# Hacer de su sitio web ASP.NET un PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

En este artículo, te mostraré cómo hacer de tu sitio web ASP.NET Core una PWA (App Web Progressive).

## Requisitos previos

Es realmente bastante simple ver https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## Bits ASP.NET

Instalar el paquete Nuget

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

En su programa.cs añadir:

```csharp
builder.Services.AddProgressiveWebApp();
```

A continuación, crear algunos favicons que coincidan con los tamaños de abajo[aquí](https://realfavicongenerator.net/)es una herramienta que se puede utilizar para crearlos. Estos pueden realmente ser cualquier icono (Usé un emoji )

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Entonces necesitas un manifiesto.json

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