# يجري تلقائياً ترجمة الملفات مع سهلNMT

<datetime class="hidden">2024-08-03-TT 13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## أولاً

وهي خدمة قابلة للتثبيت محليا توفر وصلة بينية بسيطة لعدد من خدمات الترجمة الآلية. في هذا الدرس، سنستخدم "ايزي انميت" لترجمة ملف العلامة تلقائياً من الانجليزية إلى لغات متعددة.

يمكنك العثور على كل الملفات لهذا التعليق في [مجلtHub محفوظ](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) لهذا المشروع.

ونتجت عن ذلك مجموعة جديدة من الملفات الرمزية باللغات المستهدفة. هذه طريقة بسيطة جداً للحصول على نص مدوّن مُترجم إلى لغات متعدّدة.

[الوظائف المعاد توزيعها](/translatedposts.png)

[رابعاً -

## النفقات قبل الاحتياجات

ويلزم تركيب نظام " أيسي نميت " لمتابعة هذا البرنامج. أديره عادة كخدمة لـ(دوكر) يمكنك العثور على أمر تثبيت [هنا هنا](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) الذي يغطي كيفية تشغيله كخدمة متطفلة.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

أو إذا كان لديك منفذ نفيديا متاح:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

والمتغيرات البيئية الرئيسية تحدد عدد العمال الذين سيستخدمهم "ايزينامت". يمكنك تعديل هذه لتتناسب مع آلتك.

MENMT ليست خدمة motothest لتشغيل، ولكن هذا هو أفضل لقد وجدت لهذا الغرض. هو a قليلاً حول دَخْل سلسلة نص هو مرّ، لذا أنت قَدْ تَحتاجُ إلى أَنْ تَعمَلُ بَعْض المعالجة المسبقة لنصّ دَخْلِكَ قَبْلَ أَنْ تَرْفعَه إلى eeyNMT.

و ترجمت أيضاً "الاتفاق" إلى بعض الهراء حول تقديم الاقتراح إلى الاتحاد الأوروبي...

## النهج الناعِي إلى تحميل الموازنة

سهل NAMT هو وحش متعطش عندما يتعلق الأمر بالموارد، لذلك في بلدي علامة التأشيرة التحريرية خدمة لدي منتقى IP عشوائي جدا بسيط جدا الذي يدور فقط من خلال قائمة من IPs من قائمة الآلات التي أستخدمها لتشغيل eeasNMT.

هذا يعني الحصول على `model_name` هذه طريقة سريعة وبسيطة للتحقق إذا كانت الخدمة جاهزة. إذا كان كذلك، فهو يضيف الـ IP إلى قائمة من IPs. إذا لم يكن كذلك، فإنه لا يضيفه إلى القائمة.

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

ثم داخل `Post` عـد `MarkdownTranslatorService` ندور من خلال IPs العامل للعثور على التالي.

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

هذه طريقة بسيطة جداً لتحميل التوازن بين الطلبات عبر عدد من الآلات. إنه ليس مثالياً (إنه لا يُحسب لآلة مُزدحمة جداً للإختبار) لكنه جيد بما فيه الكفاية لأغراضي.

الـ ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` يتناوب فقط خلال قائمة IPs التي تبدأ بـ 0 وتذهب إلى طول القائمة.

## جاري تحرير ملف

هذا هو الرمز الذي لديّ في ملفّ (ماركداون) للتحرير. إنّها خدمة بسيطة تأخذ خيطاً هدفياً ولغة مستهدفة وتعود بالخط المترجم.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
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

1. `  var document = Markdig.Markdown.Parse(markdown);` - هذا يُبيّنُ العلامةَ أسفل السلسلةِ إلى a مستند.
2. `  var textStrings = ExtractTextStrings(document);` -هذا يستخرج نص السلاسل من المستند.
   هذا يستخدم هذه الطريقة

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

هذا تفقّد IF كلمة هو a عمل صورة الاسم إلى تفسّد جملة تجزئة الدالة بوصة eeyNMT.

3. `  var batchSize = 10;` - هذا يحدد حجم الدفعة لدائرة الترجمة. لدى (EaseNMT) حد لعدد الكلمات التي يمكن ترجمتها في جولة واحدة (حوالي 500، لذا فإن 10 سطر هي عموماً دفعة جيدة الحجم هنا).
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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - هذا يعيد تشغيل الأوتار المترجمة إلى المستند. استخدام قدرة علامة Dig على المشي على المستند واستبدال نص السلاسل.

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## نوع نوع الخدمة الخدمـ الخدمـ الخدمـ الخدمـ الخدمـ الخدمـ الخدمة

لكي أدير كل هذا أستخدم خدمة حياتية مُقدسة والتي بدأت في ملف البرنامج.cs. تُقرأ هذه الخدمة في ملف رمزي، وتترجمه إلى عدد من اللغات وتكتب الملفات المترجمة إلى قرص.

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

كما ترون فهو يفحص أيضاً مشط الملف ليرى ما إذا كان قد تغير قبل ترجمته. هذا لتجنب ترجمة الملفات التي لم تتغير.

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
});
```

لقد أنشأت الخدمة المُضيفة (الخدمة المُترجمة الخلفية) والمُحددة HttpClient لدائرة المُترجمين المُعلّمين.
الخدمة المستضيفة هي خدمة طويلة الأمد تعمل في الخلفية. هو مكان جيد لوضع الخدمات التي تحتاج إلى تشغيل باستمرار في الخلفية أو مجرد أخذ بعض الوقت لاستكمال. الواجهة الجديدة لخدمة خدمة وقت الحياة هي أكثر مرونة بعض الشيء من واجهة الخدمة القديمة القديمة ودعونا تشغيل المهام تماما في الخلفية بسهولة أكبر من الخدمة القديمة القديمة IhostedService.

هنا يمكنكم أن تروا أني أضع وقت الإستراحة لـ HttpClient إلى 15 دقيقة. والسبب في ذلك هو أن HeynMT يمكن أن تكون بطيئة بعض الشيء في الاستجابة (وبخاصة المرة الأولى باستخدام نموذج اللغة). أنا أيضاً أضع عنوان القاعدة إلى عنوان IP للآلة التي تدير خدمة "ايزي ناتم"

## في الإستنتاج

هذه طريقة بسيطة جداً لترجمة ملف إلى عدة لغات. إنها ليست مثالية لكنها بداية جيدة أدير هذا عادةً لكل تدوينة جديدة وهي مستخدمة في الـ `MarkdownBlogService` لسحب الأسماء المترجمة لكل تدوينة.