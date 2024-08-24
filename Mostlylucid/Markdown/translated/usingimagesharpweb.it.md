# Utilizzando ImageSharp.Web con ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Introduzione

[Condivisione immagini](https://docs.sixlabors.com/index.html) è una potente libreria di elaborazione delle immagini che consente di manipolare le immagini in una varietà di modi. ImageSharp.Web è un'estensione di ImageSharp che fornisce funzionalità aggiuntive per lavorare con le immagini nelle applicazioni ASP.NET Core. In questo tutorial, esploreremo come usare ImageSharp.Web per ridimensionare, ritagliare e formattare le immagini in questa applicazione.

[TOC]

## Installazione Web di ImageSharp

Per iniziare con ImageSharp.Web, è necessario installare i seguenti pacchetti NuGet:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Configurazione Web di ImageSharp

Nel nostro file Program.cs abbiamo quindi impostato ImageSharp.Web. Nel nostro caso ci stiamo riferendo e archiviando le nostre immagini in una cartella chiamata "immagini" nel wwwroot del nostro progetto. Abbiamo quindi impostato il middleware ImageSharp.Web per utilizzare questa cartella come fonte delle nostre immagini.

ImageSharp.Web utilizza anche una cartella 'cache' per memorizzare i file elaborati (questo impedisce di riporre i file ogni volta).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Queste cartelle sono relative al wwwroot quindi abbiamo la seguente struttura:

![Struttura cartella](/cachefolder.png)

ImageSharp.Web ha più opzioni per memorizzare i file e la cache (vedi qui per tutti i dettagli: [https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Per esempio per memorizzare le immagini in un contenitore blob Azure (utile per scalare) si userebbe il Provider Azure con AzureBlobCacheOpzioni:

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

## Uso Web di ImageSharp

Ora che abbiamo questo set up è davvero semplice da usare all'interno della nostra applicazione. Per esempio, se vogliamo servire un'immagine ridimensionata potremmo usare entrambi [il tagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) o specificare direttamente l'URL.

TagHelper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Si noti che con questo stiamo ridimensionando l'immagine, impostando la larghezza e l'altezza, e anche impostando il ResizeMode e ricolorando l'immagine.

In questa applicazione andiamo il modo più semplice e basta utilizzare querystring parametri. Per il markdown utilizziamo un'estensione che ci permette di specificare la dimensione e il formato dell'immagine.

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

Questo ci dà la felicità di specificare questi nei post come

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Da dove verrà questa immagine `wwwroot/articleimages/image.jpg` ed essere ridimensionato al 50% di qualità e in formato webp.

Oppure possiamo semplicemente usare l'immagine così com'è e sarà ridimensionata e formattata come specificato nella stringa di query.

## DockerCity name (optional, probably does not need a translation)

Notare la `cache` Forlder Ho usato sopra deve essere scrivibile dall'applicazione. Se stai usando Docker, devi assicurarti che sia cosi'.
Vedi [il mio post precedente](/blog/imagesharpwithdocker) per come lo gestisco usando un volume mappato.

## Conclusione

Come avete visto ImageSharp.Web ci offre una grande capacità di ridimensionare e formattare le immagini nelle nostre applicazioni ASP.NET Core. È facile da configurare e utilizzare e offre molta flessibilità nel modo in cui possiamo manipolare le immagini nelle nostre applicazioni.