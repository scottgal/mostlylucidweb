# Käyttämällä ImageSharp.Web ASP.NET Corea

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Johdanto

[ImageSharp](https://docs.sixlabors.com/index.html) on tehokas kuvankäsittelykirjasto, jonka avulla kuvia voi manipuloida monin eri tavoin. ImageSharp.Web on laajennus ImageSharpiin, joka tarjoaa lisätoimintoja kuvien kanssa työskentelyyn ASP.NET Core -sovelluksissa. Tässä tutoriaalissa tutkimme, miten tässä sovelluksessa voidaan käyttää ImageSharp.Web-verkkoa kuvien kokoa, satoa ja muotoa.

[TÄYTÄNTÖÖNPANO

## ImageSharp.Web-asennus

Jotta pääset alkuun ImageSharp.Web:llä, sinun täytyy asentaa seuraavat NuGet-paketit:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## ImageSharp.Web:n asetukset

Ohjelma.cs-tiedostossa perustamme sitten ImageSharp.Web-sivuston. Meidän tapauksessamme viittaamme ja tallennamme kuviamme "images"-nimiseen kansioon projektimme www-juuressa. Sen jälkeen perustimme ImageSharp.Web-väliohjelmiston, jonka avulla voimme käyttää tätä kansiota kuviemme lähteenä.

ImageSharp.Web käyttää myös "cache"-kansiota käsiteltyjen tiedostojen tallentamiseen (tämä estää tiedostojen uudelleenkäsittelyn joka kerta).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Nämä kansiot ovat suhteessa www-rootiin, joten meillä on seuraava rakenne:

![Kansion rakenne](/cachefolder.png)

ImageSharp.Webissä on useita vaihtoehtoja, joihin tallennat tiedostot ja välimuistit (ks. kaikki yksityiskohdat täältä: [https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Esimerkiksi tallentaaksesi kuvasi Azure blob -purkkiin (hidastamiseen) käytät Azure Provideria AzureBlobCacheOptionsin kanssa:

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

## ImageSharp.Web-käyttö

Nyt kun meillä on tämä laite, sitä on todella helppo käyttää sovelluksessamme. Esimerkiksi, jos haluamme tarjota suurennetun kuvan, voimme tehdä joko [TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) tai täsmennä URL suoraan.

Tag Helper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Huomaa, että tämän myötä kokoamme kuvan uudelleen, asetamme leveyden ja korkeuden, ja asetamme myös ResizeModen ja värjäämme kuvan uudelleen.

Tässä sovelluksessa kuljemme yksinkertaisempaan suuntaan ja käytämme vain querystring-parametreja. Markdownia varten käytämme laajennusta, jonka avulla voimme määrittää kuvan koon ja muodon.

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

Tämä antaa meille ymmärrettävyyden joko tarkentaa näitä virkoihin kuten

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Mistä tämä kuva tulee? `wwwroot/articleimages/image.jpg` ja on kooltaan 50 % laatua ja webp-muodossa.

Tai voimme vain käyttää kuvaa sellaisena kuin se on, ja se muutetaan ja muotoillaan kyselyn mukaisesti.

## Docker

Huomaa, että `cache` Haalarin, jota olen käyttänyt edellä, täytyy olla kirjoitettavissa sovelluksen kautta. Jos käytät Dockeria, varmista, että asia on näin.
Katso [aiempi postaukseni](/blog/imagesharpwithdocker) siitä, miten hallitsen tätä kartoitetulla volyymilla.

## Päätelmät

Kuten olet nähnyt ImageSharp.Web antaa meille suuren kyvyn muuttaa ja muotoilla kuvia ASP.NET Core -sovelluksissamme. Se on helppo asentaa ja käyttää, ja se tarjoaa paljon joustavuutta siinä, miten voimme manipuloida kuvia sovelluksissamme.