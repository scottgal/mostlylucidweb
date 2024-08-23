# Taustaa Käännökset Pt. 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1

<datetime class="hidden">2024-08-23T05:38</datetime>

<!--category-- EasyNMT, ASP.NET -->
## Johdanto

Joten jo jonkin aikaa olen käyttänyt EasyNMT kääntää minun `.md` tiedostot eri kielille [täällä](/blog/autotranslatingmarkdownfiles)...................................................................................................................................... Olen halunnut "pinnata" tämän, jotta te kaikki voitte leikkiä sen kanssa.

Kotka on varmasti huomannut, että olen lisännyt pienen tipan alas maaliviiva-editorille.

![Pudotus](translatedropdown.png)

Tämä on luettelo kielistä, joita käännän (EasyNMT on vähän resurssihomma, joten olen rajoittanut kielimäärää, johon voin kääntää).

[TÄYTÄNTÖÖNPANO

## Miten se toimii?

Kun valitset kielen pudotuksesta ja painat painiketta, lähetät "käännä"-tehtävän tähän API-rajapintaan:

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

THis tekee muutamia asioita:

1. Se luo yksilöllisen tunnisteen käännöstehtävään
2. Se luo evästeen, jonka avulla voit elää selaimellasi. Käytän tätä kytkeäkseni sen myöhemmin käännöstehtävään.
3. Se käynnistää käännöstehtävän ja tallentaa siihen liittyvän tehtävän välimuistiin.
4. Se palauttaa tehtävätunnuksen asiakkaalle

### Keksi

Cookie-palvelu on yksinkertainen laajennus HttpRequest-objektiin. Se tarkistaa, onko eväste olemassa, jos se ei luo uutta. Tätä käytetään tunnistamaan sinut ja käännöstehtäväsi.

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

### Häkkipalvelu

Välimuistipalvelu käyttää yksinkertaista muistinsisäistä välimuistia pitääkseen kaikki käännöstehtävät yhdelle käyttäjälle (sinä!). Huomaat, että olen asettanut välimuistin vanhenemaan tunnin kuluttua. Tämä johtuu siitä, että en halua pitää kiinni näistä tehtävistä liian pitkään.

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

### Taustapalvelu

Hoidan tämän seuraavassa osassa. Se on aikamoinen peto.

Euroopan parlamentin ja neuvoston asetus (EU) N:o 952/2013, annettu 17 päivänä kesäkuuta 2013, Euroopan yhteisön perustamissopimuksen 93 artiklan soveltamista koskevista yksityiskohtaisista säännöistä (EUVL L 347, 20.12.2013, s. 1). `Translate` menetelmä käyttää TaskCompletionSourceä, jotta voimme seurata käännöstehtävän tilaa.

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

Kuten näet, kaikki tämä todella tekee on lähettää käännös `BufferBlock` (Katson kanavia tulevaisuudessa, se voi olla parempi lähestymistapa!).
Se käyttää myös `TaskCompletionSource` Kääntämistehtävän tilan seuraaminen.

Jalostuspalvelussa (joka taas kattaa myöhemmin) määritämme lopputuloksen `TaskCompletionSource` käännöstehtävän tulokseen.

```csharp
   var translatedMarkdown =
                await markdownTranslatorService.TranslateMarkdown(translateModel.OriginalMarkdown,
                    translateModel.Language, cancellationToken);
            tcs.SetResult(new TaskCompletion(translatedMarkdown, translateModel.Language, true, DateTime.Now));
        }
```

Näin voimme "pollata" välimuistiimme tallennettujen käännöstehtävien tilan ja antaa käyttäjäpalautetta käännöksen tilasta. Tähän voi kulua useita minuutteja liikenteestä ja markdown-tiedoston pituudesta riippuen.

### Käännösten saaminen

Olet jo nähnyt, että asetimme sinulle selainevästeen. Tätä käytetään tunnistamaan sinut ja käännöstehtäväsi. Käännöksen jälkeen käytämme HTMX-äänestystä osuaksemme tähän toimintoon, joka yksinkertaisesti palauttaa käännökset sinulle.

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

### Tulosten saaminen

Kun sinulla on luettelo käännöksistä ja niiden statuksista, voit käyttää tätä valitaksesi käännöksen ja antaa sen näkyä Markdown-muokkaimessa. Tässä käytetään tätä API-päätettä tehtävän saamiseksi.

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

## Johtopäätöksenä

Vielä alkuaikoina, kun rakennan tätä, mutta olen innoissani, että näen, mihin se johtaa. Käsittelen taustapalvelua yksityiskohtaisesti seuraavassa osassa.