# ImageSharp con Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp es una gran biblioteca para trabajar con imágenes en.NET. Es rápido, fácil de usar y tiene muchas características. En este post, te mostraré cómo usar ImageSharp con Docker para crear un servicio de procesamiento de imágenes simple.

## ¿Qué es ImageSharp?

ImageSharp me permite trabajar sin problemas con imágenes en.NET. Es una biblioteca multiplataforma que admite una amplia gama de formatos de imagen y proporciona una API sencilla para el procesamiento de imágenes. Es rápida, eficiente y fácil de usar.

Sin embargo hay un problema en mi configuración usando docker e ImageSharp. Cuando intento cargar una imagen desde un archivo, obtengo el siguiente error:
'Acceso denegado a la ruta /wwroot/cache/ etc...'
Esto es causado por las instalaciones Docker ASP.NET que no permiten el acceso de escritura al directorio de caché ImageSharp utiliza para almacenar archivos temporales.

## Solución

La solución es montar un volumen en el contenedor Docker que apunte a un directorio en la máquina host. De esta manera, la biblioteca ImageSharp puede escribir en el directorio de caché sin ningún problema.

Aquí está cómo hacerlo:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Aquí puede ver que mapeo el archivo /app/wwwroot/cache a un directorio local en mi máquina host. De esta manera, ImageSharp puede escribir al directorio de caché sin ningún problema.

En mi máquina Ubuntu creé un directorio /mnt/imagecache y luego corrí el comando folowing para que sea escribible (por cualquiera, sé que esto no es seguro pero no soy un gurú de Linux :))

```shell
chmod  777 -p /mnt/imagecache
```

En mi program.cs tengo esta línea:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Como el cacheroot predeterminado a wwwroot esto ahora escribirá en el directorio /mnt/imagecache en el equipo host.