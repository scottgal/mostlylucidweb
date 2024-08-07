# Condivisione immagini con Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp è una grande libreria per lavorare con le immagini in.NET. E'veloce, facile da usare, e ha un sacco di funzioni. In questo post, vi mostrerò come utilizzare ImageSharp con Docker per creare un semplice servizio di elaborazione delle immagini.

## Che cos'è ImageSharp?

ImageSharp mi permette di lavorare senza soluzione di continuità con le immagini in.NET. È una libreria multipiattaforma che supporta una vasta gamma di formati di immagine e fornisce una semplice API per l'elaborazione delle immagini. È veloce, efficiente e facile da usare.

Tuttavia c'è un problema nel mio setup usando docker e ImageSharp. Quando provo a caricare un'immagine da un file, ottengo il seguente errore:
Accesso negato al percorso /wwroot/cache/ etc...
Questo è causato da Docker ASP.NET installazioni che non consentono l'accesso in scrittura alla directory cache ImageSharp utilizza per memorizzare i file temporanei.

## Soluzione

La soluzione consiste nel montare un volume nel contenitore docker che punta ad una directory sulla macchina host. In questo modo, la libreria ImageSharp può scrivere nella directory cache senza problemi.

Ecco come si fa:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Qui vedete che ho mappato il file /app/wwwroot/cache in una directory locale sulla mia macchina host. In questo modo, ImageSharp può scrivere nella directory cache senza problemi.

Sulla mia macchina Ubuntu ho creato una directory /mnt/imagecache e poi ho eseguito il comando following per renderlo scrivibile (da chiunque, so che questo non è sicuro ma non sono un guru Linux:))

```shell
chmod  777 -p /mnt/imagecache
```

Nel mio programma.cs ho questa linea:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Come impostazione predefinita della cacheroot per wwwroot, ora scriverà alla directory /mnt/imagecache sulla macchina host.