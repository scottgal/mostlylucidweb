# إنشاء شبكة معلوماتك عن طريق شبكة الإنترنت (PWA)

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-08-01-TT11: 36</datetime>

في هذه المقالة، سأريكم كيف تجعلون موقعكم على شبكة الإنترنت ASP.NET الأساسي هو PWA (تطبيق على شبكة الإنترنت).

## النفقات قبل الاحتياجات

انها حقاً بسيطة جداً انظر https://github.com/medskristensen/WebEssentials.AspNetscore.Serviviceworker/tree/master

## SPSP.net

تثبيت الحزمة النوتة

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

في برنامجك.cs يُضاف ما يلي:

```csharp
builder.Services.AddProgressiveWebApp();
```

ثم إنشاء بعض الافاكتونات التي تطابق الأحجام تحت [هنا هنا](https://realfavicongenerator.net/) هو أداة يمكنك استخدامها لخلقها. هذه يمكن أن تكون حقاً أي أي أي أي أيقونة (استخدمت أيقونة رمزية)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

ثم تحتاج إلى بيان.

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