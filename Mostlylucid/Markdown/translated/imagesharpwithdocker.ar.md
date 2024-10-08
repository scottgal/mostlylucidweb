# صورة جاري مع مع تنفيذ

<datetime class="hidden">2024-08-00/</datetime>

<!--category-- Docker, ImageSharp -->
الصورة هي مكتبة كبيرة للعمل مع الصور في NET. إنه سريع، سهل الاستخدام، ولديه الكثير من الميزات. في هذا المنصب، سأريكم كيف تستخدمون الصورة Sharp مع Docker لإنشاء خدمة معالجة بسيطة للصور.

## ما هي الصورة؟

يُمْكِنُني الصورةُ مِنْ العمل بسلاسة مَع الصورِ في.NET. إنها مكتبة متقاطعة التي تدعم مجموعة واسعة من أشكال الصور وتوفر API بسيطة لمعالجة الصور. إنها سريعة، وكفؤة، وسهلة الاستخدام.

على أية حال هناك مشكلة في تركيبي بإستخدام (دوكر) و (صور شارب) عند محاولة تحميل صورة من ملف، أحصل على ما يلي:
'الدخول ممنوع إلى المسار / urut/ cack/ eth... '
والسبب في ذلك هو تركيبات Doker ASP.net التي لا تسمح بكتابة الوصول إلى دليل المخبأ الذي تستخدمه صورة Sharp لتخزين الملفات المؤقتة.

## الـ /   /            

الحل هو رفع حجم في الحاوية الدوكر يشير إلى دليل على الجهاز المضيف. بهذه الطريقة، يمكن لمكتبة الصورة Sharp أن تكتب إلى دليل المخبأ بدون أيّة مسائل.

وهنا كيفية القيام بذلك:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

هنا أنا خريطة ملفّ إلى a محلي دليل يعمل مضيف آلة. بهذه الطريقة، يمكن لصورة Chemsharp أن تكتب إلى دليل المخبأ بدون أيّ إصدارات.

على آلتي "أوبونتو" قمت بإنشاء دليل / mnt/ imagecache ومن ثم شغلت الأمر المتملق لجعله قابلاً للكتابة (من قبل أي شخص، أعلم أن هذا ليس آمناً

```shell
chmod  777 -p /mnt/imagecache
```

في برنامجي، لدي هذا الخط:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

كمخبأ افتراضي إلى هذا الآن كتابة إلى دليل يعمل مضيف ماكينة.