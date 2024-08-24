# Traducciones de antecedentes Pt. 1

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Introducción

Así que por un tiempo he usado EasyNMT para traducir mi `.md` archivos en diferentes idiomas [aquí](/blog/autotranslatingmarkdownfiles). He querido'superar' esto para que todos puedan jugar con él también.

Ver la segunda parte de esta serie [aquí](/blog/backgroundtranslationspt2).

Los ojos de águila entre ustedes habrán notado que he añadido una pequeña lista al editor de marcos.

![Caída](translatedropdown.png)

Esta es una lista de idiomas a los que traduzco (EasyNMT es un poco de un recurso cerdo por lo que he limitado el número de idiomas o que puedo traducir a).

[TOC]

## Cómo funciona

Cuando seleccione un idioma desde el menú desplegable, y pulse un botón, enviará una tarea de 'traducción' a esta API:

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

This hace algunas cosas:

1. Crea un identificador único para la tarea de traducción
2. Genera una cookie para que usted viva en su navegador; Uso esto para enganchar en la tarea de traducción más adelante
3. Activa la tarea de traducción y almacena la tarea asociada en un caché.
4. Devuelve el ID de la tarea al cliente

### La galleta

El servicio Cookie es una simple extensión en el objeto HttpRequest. Comproba si existe una cookie, si no crea una nueva. Esto se utiliza para identificarle a usted y sus tareas de traducción.

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

### El Servicio de Caché

El servicio de caché utiliza una caché sencilla en memoria para mantener todas las tareas de traducción para un solo usuario (¡usted!). Verás que he puesto el caché para que expire después de una hora. Esto es porque no quiero aferrarme a estas tareas por mucho tiempo.

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

### El Servicio de Antecedentes

Voy a cubrir esto en la siguiente parte; es un poco de una bestia.

Sin embargo, la `Translate` método utiliza TaskCompletionSource para hacer un seguimiento del estado de la tarea de traducción.

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

Como se puede ver todo esto realmente hace es enviar una traducción a un `BufferBlock` (Voy a mirar los canales en el futuro, puede ser un mejor enfoque!).
También utiliza un `TaskCompletionSource` para seguir el estado de la tarea de traducción.

Dentro del servicio de procesamiento (que de nuevo cubriremos más adelante) establecemos el resultado de la `TaskCompletionSource` al resultado de la tarea de traducción.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Al hacer esto podemos 'enviar' el estado de las tareas de traducción que almacenamos en nuestra caché y dar retroalimentación del usuario en cuanto al estado de la traducción. Esto puede tomar varios minutos dependiendo del tráfico y la longitud del archivo Markdown.

### Obtener las traducciones

Ya has visto que establecemos una cookie de navegador para ti. Esto se utiliza para identificarle a usted y sus tareas de traducción. Después de enviar una traducción usaremos encuestas HTMX para golpear esta acción que simplemente devuelve las traducciones para usted.

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

### Obtención del resultado

Una vez que tenga una lista de traducciones y sus estados, puede utilizar esto para seleccionar una traducción y hacerla aparecer en el editor Markdown. Esto utiliza este punto final de la API para obtener la tarea;

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

## Conclusión

Aún en los primeros días mientras construyo esto, pero estoy emocionado de ver a dónde va. Cubriré el servicio de fondo en detalle en la siguiente parte.