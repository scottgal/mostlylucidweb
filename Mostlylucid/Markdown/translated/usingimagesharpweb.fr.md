# Utilisation d'ImageSharp.Web avec ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Présentation

[ImageSharp](https://docs.sixlabors.com/index.html) ImageSharp.Web est une extension d'ImageSharp qui fournit des fonctionnalités supplémentaires pour travailler avec des images dans les applications ASP.NET Core. Dans ce tutoriel, nous allons explorer comment utiliser ImageSharp.Web pour redimensionner, recadrer et formater des images dans cette application.

[TOC]

## ImageSharp.Installation web

Pour commencer avec ImageSharp.Web, vous devrez installer les paquets suivants NuGet :

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Configuration d'ImageSharp.Web

Dans notre fichier Program.cs, nous avons ensuite configuré ImageSharp.Web. Dans notre cas, nous faisons référence et stockons nos images dans un dossier appelé "images" dans le wwwroot de notre projet. Nous avons ensuite configuré l'intergiciel ImageSharp.Web pour utiliser ce dossier comme source de nos images.

ImageSharp.Web utilise également un dossier 'cache' pour stocker les fichiers traités (ce qui l'empêche de reproduire les fichiers à chaque fois).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Ces dossiers sont relatifs au wwwroot donc nous avons la structure suivante:

![Structure du dossier](/cachefolder.png)

ImageSharp.Web a plusieurs options pour l'endroit où vous stockez vos fichiers et la mise en cache (voir ici pour tous les détails:[https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Par exemple, pour stocker vos images dans un conteneur Azure blob (handy for scale) vous utiliseriez le fournisseur Azure avec AzureBlobCacheOptions:

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

## ImageSharp.Utilisation Web

Maintenant que nous avons cette configuration, il est vraiment simple de l'utiliser à l'intérieur de notre application. Par exemple, si nous voulons servir une image redimensionnée, nous pourrions faire l'un ou l'autre[le TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)ou indiquez directement l'URL.

TagHelper :

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Notez qu'avec cela nous redimensionnons l'image, réglons la largeur et la hauteur, et aussi réglons le ResizeMode et recolorons l'image.

Dans cette application, nous allons de la manière la plus simple et utilisons simplement les paramètres de la chaîne de requête. Pour le balisage, nous utilisons une extension qui nous permet de spécifier la taille et le format de l'image.

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

Cela nous donne la felxibilité de les spécifier dans les messages comme

```markdown
![image](/image.jpg?format=webp&quality=50)
```

D'où viendra cette image`wwwroot/articleimages/image.jpg`et être redimensionné à 50% de qualité et en format webp.

Ou nous pouvons simplement utiliser l'image telle qu'elle est et elle sera redimensionnée et formatée comme spécifié dans la chaîne de requête.

## Conclusion

Comme vous l'avez vu, ImageSharp.Web nous offre une grande capacité de redimensionner et de formater des images dans nos applications ASP.NET Core. Il est facile à configurer et à utiliser et offre beaucoup de flexibilité dans la façon dont nous pouvons manipuler des images dans nos applications.