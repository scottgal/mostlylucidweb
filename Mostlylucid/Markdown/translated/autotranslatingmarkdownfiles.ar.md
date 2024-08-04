# يجري تلقائياً ترجمة الملفات مع سهلNMT

## أولاً

هي خدمة قابلة للتثبيت محلياً والتي توفر واجهة بسيطة لعدد من خدمات الترجمة الآلية. في هذا الدرس، سوف نستخدم HeysNMT لترجمة ملف العلامة من الإنجليزية إلى اللغات المتعددة تلقائياً.

يمكنك العثور على كل الملفات لهذا التعليق في[مجلtHub محفوظ](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator)لهذا المشروع.

ملاحظة: لا يزال هذا الأمر صعباً جداً، وسأواصل صقله بينما أذهب.

لقد قمت فقط بترجمة هذا الملف في (الواقع و)[حولي حولي](/blog/aboutme)كما أقوم بصقل الطريقة، هناك عدد قليل من القضايا مع الترجمة التي أحتاج إلى حلها.

[رابعاً -

## النفقات قبل الاحتياجات

a تثبيت من سهلNMT هو مطلوب إلى متابعة هذا هو أنا عادة تشغيله كخدمة Docker. يمكنك العثور على تثبيت توجيهات[هنا هنا](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md)الذي يغطي كيفية تشغيله كخدمة متطفلة.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

أو إذا كان لديك منفذ نفيديا متاح:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

الدالة MAX_ Workers_ Bakend و MAX_ Workers_ FONETEND البيئة المتغيّرة تحدد عدد العمال الذين سيستخدمهم (EaseNMT). يمكنك تعديل هذه لتتناسب مع ماكينتك.

ملاحظة: iseNMT ليست خدمة SMOTH أفضل لتشغيلها، لكنها أفضل ما وجدت لهذا الغرض. انها قليلاً من الثرثرة حول إدخال سلسلة نص هو يمر، لذلك قد تحتاج إلى القيام ببعض المعالجة المسبقة من النص المدخل الخاص بك قبل تمريره إلى EaseNMT.

## النهج الناعِي إلى تحميل الموازنة

سهل NAMT هو وحش متعطش عندما يتعلق الأمر بالموارد، لذلك في جهازي للماركداون للتحرير لدي منتقى IP عشوائي جداً بسيط جداً والذي يأخذ الـ IP عشوائياً من قائمة الآلات لدي. هذا ساذج قليلاً ويمكن تحسينه باستخدام خوارزمية موازنة تحميل أكثر تطوراً.

```csharp
    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };

     var ip = IPs[random.Next(IPs.Length)];
     logger.LogInformation("Sendign request to {IP}", ip);
     var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

```

## جاري تحرير ملف

هذه هي الشفرة التي لديّ في ملفّ Markdown Translated Translated Service.cs. إنها خدمة بسيطة تأخذ علامة أسفل سلسلة نصية و لغة مستهدفة و ترجع سلسلة العلامات المترجمة.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 50;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

وكما ترون، فإنه يتضمن عددا من الخطوات:

1. `  var document = Markdig.Markdown.Parse(markdown);`- هذا يُبيّنُ العلامةَ أسفل السلسلةِ إلى a مستند.
2. `  var textStrings = ExtractTextStrings(document);`-هذا يستخرج نص السلاسل من المستند.
3. `  var batchSize = 50;`- هذا يحدد حجم الدفعة لخدمة الترجمة. seaseNMT له حد على عدد المحارف التي يمكن ترجمتها في ذهاب واحد.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   وهذا يشير إلى الطريقة التي تقوم بعد ذلك بوضع الدفعة على خدمة " إيز ناتم ".

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());`- هذا يعيد تشغيل الخيوط المترجمة إلى المستند. باستخدام قدرة مارك ديغ على السير في المستند واستبدال النصوص.

## نوع نوع الخدمة الخدمـ الخدمـ الخدمـ الخدمـ الخدمـ الخدمـ الخدمة

إلى تشغيل كل هذا أنا استخدام IHOSTD حياة خدمة وقت والتي بدأت في ملف البرنامج.cs. هذه الخدمة تقرأ في ملف علامة أسفل ، تترجمه إلى عدد من اللغات ، وتكتب الملفات المترجمة إلى قرص.

```csharp
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "jap", "uk", "zh" };
        foreach (var language in languages)
        {
            foreach (var file in files)
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, cancellationToken);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            }
        }

        logger.LogInformation("Background translation service started");
    }
```

كما ترون فهو يفحص أيضاً هامش الملف ليرى ما إذا كان قد تغير قبل ترجمته. هذا لتجنب ترجمة الملفات التي لم تتغير.

يتم ذلك عن طريق حساب مشط سريع من ملف العلامة التنازلية الأصلي ثم اختبار لمعرفة ما إذا كان هذا الملف قد تغير قبل محاولة ترجمته.

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

الإعداد في البرنامج.cs بسيط جداً:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

لقد أنشأت الخدمة المُضيفة (الخدمة المُترجمة الخلفية) والمُحددة HttpClient لدائرة المُترجمين المُعلّمين.
الخدمة المستضيفة هي خدمة طويلة الأمد تعمل في الخلفية. إنها مكان جيد لوضع الخدمات التي تحتاج إلى تشغيل مستمر في الخلفية أو مجرد أخذ فترة للاستكمال. الواجهة الجديدة لخدمة خدمة وقت العمل المُخصصة هي أكثر مرونة قليلاً من واجهة الخدمة المُخصصة القديمة و تسمح لنا بإدارة المهام بالكامل في الخلفية بسهولة أكبر من الخدمة القديمة المُقدّمة.

يمكنك أن ترى هنا أني أعد الوقت لـ HttpClient إلى 15 دقيقة. هذا لأن سهل NMT يمكن أن يكون بطيئاً قليلاً في الاستجابة (خاصة أول مرة باستخدام نموذج لغة). وأنا أيضاً أضع عنوان الأساس لعنوان IP للآلة التي تدير خدمة seysNMT.

(أ) مـا مـن مـن مـن مـن مـن مـن مـن مـن مـن مـن