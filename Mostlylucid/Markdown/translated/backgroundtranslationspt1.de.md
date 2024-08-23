# Hintergrundübersetzungen Pt. 1.............................................................................................................................................................................................................................................................. 

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Einleitung

So für eine Weile jetzt habe ich EasyNMT verwendet, um meine zu übersetzen `.md` Dateien in verschiedenen Sprachen [Hierher](/blog/autotranslatingmarkdownfiles)......................................................................................................... Ich wollte dies 'aufdecken', damit ihr alle ein Spiel damit machen könnt.

Der Adler unter Ihnen wird bemerkt haben, dass ich dem Markdown-Editor einen kleinen Tropfen hinzugefügt habe.

![Dropdown](translatedropdown.png)

Dies ist eine Liste von Sprachen, die ich übersetzen in (EasyNMT ist ein bisschen von einer Ressource Schwein, so dass ich die Anzahl der Sprachen, die ich übersetzen kann begrenzt).

[TOC]

## Wie es funktioniert

Wenn Sie eine Sprache aus dem Dropdown auswählen und eine Schaltfläche drücken, senden Sie eine 'übersetzen' Aufgabe an diese API:

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

THIS macht ein paar Dinge:

1. Es erstellt eine eindeutige Kennung für die Übersetzungsaufgabe
2. Es erzeugt ein Cookie für Sie, um in Ihrem Browser zu leben; Ich benutze dies, um in der Übersetzungsaufgabe später Haken
3. Es löst die Übersetzungsaufgabe aus und speichert die zugehörige Aufgabe in einem Cache.
4. Es gibt die Task-ID an den Client zurück

### Das Cookie

Der Cookie-Dienst ist eine einfache Erweiterung des HttpRequest-Objekts. Es prüft, ob ein Cookie existiert, wenn es nicht ein neues erstellt. Dies dient dazu, Sie und Ihre Übersetzungsaufgaben zu identifizieren.

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

### Der Cache-Dienst

Der Cache-Dienst verwendet einen einfachen In-Memory-Cache, um alle Übersetzungsaufgaben für einen einzelnen Benutzer (Sie!) zu halten. Du wirst sehen, dass ich den Cache nach einer Stunde ablaufe. Das liegt daran, dass ich diese Aufgaben nicht zu lange einhalten will.

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

### Der Hintergrunddienst

Ich decke das im nächsten Teil, es ist ein bisschen eine Bestie.

Die `Translate` Methode verwendet TaskCompletionSource, um den Status der Übersetzungsaufgabe nachzuverfolgen.

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

Wie Sie sehen können, alles, was dies wirklich tut, ist eine Übersetzung an eine `BufferBlock` (Ich werde auf Channels in Zukunft schauen, es könnte ein besserer Ansatz sein!)== Einzelnachweise ==
Es verwendet auch eine `TaskCompletionSource` um den Status der Übersetzungsaufgabe zu verfolgen.

Innerhalb des Verarbeitungsservice (der wir später wieder abdecken werden) setzen wir das Ergebnis der `TaskCompletionSource` zum Ergebnis der Übersetzungsaufgabe.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Dadurch können wir den Status der von uns in unserem Cache gespeicherten Übersetzungsaufgaben 'pollen' und Nutzer-Feedback über den Status der Übersetzung geben. Dies kann je nach Verkehr und Länge der Markdown-Datei mehrere Minuten dauern.

### Die Übersetzungen erhalten

Sie haben bereits gesehen, dass wir einen Browser-Cookie für Sie gesetzt haben. Dies dient dazu, Sie und Ihre Übersetzungsaufgaben zu identifizieren. Nachdem Sie eine Übersetzung eingereicht haben, verwenden wir HTMX polling, um diese Aktion zu treffen, die einfach die Übersetzungen für Sie zurückgibt.

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

### Erhalten des Ergebnisses

Sobald Sie eine Liste von Übersetzungen und deren Status haben, können Sie diese verwenden, um eine Übersetzung auszuwählen und sie im Markdown-Editor erscheinen zu lassen. Dies nutzt diesen API-Endpunkt, um die Aufgabe zu erhalten;

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

## Schlussfolgerung

Noch früh, als ich dies herausbaue, aber ich freue mich, zu sehen, wohin es geht. Ich kümmere mich im nächsten Teil ausführlich um den Hintergrunddienst.