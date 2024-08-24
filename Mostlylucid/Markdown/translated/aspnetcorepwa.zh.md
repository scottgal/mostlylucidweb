# 使ASP.Net核心网站成为PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-008-001T11:36</datetime>

在本篇文章中,我将教你如何使您的 ASP.NET核心网站成为PWA(渐进式网络应用程序)。

## 先决条件

它非常简单,见https://github.com/madskristensen/WebEsssentials.AspNetCore.ServiciceWorker/tree/master。

## ASP.NET比特项目

安装 Niget 软件包

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

在您的程式中, cs 添加 :

```csharp
builder.Services.AddProgressiveWebApp();
```

然后创建一些与下面大小相匹配的飞行要量 [在这里](https://realfavicongenerator.net/) 是一个可以用来创建它们的工具。 这些真的可以是任何图标( 我使用了 emoji {} @ {} )

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

然后,你需要一个名单。 json。

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