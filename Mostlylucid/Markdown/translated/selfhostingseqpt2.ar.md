# Sex for ASP.net progrging - التعقب مع Serilog Tring Seriques

<datetime class="hidden">2424-2024-08-31-31T 11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# أولاً

في الجزء السابق سأريكم كيف تنشئون [الاستضافة الذاتية لـ Sex باستخدام ASP.net ](/blog/selfhostingseq)/ / / / الآن بعد أن قمنا بتأسيسه، حان الوقت لاستخدام المزيد من خصائصه للسماح بالمزيد من قطع الأشجار والتعقب الكاملين باستخدام نموذجنا التربيعي الجديد.

[رابعاً -

# الملتليج

التعقب مثل قطع الأشجار + + + يعطيك طبقة إضافية من المعلومات حول ما يحدث في تطبيقك. إنه مفيد بشكل خاص عندما يكون لديك نظام توزيع و تحتاج لتتبع الطلب من خلال خدمات متعددة.
في هذا الموقع أستخدمه لتعقب المشاكل بسرعة، فقط لأن هذا موقع هواية لا يعني أنني أتخلى عن معاييري المهنية.

## الإعدادات المُعَدّة

إعداد التعقب مع Serilog هو حقاً سهل جداً باستخدام [SERIL التعقب](https://github.com/serilog-tracing/serilog-tracing) (د) حزمة من الحزمة. أولاً تحتاج إلى تثبيت الحزم:

ونضيف هنا أيضاً المغسلة الكونسولية والمغسلة التعاقبية

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

كونسول مفيد دائماً لإزالة التأثير و Seq هو ما نحن هنا من أجله. Seq أيضاً يحتوي على مجموعة من 'enrichers' التي يمكن أن تضيف معلومات إضافية إلى سجلاتك.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

لتستخدم هذه المثريات التي تحتاج إلى إضافتها إلى `Serilog` في تشكيل `appsettings.json` ملف ملفّيّاً. تحتاج أيضاً إلى تركيب جميع الإثراءات المجزأة باستخدام النوتات.

إنها واحدة من الأشياء الجيدة والسيئة في سيريلوج، ينتهي بك الأمر بتركيب مجموعة كبيرة من الحزم، لكن هذا يعني أنك فقط تضيف ما تحتاجه وليس حزمة أحادية واحدة فقط.
هذا هو منجم

![المزميرات](serilogenrichers.png)

مع كل هذه القنابل حصلت على مخرجات جيدة جداً في Seq.

![](serilogerror.png)

هنا ترى رسالة الخطأ، ومستند الرّمة، وهوية الخيط، وهوية العملية، واسم العملية. هذه كلها معلومات مفيدة عندما تحاول تعقب قضية ما.

شيء واحد لملاحظته هو أنني وضعت `  "MinimumLevel": "Warning",` في `appsettings.json` ملف ملفّيّاً. وهذا يعني أن التحذيرات وما فوقها فقط هي التي ستُنقل إلى Seq. هذا مفيد للحفاظ على الضجيج أسفل في سجلاتك.

في Sq يمكنك ايضاً تحديد هذا على Api Kay، اذاً يمكنك ان يكون `Information` (أو إذا كنت متحمساً حقاً) `Debug`(أ) قطع الأشجار المحدد هنا والحد مما يلتقطه المعامل المكافئ فعلياً بواسطة مفتاح API.

![مربع مفتاح](apikey.png)

ملاحظة: لا يزال لديك تطبيق فوقي، يمكنك أيضا جعل هذا أكثر ديناميكية بحيث يمكنك تعديل المستوى على الذبابة. - - - - - - - - - - - - - - - [المرفس ](https://github.com/datalust/serilog-sinks-seq)لمزيد من التفاصيل.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## الملتليج

الآن نضيف التعقب، مرة أخرى باستخدام Serilog trapex انه بسيط جدا. لدينا نفس التركيبة كما من قبل لكننا نضيف حوض جديد للتعقب

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

ونضيف أيضاً حزمة إضافية لسجل معلومات أساسية أكثر تفصيلاً على الإنترنت.

### تثبيت في `Program.cs`

الآن يمكننا أن نبدأ في الواقع باستخدام التعقب. أولاً يجب أن نضيف التعقب إلى `Program.cs` ملف ملفّيّاً.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

ويستخدم التعقب مفهوم "الأنشطة" الذي يمثل وحدة عمل. يمكنك البدء بنشاط، والقيام ببعض العمل ثم وقفه. وهذا مفيد لتتبع الطلب من خلال خدمات متعددة.

في هذه الحالة نضيف المزيد من التعقب لطلبات HttpClient وطلبات AspNetCore. نضيف أيضاً `TraceToSharedLogger` الذي سَيَحْسبُ النشاطَ إلى نفس اللوغارتم كباقي تطبيقِنا.

## استخدام الشعار في إحدى الخدمات

الآن لدينا نظام التعقب يمكننا البدء باستخدامه في تطبيقنا. هذا مثال على الخدمة التي تستخدم التعقب

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

وفيما يلي الخطوط الرئيسية الهامة في هذا الصدد:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

هذا يبدأ "نشاط" جديد وهو وحدة عمل. إنه مفيد لتعقب الطلب من خلال خدمات متعددة
كما لو أنها ملفوفة في بيان مستخدم هذا سيكتمل ويتخلص في نهاية طريقتنا لكنها ممارسة جيدة لإكمالها بشكل واضح.

```csharp
            activity.Complete();
```

في حالتنا الإستثنائية للتعامل مع الصيد نحن أيضاً نكمل النشاط لكن مع مستوى الخطأ والاستثناء. هذا مفيد لتعقب القضايا في طلبك.

## &:

الآن لدينا كل هذا الضبط الذي يمكننا البدء باستخدامه. هذا مثال على أثر في طلبي

![Http](httptrace.png)

هذا يُظهر لك ترجمة لنقطة واحدة. يمكنك أن ترى الخطوات المتعددة لـ a واحد عمود و الكل HttpClient الطلبات والتوقيتات.

ملاحظة إستعمل Postgres لقاعدة بياناتي، على عكس SQL خادم npgsql سوّاق دعم لتعقب لذا أنت يمكن أن تحصل على بيانات مفيدة جدا من قاعدة بياناتك إستفسارات مثل تنفيذ SQL، التوقيتات الخ. يتم حفظ هذه على أنها'spans' إلى Seq وانظر اللغز التالي:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

يمكنك أن ترى أن هذا يتضمن إلى حد كبير كل ما تحتاج لمعرفته عن الإقتراح، الـ SQL نفّذ، سلسلة الاتصال الخ. هذه كلها معلومات مفيدة عندما تحاول تعقب قضية ما. في تطبيق أصغر مثل هذا هذا هو فقط مثير للاهتمام، في التطبيق الموزّع، هو معلومات ذهبية صلبة لتتبع القضايا.

# في الإستنتاج

لقد خدشت فقط سطح التعقب هنا، انها منطقة صغيرة مع محامين عاطفيين. على أمل أن أكون قد أظهرت مدى بساطة الذهاب مع التعقب البسيط باستخدام Seque & Serilog لتطبيقات ASP.NET الأساسية. بهذه الطريقة يمكنني الحصول على الكثير من فوائد الأدوات الأكثر قوة مثل تطبيقات بصائر بدون تكلفة أزوري (هذه الأشياء يمكن أن تُنفق عندما تكون اللوغارتمات كبيرة).