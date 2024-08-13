# Usando ImageSharp.Web con ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Introducción

[ImageSharp](https://docs.sixlabors.com/index.html )es una potente biblioteca de procesamiento de imágenes que le permite manipular imágenes de diversas maneras. ImageSharp.Web es una extensión de ImageSharp que proporciona funcionalidad adicional para trabajar con imágenes en aplicaciones ASP.NET Core. En este tutorial exploraremos cómo usar ImageSharp.Web para redimensionar, recortar y formatear imágenes en esta aplicación.

[TOC]

## ImageSharp.Web Instalación

Para empezar con ImageSharp.Web, necesitará instalar los siguientes paquetes de NuGet:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Configuración de ImageSharp.Web

En nuestro archivo Program.cs configuramos ImageSharp.Web. En nuestro caso nos referimos a nuestras imágenes y las almacenamos en una carpeta llamada "imágenes" en el wwwroot de nuestro proyecto. Luego configuramos el ImageSharp.Web middleware para utilizar esta carpeta como fuente de nuestras imágenes.

ImageSharp.Web también utiliza una carpeta 'cache' para almacenar archivos procesados (esto evita que repugne archivos cada vez).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Estas carpetas son relativas a la wwwroot por lo que tenemos la siguiente estructura:

![Estructura de la carpeta](/cachefolder.png)

ImageSharp.Web tiene múltiples opciones para almacenar sus archivos y almacenamiento en caché (ver aquí para todos los detalles:[https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-11%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Por ejemplo, para almacenar sus imágenes en un contenedor de blobs de Azure (habilidoso para escalar) utilizaría el proveedor de Azure con AzureBlobCacheOptions:

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

## Uso de ImageSharp.Web

Ahora que tenemos esta configuración, es muy simple utilizarla dentro de nuestra aplicación. Por ejemplo, si queremos servir una imagen redimensionada, podríamos usar cualquiera de las dos.[el TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)o la especificación de la URL directamente.

TagHelper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Observe que con esto estamos redimensionando la imagen, estableciendo el ancho y la altura, y también configurando el RedimensionamientoMode y recolorizando la imagen.

En esta aplicación vamos de la manera más sencilla y simplemente usamos parámetros de la cadena de consulta. Para la marca hacia abajo utilizamos una extensión que nos permite especificar el tamaño y el formato de la imagen.

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

Esto nos da la felxibilidad de especificarlos en los posts como

```markdown
![image](/image.jpg?format=webp&quality=50)
```

¿De dónde vendrá esta imagen?`wwwroot/articleimages/image.jpg`y ser redimensionado a un 50% de calidad y en formato webp.

O simplemente podemos utilizar la imagen como es y será redimensionada y formateada como se especifica en la cadena de consulta.

## Conclusión

Como usted ha visto ImageSharp.Web nos da una gran capacidad para redimensionar y formatear imágenes en nuestras aplicaciones ASP.NET Core. Es fácil de configurar y usar y proporciona mucha flexibilidad en cómo podemos manipular imágenes en nuestras aplicaciones.