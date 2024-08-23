# Bakgrundsöversättningar Pt. Denna förordning träder i kraft den tjugonde dagen efter det att den har offentliggjorts i Europeiska unionens officiella tidning.

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Inledning

Så för ett tag nu har jag använt EasyNMT för att översätta min `.md` filer till olika språk [här](/blog/autotranslatingmarkdownfiles)....................................... Jag har velat "täcka" detta så att ni alla kan ha en pjäs med det också.

Örnen bland er har märkt att jag har lagt till en liten droppe ner till markdown redaktören.

![Dropdown (nedsläppning)](translatedropdown.png)

Detta är en lista över språk som jag översätter till (EasyNMT är lite av en resurs svin så jag har begränsat antalet o språk jag kan översätta till).

[TOC]

## Hur det fungerar

När du väljer ett språk från rullgardinen, och trycker på en knapp skickar du en "översätt" uppgift till detta API:

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

Det här gör några saker:

1. Det skapar en unik identifierare för översättningsuppgiften
2. Det genererar en cookie för dig att leva på din webbläsare; Jag använder detta för att koppla in översättningsuppgiften senare
3. Det utlöser översättningsuppgiften och lagrar den tillhörande uppgiften i en cache.
4. Det returnerar uppgiften ID till klienten

### Cookien

Cookie-tjänsten är en enkel förlängning på HttpRequest-objektet. Den kontrollerar om en cookie finns, om den inte gör det skapar den en ny. Detta används för att identifiera dig och dina översättningsuppgifter.

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

### Kackerlackans tjänst

cache tjänsten använder en enkel in-minne cache för att hålla alla översättningsuppgifter för en enda användare (du!). Du får se att jag har satt skatten att löpa ut efter en timme. Det är för att jag inte vill hålla fast vid de här uppgifterna för länge.

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

### Bakgrundstjänsten

Jag täcker det här i nästa del, det är lite av ett odjur.

Emellertid gäller följande: `Translate` metod använder TaskCompletionSource för att låta oss spåra status för översättningsuppgiften.

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

Som du kan se allt detta verkligen gör är att skicka en översättning till en `BufferBlock` (Jag ska titta på Kanalerna i framtiden, det kan vara en bättre strategi!)..............................................................................................
Den använder också en `TaskCompletionSource` för att spåra översättningsuppgiftens status.

Inom bearbetningstjänsten (som vi återigen kommer att täcka senare) sätter vi resultatet av `TaskCompletionSource` till resultatet av översättningsuppgiften.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Genom att göra detta kan vi "spola" statusen för de översättningsuppgifter vi lagrat i vår cache och ge användaren feedback om status för översättningen. Detta kan ta flera minuter beroende på trafiken och längden på markdown-filen.

### Hämta översättningarna

Du har redan sett att vi ställer in en webbläsare cookie för dig. Detta används för att identifiera dig och dina översättningsuppgifter. Efter att du lämnat in en översättning kommer vi att använda HTMX röstning för att slå denna åtgärd som helt enkelt returnerar översättningar för dig.

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

### Att få resultat

När du har en lista över översättningar och deras status kan du använda detta för att välja en översättning och få den att visas i Markdown editor. Detta använder denna API-slutpunkt för att få uppgiften;

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

## Slutsatser

Fortfarande tidiga dagar när jag bygger ut detta men jag är glad att se vart det går. Jag täcker bakgrundstjänsten i detalj i nästa del.