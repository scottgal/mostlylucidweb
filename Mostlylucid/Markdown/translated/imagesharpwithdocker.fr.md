# ImageSharp avec Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp est une excellente bibliothèque pour travailler avec des images dans.NET. Il est rapide, facile à utiliser, et a beaucoup de fonctionnalités. Dans ce post, je vais vous montrer comment utiliser ImageSharp avec Docker pour créer un service de traitement d'image simple.

## Qu'est-ce que ImageSharp?

ImageSharp me permet de travailler parfaitement avec les images en.NET. Il s'agit d'une bibliothèque multiplateforme qui prend en charge une large gamme de formats d'images et fournit une API simple pour le traitement d'images. C'est rapide, efficace et facile à utiliser.

Cependant, il y a un problème dans ma configuration en utilisant docker et ImageSharp. Lorsque j'essaie de charger une image à partir d'un fichier, j'obtiens l'erreur suivante:
'Accès refusé au chemin /wwroot/cache/ etc...'
Ceci est causé par les installations Docker ASP.NET ne permettant pas l'accès en écriture au répertoire cache ImageSharp utilise pour stocker des fichiers temporaires.

## Solution

La solution est de monter un volume dans le conteneur docker qui pointe vers un répertoire sur la machine hôte. De cette façon, la bibliothèque ImageSharp peut écrire dans le répertoire cache sans aucun problème.

Voici comment le faire :

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Ici, vous voyez que je map le fichier /app/wwwroot/cache vers un répertoire local sur ma machine hôte. De cette façon, ImageSharp peut écrire dans le répertoire cache sans aucun problème.

Sur ma machine Ubuntu, j'ai créé un répertoire /mnt/imagecache puis lancé la commande folowing pour la rendre lisible (par n'importe qui, je sais que ce n'est pas sécurisé mais je ne suis pas un gourou Linux :)

```shell
chmod  777 -p /mnt/imagecache
```

Dans mon programme.cs j'ai cette ligne:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Comme le cacheroot par défaut sur wwwroot, cela va maintenant écrire dans le répertoire /mnt/imagecache de la machine hôte.