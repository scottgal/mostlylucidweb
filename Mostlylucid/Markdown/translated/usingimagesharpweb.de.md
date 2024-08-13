# Verwendung von ImageSharp.Web mit ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Einleitung

[ImageSharp](https://docs.sixlabors.com/index.html)ist eine leistungsstarke Bildverarbeitungsbibliothek, die es Ihnen ermöglicht, Bilder auf verschiedene Arten zu manipulieren. ImageSharp.Web ist eine Erweiterung von ImageSharp, die zusätzliche Funktionen für die Arbeit mit Bildern in ASP.NET Core-Anwendungen bietet. In diesem Tutorial werden wir untersuchen, wie Sie ImageSharp.Web verwenden, um Bilder in dieser Anwendung zu formatieren, zu schneiden und zu formatieren.

[TOC]

## ImageSharp.Web Installation

Um mit ImageSharp.Web zu beginnen, müssen Sie die folgenden NuGet-Pakete installieren:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## ImageSharp.Web-Konfiguration

In unserer Datei Program.cs richten wir dann ImageSharp.Web ein. In unserem Fall beziehen wir uns auf und speichern unsere Bilder in einem Ordner namens "images" im wwwroot unseres Projekts. Anschließend richten wir die ImageSharp.Web Middleware ein, um diesen Ordner als Quelle unserer Bilder zu verwenden.

ImageSharp.Web verwendet auch einen 'Cache'-Ordner, um verarbeitete Dateien zu speichern (dadurch wird verhindert, dass Dateien jedes Mal wieder rückgängig gemacht werden).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Diese Ordner sind relativ zum wwwroot, so dass wir die folgende Struktur haben:

![Ordnerstruktur](/cachefolder.png)

ImageSharp.Web hat mehrere Optionen, wo Sie Ihre Dateien und Caching speichern (siehe hier für alle Details:[Veröffentlichungen der Europäischen Gemeinschaften, 2001.](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Zum Beispiel, um Ihre Bilder in einem Azure-Blob-Container zu speichern (handlich zur Skalierung) würden Sie den Azure-Provider mit AzureBlobCacheOptionen verwenden:

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

## ImageSharp.Web-Nutzung

Jetzt, da wir diese Einrichtung haben, ist es wirklich einfach, es in unserer Anwendung zu verwenden. Zum Beispiel, wenn wir ein vergrößertes Bild dienen möchten, könnten wir entweder[der TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)oder die URL direkt angeben.

TagHelper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Beachten Sie, dass wir damit das Bild verändern, Breite und Höhe einstellen und auch die Größe ändern und das Bild umfärben.

In dieser App gehen wir den einfacheren Weg und verwenden einfach Querystring-Parameter. Für den Markdown verwenden wir eine Erweiterung, mit der wir die Bildgröße und das Format angeben können.

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

Dies gibt uns die Möglichkeit, entweder diese in den Stellen wie

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Wo dieses Bild herkommt`wwwroot/articleimages/image.jpg`und auf 50% Qualität und im Webp-Format verkleinert werden.

Oder wir können einfach das Bild so verwenden, wie es ist und es wird wie im Querystring angegeben verkleinert und formatiert werden.

## Schlußfolgerung

Wie Sie gesehen haben ImageSharp.Web gibt uns eine große Fähigkeit, die Größe und Formatierung von Bildern in unseren ASP.NET Core-Anwendungen. Es ist einfach einzurichten und zu verwenden und bietet eine Menge Flexibilität in, wie wir Bilder in unseren Anwendungen manipulieren können.