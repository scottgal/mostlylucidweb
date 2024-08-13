# استخدام صورة Sharp. Web مع ASP.net مصدر

<datetime class="hidden">2024-08-13TT 14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## أولاً

[صورة](https://docs.sixlabors.com/index.html)هي مكتبة معالجة قوية للصور تسمح لك بالتلاعب بالصور بطرق مختلفة. الصورة Sharp. Web هي امتداد للصورة Sharp الذي يوفر خاصية إضافية للعمل مع الصور في تطبيقات ASP. NET الأساسية. في هذا الخصوصي، سنستكشف كيفية استخدام الصورة Sharp. Web لإعادة تحجيم و محصول و تنسيق الصور في هذا التطبيق.

[رابعاً -

## صورة Sharp.Web

إلى بدء مع صورةWeb أنت إلى تثبيت التالي not get حزم:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## صورة شارب. Web الإعداد

في ملف برنامجنا نحن بعد ذلك ننشئ الصورة Sharp. Web. في حالتنا نحن نشير إلى و نخزن صورنا في مجلد يسمى "images" في wwwroot من مشروعنا. ثم ننشئ الصورة Sharp. Web الوسيطة لاستخدام هذا المجلد كمصدر لصورنا.

يستخدم أيضا مجلد 'cach' لتخزين الملفات المجهزة (وهذا يمنعها من إعادة النظر في الملفات كل مرة).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

هذه المجلّدات قريبة من الموقع wwwroot، لذا لدينا الهيكل التالي:

![](/cachefolder.png)

صورةSharp. Web لديها خيارات متعددة لـ حيث تخزن ملفاتك و Caching (انظر هنا لكل التفاصيل:[المصدر: https://docs.ssix6labors.com/article/imagesharp.wweb/imagecedrs.html?tabs=tabid-112Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

على سبيل المثال لخزن صورك في حاوية Azure plab (هاندي للتدرج) يمكنك استخدام مزود Azure مع AzureBlobCheas خيارات:

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

## الصورة: Sharp.wb

الآن بما أننا لدينا هذا الضبط، انه في الواقع سهل جداً لاستخدامه داخل تطبيقنا. على سبيل المثال اذا اردنا ان نخدم صورة إعادة تكبير[الـ مُرسِل مُرسِم](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper)أو تعيين الواجهة مباشرة.

المسند::

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

لاحظ أن مع هذا نحن إعادة رسم الصورة، تعيين العرض والارتفاع، وأيضا تعيين نموذج إعادة تحجيم وإعادة تلون الصورة.

في هذا التطبيق نذهب بالطريقة الابسط ونستخدم فقط معاملات الاستعلام. بالنسبة للعلامة السفلية نستخدم امتداداً يسمح لنا بتحديد حجم الصورة والتنسيق.

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

هذا يعطينا القابلية التشبيهية لأي من تحديد هذه في الوظائف مثل

```markdown
![image](/image.jpg?format=webp&quality=50)
```

حيث أن هذه الصورة سوف تأتي من`wwwroot/articleimages/image.jpg`ويعاد تشكيلها لتصبح 50% من الجودة وفي شكل ويبب.

او يمكننا ان نستخدم الصورة كما هي وسوف يتم اعادة تشكيلها و تنسيقها كما هو محدد في الاستعلام

## ثالثاً - استنتاج

كما رأيتم الصورة Sharp. ويب يعطينا قدرة كبيرة لإعادة تحجيم و تشكيل الصور في تطبيقات ASP.Net الأساسية. من السهل إنشاءها واستخدامها وتوفير الكثير من المرونة في كيفية التلاعب بالصور في تطبيقاتنا.