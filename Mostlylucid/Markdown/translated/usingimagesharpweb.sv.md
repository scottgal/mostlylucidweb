# Använda ImageSharp.Web med ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Inledning

[AvbildaSharp](https://docs.sixlabors.com/index.html) är ett kraftfullt bildbehandlingsbibliotek som låter dig manipulera bilder på en mängd olika sätt. ImageSharp.Web är en förlängning av ImageSharp som ger ytterligare funktionalitet för att arbeta med bilder i ASP.NET Core program. I denna handledning kommer vi att undersöka hur du använder ImageSharp.Web för att ändra storlek, beskära och formatera bilder i detta program.

[TOC]

## Installation av ImageSharp.Webb

För att komma igång med ImageSharp.Web måste du installera följande NuGet-paket:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Inställning av ImageSharp.Web

I vår Program.cs-fil ställer vi sedan in ImageSharp.Web. I vårt fall hänvisar vi till och lagrar våra bilder i en mapp som kallas "bilder" i wwwroot för vårt projekt. Vi ställer sedan in ImageSharp.Web middleware för att använda denna mapp som källan till våra bilder.

ImageSharp.Web använder också en "cache" mapp för att lagra bearbetade filer (detta förhindrar att den reporcesserar filer varje gång).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Dessa mappar är i förhållande till wwwroot så vi har följande struktur:

![Katalogstruktur](/cachefolder.png)

ImageSharp.Web har flera alternativ för var du lagrar dina filer och caching (se här för alla detaljer: [https://docs.sixlabors.com/artiklar/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Till exempel för att lagra dina bilder i en Azure blob behållare (hantera för skalning) skulle du använda Azure Provider med AzureBlobCacheOptions:

```bash
dotnet add SixLabors.ImageSharp.Web.Providers.Azure
```

```csharp
// Configure and register the containers.  
// Alteratively use `appsettings.json` to represent the class and bind those settings.
.Configure<AzureBlobStorageImageProviderOptions>(options =>
{
    // The "BlobContainers" collection allows registration of multiple containers.
    options.BlobContainers.Add(new AzureBlobContainerClientOptions
    {
        ConnectionString = {AZURE_CONNECTION_STRING},
        ContainerName = {AZURE_CONTAINER_NAME}
    });
})
.AddProvider<AzureBlobStorageImageProvider>()
```

## ImageSharp.Web-användning

Nu när vi har denna uppsättning är det väldigt enkelt att använda den i vår applikation. Till exempel om vi vill tjäna en storleksändring bild vi kan göra antingen använda [Tagghjälparen](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) eller ange webbadressen direkt.

Tagghjälp:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Lägg märke till att vi med detta ändrar storlek på bilden, ställer in bredden och höjden, och även ställer in storleksändringen och färgar om bilden.

I denna app går vi den enklare vägen och bara använda frågesträng parametrar. För markeringen använder vi en förlängning som gör att vi kan ange bildens storlek och format.

```csharp
    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
            {
                if(link.Url.StartsWith("http")) continue;
                
                if (!link.Url.Contains("?"))
                {
                   link.Url += "?format=webp&quality=50";
                }

                link.Url = "/articleimages/" + link.Url;
            }
               
    }
```

Detta ger oss möjlighet att antingen specificera dessa i posterna som

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Där bilden kommer ifrån `wwwroot/articleimages/image.jpg` och ändras till 50 % kvalitet och i webp-format.

Eller vi kan bara använda bilden som är och det kommer att ändras storlek och formateras som anges i frågesträngen.

## Docka

Lägg märke till `cache` Former jag har använt ovan måste skrivas av ansökan. Om du använder Docker måste du se till att så är fallet.
Se också [min tidigare inlägg](/blog/imagesharpwithdocker) för hur jag hanterar detta med hjälp av en mappad volym.

## Slutsatser

Som du har sett ImageSharp.Web ger oss en stor förmåga att ändra storlek och formatera bilder i våra ASP.NET Core-applikationer. Det är lätt att konfigurera och använda och ger en hel del flexibilitet i hur vi kan manipulera bilder i våra applikationer.