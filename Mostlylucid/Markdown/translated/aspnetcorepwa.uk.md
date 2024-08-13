# Створення вашого ядра ASP. NET веб-сайту PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 08- 01T11: 36</datetime>

У цій статті я покажу вам, як створити ваш веб-сайт ASP.NET PWA (Progressive Web App).

## Передумови

Це дуже просто побачити https: //github.com/ madskistensen/WebEsentials.AspNetCore.ServiceWorker/tree/ master

## ASP. NET Bits

Встановити пакунок Nuget

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

У вашій програмі.cs add:

```csharp
builder.Services.AddProgressiveWebApp();
```

Потім створіть сім' ї, які відповідають розмірам нижче [тут](https://realfavicongenerator.net/) Це інструмент, яким ви можете скористатися для їх створення. Це дійсно може бути будь-яка ікона (Я використовував слово mamoji ⇩)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Тоді вам потрібен маніфест.json.

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