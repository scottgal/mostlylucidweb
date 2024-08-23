# Contexte Traductions Pt. 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Présentation

Donc, depuis un moment maintenant, j'ai utilisé EasyNMT pour traduire mon `.md` fichiers dans différentes langues [Ici.](/blog/autotranslatingmarkdownfiles)C'est ce que j'ai dit. J'ai voulu "surfacer" ça pour que vous puissiez tous jouer avec.

L'aigle qui a vu parmi vous aura remarqué que j'ai ajouté un peu de goutte vers le bas à l'éditeur de balisage.

![Décrochage](translatedropdown.png)

Il s'agit d'une liste de langues dans laquelle je traduit (EasyNMT est un peu une ressource, donc j'ai limité le nombre de langues dans lesquelles je peux traduire).

[TOC]

## Comment ça marche

Lorsque vous sélectionnez une langue dans le menu déroulant, et appuyez sur un bouton, vous allez envoyer une tâche de « traduire » à cette API :

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

This fait quelques choses :

1. Il crée un identifiant unique pour la tâche de traduction
2. Il génère un Cookie pour vous de vivre sur votre navigateur; J'utilise ceci pour brancher dans la tâche de traduction plus tard
3. Il déclenche la tâche de traduction et stocke la tâche associée dans un cache.
4. Il retourne l'ID de la tâche au client

### Le cookie

Le service Cookie est une simple extension sur l'objet HttpRequest. Il vérifie si un cookie existe, s'il n'en crée pas un nouveau. Ceci est utilisé pour vous identifier ainsi que vos tâches de traduction.

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

### Le service Cache

Le service cache utilise un cache en mémoire simple pour tenir toutes les tâches de traduction pour un seul utilisateur (vous!). Vous verrez que j'ai réglé le cache pour expirer après une heure. C'est parce que je ne veux pas tenir ces tâches trop longtemps.

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

### Le Service d'information générale

Je vais couvrir ça dans la partie suivante ; c'est un peu une bête.

Toutefois, `Translate` méthode utilise TaskCompletionSource pour nous permettre de suivre l'état de la tâche de traduction.

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

Comme vous pouvez le voir, tout ce que cela fait vraiment est d'envoyer une traduction à un `BufferBlock` (Je regarderai Channels à l'avenir, ce sera peut-être une meilleure approche!).............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................
Il utilise également une `TaskCompletionSource` pour suivre l'état de la tâche de traduction.

Au sein du service de traitement (que nous couvrirons à nouveau plus tard), nous avons défini le résultat de la `TaskCompletionSource` au résultat de la tâche de traduction.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

En faisant cela, nous pouvons 'pouler' l'état des tâches de traduction que nous avons stockées dans notre cache et donner aux utilisateurs une rétroaction sur l'état de la traduction. Cela peut prendre plusieurs minutes en fonction du trafic et de la longueur du fichier de balisage.

### Obtenir les traductions

Vous avez déjà vu que nous avons défini un cookie de navigateur pour vous. Ceci est utilisé pour vous identifier ainsi que vos tâches de traduction. Après avoir soumis une traduction, nous utiliserons le sondage HTMX pour frapper cette action qui vous renvoie simplement les traductions.

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

### Obtenir le résultat

Une fois que vous avez une liste de traductions et leurs statuts, vous pouvez l'utiliser pour sélectionner une traduction et la faire apparaître dans l'éditeur Markdown. Ceci utilise ce paramètre d'API pour obtenir la tâche;

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

## En conclusion

Encore les premiers jours pendant que je construis ça, mais je suis excité de voir où ça va. Je couvrirai le service d'arrière-plan en détail dans la prochaine partie.