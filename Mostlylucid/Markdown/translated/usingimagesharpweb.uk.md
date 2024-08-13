# Використання ImageShalp. Web з ядром ASP. NET

<datetime class="hidden">2024- 08- 13T14: 16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Вступ

[Розширення зображеньComment](https://docs.sixlabors.com/index.html)є потужною бібліотекою для обробки зображень, яка надає вам змогу керувати зображеннями у декілька способів. ImageSharm. Web - це розширення можливостей ImageSharp, яке надає додаткові можливості для роботи з зображеннями у програмах, призначених для роботи з ядрами ASP. NET. У цьому підручнику ми дослідимо, як використовувати ImageSharp. Web для зміни розмірів, обрізання і форматування зображень у цій програмі.

[TOC]

## Встановлення imageShalp. Web

Щоб розпочати роботу з ImageSharp. Web, вам слід встановити такі пакунки NuGet:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Налаштування imageShalp. WebComment

У нашому файлі Program. cs ми налаштували програму ImageSharp. Web. У нашому випадку ми маємо на увазі і зберігаємо зображення у теці з назвою " images " у кореневій теці нашого проекту www. Потім ми встановимо програму ImageSap. Wab для використання цієї теки як джерела наших зображень.

ImageShalp. Web також використовує теку " cache " для зберігання оброблених файлів (це запобігає відшкодуванню файлів кожного разу).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Ці теки відносяться до кореневої теки wwwroot, отже у нас є така структура:

![Структура тек](/cachefolder.png)

ImageShalp. Web має декілька параметрів, за допомогою яких ви можете зберегти ваші файли і кешування (див. тут, щоб дізнатися більше:[https: // docs. 6labors.com/articles/imagesapal. web/ imageprofiders. html? tabs=tabid- 1% 2Ctabid- 1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Наприклад, щоб зберігати ваші зображення у контейнері Azure (корисно для масштабування), вам слід скористатися постачальником Azure за допомогою параметрів AzureBlobCache:

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

## Використання imageSape. Web

Тепер, коли ми налаштували це зображення, дуже просто використовувати його всередині нашої програми. Наприклад, якщо ми хочемо облаштувати збільшене зображення, ми можемо використовувати або інше зображення.[Інструмент довідки міток](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)або напряму вказати адресу URL.

Допомога мітками:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Зауважте, що за допомогою цього пункту ми змінюємо розміри зображення, встановлюємо ширину і висоту, а також встановлюємо значення Змінити розмір і змінити колір зображення.

У цій програмі ми пройдемо простіший спосіб і просто використаємо параметри querystring. Для позначення цього параметра ми скористаємося суфіксом, який надасть нам змогу вказати розмір і формат зображення.

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

Це надає нам можливість вказати їх у таких постах, як

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Звідки прийде це зображення?`wwwroot/articleimages/image.jpg`і його розміри у 50% якості у форматі webp.

Або ж ми можемо використовувати зображення у такому вигляді, у якому його буде змінено і відформатовано, як вказано у рядку діалогу.

## Висновки

Як ви вже бачили, ImageSap. Web надає нам чудову можливість змінювати розмір і форматувати зображення у наших програмах ядра ASP. NET. Їх дуже просто налаштувати і використати, а також надати нам велику гнучкість у тому, як можна маніпулювати зображеннями у наших програмах.