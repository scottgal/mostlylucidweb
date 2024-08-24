# الـ خلفيات الشّرْع. 1

<datetime class="hidden">2024-08-23TT05: 38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## أولاً

لذا لفترة من الوقت الآن لقد استخدمت "ايسي نمت" لترجمة `.md` إلى لغات مختلفة [هنا هنا](/blog/autotranslatingmarkdownfiles)/ / / / لقد أردت أن "السطح" هذا حتى تتمكنوا جميعا من الحصول على اللعب معه أيضا.

انظر الجزء الثاني من هذا السلسلة [هنا هنا](/blog/backgroundtranslationspt2).

النسر المُعين بينكم سيكون قد لاحظتم أنني أضفت قطرة صغيرة إلى مُحرّر الهدف.

![الانخفاض (الانخفاض)](translatedropdown.png)

هذه قائمة من اللغات التي أترجمها (ياسي نمت هي نوع من خنزير مورد لذا قمت بتحديد عدد اللغات التي يمكنني ترجمتها).

[رابعاً -

## كيف يعمل

عند تحديد لغة من الأسفل، واضغط على زر سترسل مهمة 'ترجمة' إلى هذا API:

```csharp
[ApiController]
[Route("api/[controller]")]
public class TranslateController(
    BackgroundTranslateService backgroundTranslateService,
    TranslateCacheService translateCacheService) : ControllerBase
{
    [HttpPost("start-translation")]
    public async Task<IActionResult> StartTranslation([FromBody] MarkdownTranslationModel model)
    {
        // Create a unique identifier for this translation task
        var taskId = Guid.NewGuid().ToString("N");
        var userId = Request.GetUserId(Response);
        // Trigger translation and store the associated task
        var translationTask = await backgroundTranslateService.Translate(model);
    
        var translateTask = new TranslateTask(taskId, model.Language, translationTask);
        translateCacheService.AddTask(userId, translateTask);

        // Return the task ID to the client
        return Ok(new { TaskId = taskId });
    }
}
```

يقوم (ثيس) ببعض الأشياء:

1. إنشاء a فريد مُعرِف لـ مُشّر مهمة
2. يولّد كوكي لك لتعيش على متصفحك، أستعمل هذا للربط في مهمة الترجمة لاحقاً
3. ويشغل مهمة الترجمة ويخزن المهمة المرتبطة بها في مخبأ.
4. الدالة الدالة ترجع هوية المهمة إلى العميل

### الكعكة

خدمة (كوكي) هي امتداد بسيط على كائن Httpque. إنه يتأكد من وجود كعكة، إن لم تكن موجودة، فإنه يخلق كعكة جديدة. هذا مُستخدَم إلى تعرّف و.

```csharp
public static class UserIdHtlper
{
    public  static string GetUserId(this HttpRequest request, HttpResponse response)
    {
        var userId = request.Cookies["UserIdentifier"];
        if (userId == null)
        {
            userId = Guid.NewGuid().ToString();
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(60),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            response.Cookies.Append("UserIdentifier", userId, cookieOptions);
        }

        return userId;
    }
}
```

### الخزنة

تستخدم خدمة المخبأ مخبأ بسيطاً داخل الذاكرة لتحمّل جميع مهام الترجمة لمستعمل واحد (أنت! ). سترين أنّي وضعتُ المخبأ لينتهي بعد ساعة. هذا لأنني لا أريد التمسك بهذه المهام لفترة طويلة.

```csharp
public class TranslateCacheService(IMemoryCache memoryCache)
{
    public List<TranslateTask> GetTasks(string userId)
    {
        if (memoryCache.TryGetValue(userId, out List<TranslateTask>? task)) return task;

        return new List<TranslateTask>();
    }

    public void AddTask(string userId, TranslateTask task)
    {
        if (memoryCache.TryGetValue(userId, out List<TranslateTask>? tasks))
        {
            tasks ??= new List<TranslateTask>();
            tasks.Add(task);
            memoryCache.Set(userId, tasks, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }
        else
        {
            memoryCache.Set(userId, new List<TranslateTask> { task }, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }
    }
}

public record TranslateTask(string TaskId, string language, Task<TaskCompletion>? Task);
```

### دائرة المعلومات

سأغطي هذا في الجزء التالي، إنه نوع من الوحش.

- - - - - - - - - - - `Translate` تستخدم طريقة AMPLEULEULETION Source للسماح لنا بتتبع حالة مهمة الترجمة.

```csharp
    public async Task<Task<TaskCompletion>> Translate(MarkdownTranslationModel message)
    {
        // Create a TaskCompletionSource that will eventually hold the result of the translation
        var translateMessage = new PageTranslationModel
        {
            Language = message.Language,
            OriginalFileName = "",
            OriginalMarkdown = message.OriginalMarkdown,
            Persist = false
        };
        return await Translate(translateMessage);
    }

    private async Task<Task<TaskCompletion>> Translate(PageTranslationModel message)
    {
        // Create a TaskCompletionSource that will eventually hold the result of the translation
        var tcs = new TaskCompletionSource<TaskCompletion>();
        // Send the translation request along with the TaskCompletionSource to be processed
        await _translations.SendAsync((message, tcs));
        return tcs.Task;
    }
```

كما يمكنك أن ترى كل ما يفعله هذا حقاً هو إرسال ترجمة إلى `BufferBlock` (سأنظر إلى القنوات في المستقبل، قد يكون نهجاً أفضل))ع(
كما أنه يستخدم أيضاً `TaskCompletionSource` (ب) تتبع حالة مهمة الترجمة.

داخل خدمة المعالجة (التي سنغطيها لاحقاً مرة أخرى) `TaskCompletionSource` (بآلاف دولارات الولايات المتحدة)

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

من خلال القيام بذلك يمكننا أن ننشر حالة مهام الترجمة التي قمنا بتخزينها في مخبأنا ونعطي المستخدم تغذية مرتدة عن حالة الترجمة. وهذا يمكن أن يستغرق عدة دقائق اعتماداً على حركة المرور وطول ملف العلامة.

### الحصول على الترجمات

لقد رأيتِ مسبقاً أننا وضعنا لكِ بسكويتة متصفح هذا مُستخدَم إلى تعرّف و. بعد تقديم الترجمة سنستخدم إقتراع HTMX لضرب هذا الإجراء الذي ببساطة يعيد الترجمات لك.

```csharp
  [HttpGet]
    [Route("get-translations")]
    public IActionResult GetTranslations()
    {
        var userId = Request.GetUserId(Response);
        var tasks = translateCacheService.GetTasks(userId);
        var translations = tasks;
        return PartialView("_GetTranslations", translations);
    }
```

### الحصول على النتيجة

متى a قائمة من و استخدام إلى تحديد a ترجمة و الإيطالية بوصة محرّر. هذا استخدام API نقطة نهاية إلى get مهمة;

```csharp
    [HttpGet]
    [Route("get-translation/{taskId}")]
    public Results<JsonHttpResult<TaskCompletion>, BadRequest<string>> GetTranslation(string taskId)
    {
        var userId = Request.GetUserId(Response);
        var tasks = translateCacheService.GetTasks(userId);
        var translationTask = tasks.FirstOrDefault(t => t.TaskId == taskId);
        if(translationTask?.Task?.Status != System.Threading.Tasks.TaskStatus.RanToCompletion)
        {
            return TypedResults.BadRequest<string>("Task not completed");
        }
        return TypedResults.Json(translationTask.Task.Result);
    }
```

## في الإستنتاج

ما زال الوقت مبكراً وأنا أبني هذا لكن أنا متحمس لرؤية أين يذهب. أنا سَأَغطّي خدمةَ الخلفيةَ بالتفصيل في الجزءِ القادمِ.