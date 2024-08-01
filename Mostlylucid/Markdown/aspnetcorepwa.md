# Making your ASP.NET Core Website a PWA

In this article, I'll show you how to make your ASP.NET Core website a PWA (Progressive Web App).

## Prerequisites

It's really pretty simple see https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET Bits

Install the Nuget package

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```
In your program.cs add:

``` csharp
builder.Services.AddProgressiveWebApp();
```


Then create some favicons which match the sizes below [here](https://realfavicongenerator.net/). These can really be any icon (I used an emoji 😉) 

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Then you need a manifest.json

``` json
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