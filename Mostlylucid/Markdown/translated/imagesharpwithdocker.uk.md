# Розширення різкості зображень за допомогою DockerName

<datetime class="hidden">2024- 08- 01: 00</datetime>

<!--category-- Docker, ImageSharp -->
ImageShalp - це чудова бібліотека для роботи з зображеннями у. NET. Вона швидка, проста і має багато особливостей. У цьому дописі я покажу вам, як використовувати ImageSharp з Docker для створення простої служби обробки зображень.

## Що таке ImageSape?

Зусилля зображення дозволяє мені безперешкодно працювати з зображеннями у NET. Це міжплатформова бібліотека, яка підтримує широкий спектр форматів зображень і надає простий API для обробки зображень. Він швидкий, ефективний і простий у використанні.

Тем не менее, в моей установке есть проблема с помощью докера и ImageSagg. Коли я намагаюся завантажити зображення з файла, я отримую таку помилку:
"Відхилення від шляху / wwroot/cache / тощо... "
Це спричинено встановленнями Docker ASP. NET, які не надають доступу для запису до каталогу кешу ImageShalp, який використовує для зберігання тимчасових файлів.

## Розв' язок

Розв' язання проблеми полягає у змонтуванні гучності у контейнері docker, який вказує на каталог на комп' ютері вузла. Таким чином, бібліотека ImageShalp може записувати до каталогу кешу без будь- яких проблем.

Ось як це зробити:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Тут ви бачите, що я прив' язую файл / apps/ wwwroot/cache до локального каталогу на моєму комп' ютері. Таким чином, ImageSharm може записувати до каталогу кешу без будь- яких проблем.

На моєму комп' ютері Ubuntu я створив каталог / mmnt/imagecache, а потім запустив команду flowing, щоб зробити його придатним для запису (на всіх, я знаю, що це небезпечно, але я не гуру Linux:)

```shell
chmod  777 -p /mnt/imagecache
```

У моїй програмі. cs я маю такий рядок:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Оскільки типовим є коренева тека кешу, цей параметр буде записуватися до каталогу / mnt/ imagecache на комп' ютері вузла.