# Background Translations Pt. 1

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Εισαγωγή

Έτσι, εδώ και λίγο καιρό χρησιμοποιώ το EasyNMT για να μεταφράσω το δικό μου `.md` αρχεία σε διαφορετικές γλώσσες [Ορίστε.](/blog/autotranslatingmarkdownfiles). Ήθελα να το "επιστρέψω" για να παίξετε κι εσείς.

Δείτε το δεύτερο μέρος αυτής της σειράς [Ορίστε.](/blog/backgroundtranslationspt2).

Ο αετός που κοιτάει ανάμεσά σας θα έχει παρατηρήσει ότι έχω προσθέσει μια μικρή πτώση κάτω στον εκδότη Markdown.

![Πτώση](translatedropdown.png)

Αυτή είναι μια λίστα των γλωσσών στις οποίες μεταφράζω (EasyNMT είναι ένα κομμάτι ενός γουρούνι πόρων, έτσι έχω περιορίσει τον αριθμό o γλώσσες μπορώ να μεταφράσει σε).

[TOC]

## Πώς λειτουργεί

Όταν επιλέξετε μια γλώσσα από το dropdown, και πατήσετε ένα κουμπί θα στείλετε ένα 'translate' έργο σε αυτό το API:

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

Του κάνει μερικά πράγματα:

1. Δημιουργεί ένα μοναδικό αναγνωριστικό για το έργο μετάφρασης
2. Δημιουργεί ένα Cookie για να ζείτε στο πρόγραμμα περιήγησής σας; Θα χρησιμοποιήσω αυτό για να συνδέσετε το έργο μετάφραση αργότερα
3. Ενεργοποιεί το έργο μετάφρασης και αποθηκεύει το σχετικό έργο σε μια κρύπτη.
4. Επιστρέφει την ταυτότητα εργασίας στον πελάτη

### Το Cookie

Η υπηρεσία Cookie είναι μια απλή επέκταση στο αντικείμενο HttpRequest. Ελέγχει αν υπάρχει ένα μπισκότο, αν δεν δημιουργεί ένα νέο. Αυτό χρησιμοποιείται για να προσδιορίσει εσάς και τις μεταφραστικές σας εργασίες.

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

### Η Υπηρεσία Κρύπτης

Η υπηρεσία cache χρησιμοποιεί ένα απλό cache in-memory για να κρατήσει όλες τις μεταφραστικές εργασίες για έναν μόνο χρήστη (σας!). Θα δεις ότι έβαλα την κρύπτη να λήξει μετά από μια ώρα. Αυτό συμβαίνει επειδή δεν θέλω να κρατήσω αυτές τις εργασίες για πολύ καιρό.

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

### Η Υπηρεσία Υποβάθρου

Θα το καλύψω αυτό στο επόμενο μέρος, είναι λίγο θηρίο.

Ωστόσο, η `Translate` μέθοδος χρησιμοποιεί TaskCleaseΠηγή για να μας αφήσει να παρακολουθείτε την κατάσταση του έργου μετάφραση.

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

Όπως μπορείτε να δείτε όλα αυτά πραγματικά κάνει είναι να στείλετε μια μετάφραση σε ένα `BufferBlock` (Θα κοιτάξω τα κανάλια στο μέλλον, μπορεί να είναι μια καλύτερη προσέγγιση!).
Χρησιμοpiοιεί εpiίση ένα `TaskCompletionSource` να παρακολουθεί την κατάσταση του μεταφραστικού έργου.

Εντός της υπηρεσίας επεξεργασίας (την οποία και πάλι θα καλύψουμε αργότερα) βάλαμε το αποτέλεσμα της `TaskCompletionSource` στο αποτέλεσμα του μεταφραστικού έργου.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Κάνοντάς το αυτό μπορούμε να "πολεμήσουμε" την κατάσταση των μεταφραστικών εργασιών που αποθηκεύσαμε στην κρύπτη μας και να δώσουμε στα σχόλια των χρηστών σχετικά με την κατάσταση της μετάφρασης. Αυτό μπορεί να διαρκέσει αρκετά λεπτά ανάλογα με την κίνηση και το μήκος του αρχείου markdown.

### Λήψη των μεταφράσεων

Έχεις ήδη δει ότι βάλαμε ένα μπισκότο περιήγησης για σένα. Αυτό χρησιμοποιείται για να προσδιορίσει εσάς και τις μεταφραστικές σας εργασίες. Αφού υποβάλετε μια μετάφραση θα χρησιμοποιήσουμε τη δημοσκόπηση HTMX για να χτυπήσουμε αυτή τη Δράση, η οποία απλά επιστρέφει τις μεταφράσεις για εσάς.

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

### Παίρνοντας το αποτέλεσμα

Μόλις έχετε μια λίστα των μεταφράσεων και των καταστάσεων τους μπορείτε να χρησιμοποιήσετε αυτό για να επιλέξετε μια μετάφραση και να το εμφανιστεί στον επεξεργαστή Markdown. Αυτό χρησιμοποιεί αυτό το τελικό σημείο API για να πάρει την εργασία?

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

## Συμπέρασμα

Νωρίς το χτίζω, αλλά είμαι ενθουσιασμένος που θα δω που θα πάει. Θα καλύψω την υπηρεσία υποβάθρου λεπτομερώς στο επόμενο μέρος.