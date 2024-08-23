# Traduzioni di sfondo Pt. 1

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Introduzione

Così per un po 'ora ho usato EasyNMT per tradurre il mio `.md` file in diverse lingue [qui](/blog/autotranslatingmarkdownfiles). Ho voluto'superare' questo in modo che tutti possano avere un gioco con esso troppo.

L'aquila guardata tra di voi avrà notato che ho aggiunto un po 'di goccia verso il basso per l'editor di markdown.

![A discesa](translatedropdown.png)

Questo è un elenco di lingue in cui traduco (EasyNMT è un po 'di un maiale risorsa quindi ho limitato il numero di lingue in cui posso tradurre).

[TOC]

## Come funziona

Quando si seleziona una lingua dal menu a discesa, e premere un pulsante si invia un'attività 'tradurre' a questa API:

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

Questo fa un paio di cose:

1. Crea un identificatore unico per l'attività di traduzione
2. Genera un Cookie per voi a vivere sul vostro browser; Io uso questo per agganciare l'attività di traduzione in seguito
3. Attiva l'attività di traduzione e memorizza l'attività associata in una cache.
4. Restituisce l'ID dell'attività al client

### Il cookie

Il servizio Cookie è una semplice estensione dell'oggetto HttpRequest. Controlla se esiste un cookie, se non ne crea uno nuovo. Questo viene utilizzato per identificare voi e le vostre attività di traduzione.

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

### Il servizio Cache

Il servizio cache utilizza una semplice cache in-memory per gestire tutte le attività di traduzione per un singolo utente (tu!). Vedrai che ho messo la cache a scadere dopo un'ora. Questo perché non voglio tenere questi compiti per troppo tempo.

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

### Il servizio di background

Lo copriro' nella prossima parte, e' un po' una bestia.

Tuttavia, `Translate` metodo utilizza TaskCompletionSource per farci monitorare lo stato dell'attività di traduzione.

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

Come si può vedere tutto questo fa davvero è inviare una traduzione a un `BufferBlock` (Vedrò Canali in futuro, potrebbe essere un approccio migliore!).
Esso utilizza anche un `TaskCompletionSource` per tracciare lo stato dell'attività di traduzione.

All'interno del servizio di elaborazione (che ancora una volta copriremo più tardi) abbiamo impostato il risultato del `TaskCompletionSource` al risultato del compito di traduzione.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

In questo modo possiamo 'poll' lo stato delle attività di traduzione che abbiamo memorizzato nella nostra cache e dare feedback degli utenti sullo stato della traduzione. Questo può richiedere diversi minuti a seconda del traffico e della lunghezza del file di markdown.

### Ottenere le traduzioni

Avete già visto che abbiamo impostato un cookie del browser per voi. Questo viene utilizzato per identificare voi e le vostre attività di traduzione. Dopo aver inviato una traduzione useremo il sondaggio HTMX per colpire questa azione che semplicemente restituisce le traduzioni per voi.

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

### Ottenere il risultato

Una volta che si dispone di un elenco di traduzioni e dei loro stati è possibile utilizzare questo per selezionare una traduzione e farlo apparire nell'editor Markdown. Questo utilizza questo endpoint API per ottenere l'attività;

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

## In conclusione

Sono ancora all'inizio, ma non vedo l'ora di vedere dove va a finire. Mi occuperò del servizio di background in dettaglio nella prossima parte.