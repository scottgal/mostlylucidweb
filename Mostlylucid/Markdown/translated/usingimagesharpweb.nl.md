# ImageSharp.Web wordt gebruikt met ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Inleiding

[ImageSharp](https://docs.sixlabors.com/index.html)is een krachtige image processing library die u toelaat om afbeeldingen te manipuleren op verschillende manieren. ImageSharp.Web is een uitbreiding van ImageSharp die extra functionaliteit biedt voor het werken met afbeeldingen in ASP.NET Core applicaties. In deze tutorial zullen we onderzoeken hoe ImageSharp.Web gebruiken om afbeeldingen te wijzigen, bij te snijden en te formatteren in deze toepassing.

[TOC]

## ImageSharp.Web-installatie

Om te beginnen met ImageSharp.Web, moet u de volgende NuGet pakketten installeren:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## ImageSharp.Web configuratie

In ons programma.cs bestand hebben we vervolgens ImageSharp.Web ingesteld. In ons geval verwijzen we naar en opslaan we onze afbeeldingen in een map genaamd "afbeeldingen" in de wwwroot van ons project. Vervolgens zetten we de ImageSharp.Web middleware op om deze map te gebruiken als de bron van onze afbeeldingen.

ImageSharp.Web gebruikt ook een 'cache' map om verwerkte bestanden op te slaan (dit voorkomt dat bestanden elke keer worden gereporbeerd).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Deze mappen zijn relatief aan de wwwroot dus we hebben de volgende structuur:

![Mapstructuur](/cachefolder.png)

ImageSharp.Web heeft meerdere opties voor waar u uw bestanden en caching opslaat (zie hier voor alle details:[https://docs.sislabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Bijvoorbeeld om uw afbeeldingen op te slaan in een Azure blob container (handig om te schalen) zou u de Azure Provider gebruiken met AzureBlobCacheOptions:

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

## ImageSharp.Web Usage

Nu we deze set-up hebben is het heel eenvoudig om het te gebruiken in onze toepassing. Bijvoorbeeld als we een verkleinde afbeelding willen serveren kunnen we beide gebruiken[de TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)of de URL direct specificeren.

TagHelper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Merk op dat we hiermee de grootte van de afbeelding wijzigen, de breedte en hoogte instellen, en ook de grootte wijzigen en de afbeelding herkleuren.

In deze app gaan we op de eenvoudigere manier en gebruiken we alleen querystring parameters. Voor de markdown gebruiken we een extensie waarmee we de grootte en het formaat van de afbeelding kunnen opgeven.

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

Dit geeft ons de bruikbaarheid van ofwel het specificeren van deze in de berichten zoals

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Waar deze afbeelding vandaan komt`wwwroot/articleimages/image.jpg`en worden verkleind tot 50% kwaliteit en in webp formaat.

Of we kunnen gewoon het beeld gebruiken zoals het is en het zal worden aangepast en geformatteerd zoals gespecificeerd in de querystring.

## Conclusie

Zoals u hebt gezien ImageSharp.Web geeft ons een geweldige mogelijkheid om afbeeldingen te wijzigen en te formatteren in onze ASP.NET Core toepassingen. Het is eenvoudig in te stellen en te gebruiken en biedt veel flexibiliteit in hoe we beelden kunnen manipuleren in onze toepassingen.