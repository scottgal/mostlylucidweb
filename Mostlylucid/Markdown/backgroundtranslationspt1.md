# Background Translations Pt. 1

<datetime class="hidden">2024-08-23T05:38</datetime>
<!--category-- EasyNMT, ASP.NET -->

## Introduction
So for a while now I've used EasyNMT to translate my `.md` files into different languages [here](/blog/autotranslatingmarkdownfiles). I've wanted to 'surface' this so you can all have a play with it too. 

The eagle eyed amongst you will have noticed that I've added a little drop down to the markdown editor.

![Dropdown](translatedropdown.png)

This is a list of languages which I translate into (EasyNMT is a bit of a resource hog so I've limited the number o languages I can translate into).

[TOC]

## How it works
When you select a language from the dropdown, and hit a button you'll send a 'translate' task to this API:

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
THis does a few things:
1. It creates a unique identifier for the translation task
2. It generates a Cookie for you to live on your browser; I use this to hook into the translation task later
3. It triggers the translation task and stores the associated task in a cache.
4. It returns the task ID to the client

### The Cookie
The Cookie service is a simple extension on the HttpRequest object. It checks if a cookie exists, if it doesn't it creates a new one. This is used to identify you and your translation tasks.

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

### The Cache Service
The cache service uses a simple in-memory cache to hold all the translation tasks for a single user (you!). You'll see that I've set the cache to expire after an hour. This is because I don't want to hold onto these tasks for too long.

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

### The Background Service
I'll cover this in the next part; it's a bit of a beast.

However the `Translate` method uses TaskCompletionSource to let us track the status of the translation task.

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

As you can see all this really does is send a translation to a `BufferBlock` (I'll look at Channels in future, it may be a better approach!). 
It also uses a `TaskCompletionSource` to track the status of the translation task.

Within the processing service (which again we'll cover later) we set the result of the `TaskCompletionSource` to the result of the translation task.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```
By doing this we can 'poll' the status of the translation tasks we stored in our cache and give user feedback as to the status of the translation. This can take several minutes depending on traffic and the length of the markdown file.

### Getting the translations
You've already seen that we set a browser cookie for you. This is used to identify you and your translation tasks. After you submit a translation we'll use HTMX polling to hit this Action which simply returns the translations for you.

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

### Getting the result
Once you have a list of translations and their statuses you can use this to select a translation and have it appear in the Markdown editor. This uses this API endpoint to get the task;

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

## In Conclusion
Still early days as I build this out but I'm excited to see where it goes. I'll cover the background service in detail in the next part.