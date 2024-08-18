# ImageSharp med Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp är ett bra bibliotek för att arbeta med bilder i.NET. Den är snabb, lätt att använda och har många funktioner. I det här inlägget ska jag visa dig hur du använder ImageSharp med Docker för att skapa en enkel bildbehandlingstjänst.

## Vad är ImageSharp?

ImageSharp gör det möjligt för mig att sömlöst arbeta med bilder i.NET. Det är ett plattformsoberoende bibliotek som stöder ett brett utbud av bildformat och ger ett enkelt API för bildbehandling. Den är snabb, effektiv och lätt att använda.

Men det finns ett problem i min installation med docka och ImageSharp. När jag försöker ladda en bild från en fil får jag följande fel:
Åtkomst nekad till stigen /wwroot/cache/ etc...
Detta orsakas av Docker ASP.NET-installationer som inte tillåter skrivåtkomst till cachekatalogen ImageSharp använder för att lagra tillfälliga filer.

## Lösning

Lösningen är att montera en volym i dockerbehållaren som pekar på en katalog på värdmaskinen. På så sätt kan ImageSharp-biblioteket skriva till cachekatalogen utan några problem.

Så här gör vi:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Här ser du att jag kartlägger /app/wwwroot/cache-filen till en lokal katalog på min värdmaskin. På så sätt kan ImageSharp skriva till cachekatalogen utan några problem.

På min Ubuntu-maskin skapade jag en katalog /mnt/imagecache och körde sedan kommandot folowing för att göra den skrivbar (av vem som helst, jag vet att detta inte är säkert men jag är ingen Linux guru :)

```shell
chmod  777 -p /mnt/imagecache
```

I mitt program.cs Jag har denna rad:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Som cacheroot standard till wwwroot kommer detta nu att skriva till katalogen /mnt/imakecache på värddatorn.