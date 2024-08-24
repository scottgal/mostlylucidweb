# पृष्ठभूमि अनुवाद Pt. 1

<datetime class="hidden">2024- 0. 232323:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## परिचय

तो कुछ समय के लिए मैं मेरे अनुवाद के लिए आसानNMT इस्तेमाल किया है `.md` फ़ाइल को भिन्न भाषा [यहाँ](/blog/autotranslatingmarkdownfiles)___ मैं 'राइट' यह करने के लिए चाहता था तो आप सब के साथ भी एक खेल कर सकते हैं.

इस श्रृंखला का दूसरा भाग देखिए [यहाँ](/blog/backgroundtranslationspt2).

आप के बीच उकाब आंख देखा होगा कि मैं एक छोटे से नीचे निशान संपादक को नीचे गिर गया है.

![छोडें](translatedropdown.png)

यह उन भाषाओं की सूची है जिनका मैं अनुवाद करता हूँ (जी.एन.एम.टी) में अनुवाद करता हूँ ताकि मैं संख्या ओओ भाषाओं में अनुवाद कर सकूँ.

[विषय

## यह कैसे काम करता है

जब आप ड्रॉप डाउन से एक भाषा चुनते हैं, और एक बटन को हिट करें जो आप इस एपीआई में 'ट्यूमिन' कार्य करेंगे:

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

वह कुछ करता है.

1. यह अनुवाद कार्य के लिए एक अद्वितीय पहचान बनाता है
2. यह आप के लिए एक कुकी बनाता है अपने ब्राउज़र पर रहने के लिए; मैं अनुवाद कार्य बाद में हुक करने के लिए इस का उपयोग
3. यह अनुवाद कार्य को स्थापित करता है और कैश में सम्बद्ध कार्य को जमा करता है ।
4. यह क्लाइंट के लिए कार्य आईडी लौटाता है

### कुकी

कुकी सेवा एक सरल विस्तार वस्तु पर है. यह जांच करता है कि एक कुकी मौजूद है, अगर यह एक नया जन्म नहीं करता. यह आपकी और आपके अनुवाद कार्य की पहचान करने के लिए प्रयुक्त है.

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

### कैश सेवा

कैश सेवा एक सरल उपयोक्ता के लिए सभी अनुवाद कार्यों को पकड़े रखने के लिए उपयोग में आता है (आप!) आप देखेंगे कि मैंने कैश को एक घंटे के बाद तय किया है। यह है क्योंकि मैं बहुत लंबे समय के लिए इन कार्यों पर पकड़ नहीं करना चाहती.

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

### पृष्ठभूमि सेवा

मैं अगले भाग में यह कवर होगा, यह एक जानवर का एक सा है.

लेकिन `Translate` विधि अनुवाद कार्य की स्थिति पर पुनर्विचार करने के लिए कार्य को को को कॉफ़न स्रोत प्रयोग करती है ।

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

जैसा कि आप देख सकते हैं यह सब वास्तव में एक अनुवाद भेज रहा है `BufferBlock` (मैं भविष्य में चैनल पर देखो, यह एक बेहतर तरीका हो सकता है!___
यह भी एक प्रयोग करता है `TaskCompletionSource` अनुवाद कार्य की स्थिति को ट्रैक करने के लिए.

प्रक्रिया सेवा के भीतर (जो फिर हम बाद में कवर करेंगे) हमने इसके परिणाम को स्थापित किया `TaskCompletionSource` अनुवाद के काम की वजह से ।

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

ऐसा करने से हम अनुवाद के काम की स्थिति 'पोज़' कर सकते हैं जिसे हम अपने कैश में जमा कर रहे हैं और उपयोगकर्ता फ़ीडबैक को अनुवाद की स्थिति के रूप में दे सकते हैं. यह ट्रैफिक के आधार पर कई मिनट और निशान नीचे दिए गए फ़ाइल की लम्बाई के आधार पर कई मिनट ले सकता है.

### अनुवाद करना

आप पहले से ही देखा है कि हम आप के लिए एक ब्राउज़र कुकी सेट है। यह आपकी और आपके अनुवाद कार्य की पहचान करने के लिए प्रयुक्त है. जब आप एक अनुवाद जमा करने के बाद हम HMMATTE का उपयोग इस क्रिया को चलाने के लिए करेंगे जो केवल आप के लिए अनुवाद लौटाता है.

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

### परिणाम प्राप्त करना

एक बार जब आपके पास अनुवाद की सूची और उनकी स्थिति आप इसका उपयोग किसी अनुवाद को चुनने के लिए कर सकते हैं और इसे मार्क्ड संपादक में प्रकट कर सकते हैं. यह कार्य को पाने के लिए एपीआई अंत बिन्दुओं का उपयोग करता है;

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

## ऑन्टियम

मैं यह बाहर निर्माण के रूप में अभी भी प्रारंभिक दिनों के रूप में...... लेकिन मैं देखने के लिए उत्साहित हूँ कि यह कहाँ चला जाता है. मैं अगले भाग में विस्तार में पृष्ठभूमि सेवा कवर करेंगे.