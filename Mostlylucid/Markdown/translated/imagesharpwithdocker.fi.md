# Kuvanvahvistaja Dockerilla

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp on loistava kirjasto kuvien kanssa työskentelyyn.NETissä. Se on nopea, helppokäyttöinen ja siinä on paljon ominaisuuksia. Tässä viestissä näytän sinulle, kuinka voit käyttää ImageSharpia Dockerin kanssa luodaksesi yksinkertaisen kuvankäsittelypalvelun.

## Mikä on ImageSharp?

ImageSharp mahdollistaa saumattoman työskentelyn kuvien kanssa.NETissä. Se on cross-platform-kirjasto, joka tukee monenlaisia kuvaformaatteja ja tarjoaa yksinkertaisen API:n kuvankäsittelyyn. Se on nopea, tehokas ja helppokäyttöinen.

Setissäni on kuitenkin ongelma dockerin ja ImageSharpin avulla. Kun yritän ladata kuvan tiedostosta, saan seuraavan virheen:
Pääsy kielletty polulle / wwroot/cache/ ym....
Tämä johtuu Docker ASP.NET -laitteista, jotka eivät salli kirjoitusoikeutta välimuistihakemistoon ImageSharpin avulla tallentaa tilapäisiä tiedostoja.

## Ratkaisu

Ratkaisuna on asentaa laatikkoon äänenvoimakkuus, joka osoittaa isäntäkoneen hakemistoon. Näin ImageSharp-kirjasto voi kirjoittaa välimuistihakemistoon ilman ongelmia.

Näin se tehdään:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Tässä näet, että kartoitan /app/wwwroot/cache-tiedoston paikalliseen hakemistoon isäntäkoneessani. Näin ImageSharp voi kirjoittaa välimuistihakemistoon ilman ongelmia.

Ubuntu-koneellani loin hakemiston /mnt/imagecache ja suoritin sitten folowing-komennon, jotta se olisi kirjoitettavissa (kuka tahansa, tiedän, että tämä ei ole turvallista, mutta en ole mikään Linux-guru :)

```shell
chmod  777 -p /mnt/imagecache
```

Ohjelmassa.cs:ssä minulla on tämä rivi:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Koska cacheroot-oletukset ovat wwwrootille, tämä kirjoitetaan nyt isäntäkoneen /mnt/imagecache-hakemistoon.