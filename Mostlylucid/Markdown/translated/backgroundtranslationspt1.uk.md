# Тло перекладів Pt. 1

<datetime class="hidden">2024-08- 23T05: 38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Вступ

Якийсь час я користувався FreeNMT для перекладу мого `.md` файли на різних мовах [тут](/blog/autotranslatingmarkdownfiles). Я хотел бы "повесить" это, чтобы вы тоже могли поиграть.

Орелок серед вас помітив, що я додав маленьку краплину до редактора з відміткою.

![Спадне](translatedropdown.png)

Це список мов, якими я перекладаю (EasyNMT - це трохи відлуння ресурсів, тому я обмежив кількість мов o, на які я можу перекладати).

[TOC]

## Як це працює

Після вибору мови зі спадного списку і натискання кнопки ви надішлете завдання " переклад " до цього API:

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

ТВ робить кілька речей:

1. Він створює унікальний ідентифікатор для завдання з перекладу@ info: tooltip
2. Це створює куку для вас, щоб жити на вашому браузері. Я використовую її для виконання завдання з перекладу.
3. За допомогою цього пункту можна виконати завдання з перекладу і зберегти пов' язане з завданням завдання у кеші.
4. Він повертає ідентифікатор завдання клієнтові

### Кука

Служба кук є простим розширенням об' єкта HtpRequest. Перевіряє, чи існує печиво, чи ні, створює нову. Цей пункт використовується для ідентифікації вас і ваших перекладацьких завдань.

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

### Служба кешу

Служба кешу використовує простий кеш пам' яті для зберігання всіх завдань з перекладу для одного користувача (ви!). Ви побачите, що я встановив кеш, щоб закінчити його через годину. Це тому, що я не хочу занадто довго виконувати ці завдання.

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

### Служба тла

Я покрию це в наступній частині; це трохи звіра.

Проте `Translate` Метод використовує TestompletionSource, щоб відстежити статус завдання з перекладу.

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

Як ви бачите, все, що це дійсно робить, це відправити переклад до `BufferBlock` (Подивлюся в майбутньому на Канали, можливо, це кращий підхід!).
Вона також використовує a `TaskCompletionSource` стежити за станом завдання з перекладу.

В рамках обробки (який знову буде охоплювати пізніше) ми встановили результат `TaskCompletionSource` у результат завдання перекладу.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Роблячи це, ми можемо "пропускати" стан завдань перекладу, які ми зберігали у нашому кеші, і надавати користувачам інформацію про стан перекладу. Це може тривати декілька хвилин, залежно від навантаження та тривалості файла позначки.

### Переклад

Ви вже бачили, що ми встановили для вас печиво браузера. Цей пункт використовується для ідентифікації вас і ваших перекладацьких завдань. Після того, як ви додасте переклад, ми використаємо HTMX опитування для того, щоб отримати доступ до цієї дії, яка просто поверне вам переклади.

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

### Отримання результатуThe role of the transaction, in present tense

Після того, як у вас буде список перекладів та їх станів, ви можете скористатися цим пунктом, щоб обрати переклад і показати його у редакторі Поміток. Це використання цієї кінцевої точки API для отримання завдання;

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

## Включення

Ще рано, коли я будую це, але я в захваті від того, куди воно йде. Я детально опишу фонову службу в наступній частині.