# Achtergrondvertalingen Pt. 1

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Inleiding

Dus voor een tijdje nu heb ik EasyNMT gebruikt om mijn `.md` bestanden in verschillende talen [Hier.](/blog/autotranslatingmarkdownfiles). Ik heb dit willen 'opduiken' zodat jullie er allemaal mee kunnen spelen.

De adelaar onder jullie zal gemerkt hebben dat ik een kleine daling heb toegevoegd aan de markdown editor.

![Dropdown](translatedropdown.png)

Dit is een lijst van talen die ik vertaal in (EasyNMT is een beetje een resource zwijn dus ik heb beperkt het aantal o talen die ik kan vertalen in).

[TOC]

## Hoe het werkt

Wanneer u een taal uit de dropdown selecteert en op een knop drukt, stuurt u een'vertaal' taak naar deze API:

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

Hij doet een paar dingen:

1. Het creëert een unieke identificatiecode voor de vertaaltaak
2. Het genereert een Cookie voor u om te leven op uw browser; Ik gebruik dit om te haak in de vertaaltaak later
3. Het activeert de vertaaltaak en slaat de bijbehorende taak op in een cache.
4. Het geeft de taak-ID terug aan de client

### The Cookie

De Cookie service is een eenvoudige uitbreiding op het HttpRequest object. Het controleert of er een cookie bestaat, als het geen nieuwe maakt. Dit wordt gebruikt om u en uw vertaaltaken te identificeren.

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

### De Cache-dienst

De cache service maakt gebruik van een eenvoudige in-memory cache om alle vertaaltaken voor een enkele gebruiker (u!) vast te houden. Je zult zien dat ik de cache na een uur heb laten verlopen. Dit is omdat ik deze taken niet te lang wil volhouden.

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

### De achtergronddienst

Ik bedek dit in het volgende deel, het is een beetje een beest.

De Commissie heeft echter de volgende maatregelen genomen: `Translate` methode maakt gebruik van TaskComplementationBron om ons de status van de vertaaltaak te laten volgen.

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

Zoals u kunt zien alles wat dit echt doet is het verzenden van een vertaling naar een `BufferBlock` (Ik zal in de toekomst naar Kanalen kijken, het kan een betere aanpak zijn!).
Het maakt ook gebruik van een `TaskCompletionSource` om de status van de vertaaltaak te volgen.

Binnen de verwerkingsdienst (die we later weer zullen behandelen) stellen we het resultaat van de `TaskCompletionSource` het resultaat van de vertaaltaak.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Door dit te doen kunnen we de status van de vertaaltaken die we in onze cache hebben opgeslagen 'poll' en feedback geven over de status van de vertaling. Dit kan enkele minuten duren, afhankelijk van het verkeer en de lengte van het markdown bestand.

### Vertalingen ophalen

Je hebt al gezien dat we een browser cookie voor je hebben ingesteld. Dit wordt gebruikt om u en uw vertaaltaken te identificeren. Nadat u een vertaling hebt ingediend gebruiken we HTMX polling om deze actie te raken die gewoon de vertalingen voor u retourneert.

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

### Ophalen van het resultaat

Zodra u een lijst van vertalingen en hun statussen hebt kunt u dit gebruiken om een vertaling te selecteren en deze in de Markdown-editor te laten verschijnen. Dit gebruikt dit API-eindpunt om de taak te krijgen;

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

## Conclusie

Het is nog vroeg toen ik dit bouwde, maar ik ben opgewonden om te zien waar het naartoe gaat. Ik zal de achtergronddienst in detail in het volgende deel behandelen.