# Автоматично перекладати файли з розміткою за допомогою EasyNMT

<datetime class="hidden">2024- 08- 03T13: 30</datetime>

<!--category-- EasyNMT, Markdown -->
## Вступ

EasyNMT - це служба локального встановлення, яка надає простий інтерфейс багатьом службам перекладу машин. У цьому підручнику ми скористаємося EasyNMT, щоб автоматично перекласти файл Markdown з англійської на декілька мов.

Ви можете знайти всі файли для цього підручника у [Сховище GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) для цього проекту.

Результатом цього було створення БУНК для нових файлів у мовах призначення. Це дуже простий спосіб перекласти допис блогу на декілька мов.

[Перекладені дописи](/translatedposts.png)

[TOC]

## Передумови

Для того, щоб слідувати за цим підручником, потрібно встановити EasyNMT. Я обычно устраиваю это как сериал Докера. Ви можете знайти настанови зі встановлення [тут](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) що охоплює як керувати ним як сервісом.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

АБО, якщо ви маєте доступ до NVIDIA GPU:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

За допомогою змінних середовища MAX_ WORKERS_ BACKEND і MAX_ WORKERS_ FRONTEND ви можете встановити кількість робітників, які використовуватимуться EasyNMT. Можеш підлаштувати це, щоб підлаштувати свою машину.

ФАЙНМТ - це не найкрутіша служба для запуску, але це найкраще, що я знайшов для цієї мети. Це трохи дивно щодо вхідного рядка, який він передав, отже, можливо, вам доведеться спочатку обробляти ваш вхідний текст, перш ніж передавати його до EasyNMT.

А також він переклав слово "концепція" на якусь нісенітницю про надсилання пропозиції до ЄС, що підштовхує його до тренінгу.

## Наївний приступ, щоб завантажити дисбаланс

Простий NMT - це жадібний звір, коли справа доходить до ресурсів, тому в моєму MarkdownTranslatorService I have super simple random random  IP chooser that just turned through the list of IPs of a tasks to run FasyNMT.

Спочатку це робить на `model_name` метод служби EasyNMT, це швидкий і простий спосіб перевірити, чи не працює ця служба. Якщо це так, то додає IP до списку робочих IP. Якщо це не так, то воно не додає його до списку.

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

Потім в середині `Post` методTag Type `MarkdownTranslatorService` Ми повертаємо робочі IP для пошуку наступного.

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

Це дуже простий спосіб завантажити балансування запитів на багатьох машинах. Вона не ідеальна (це не відповідає на надробочу машину для ексампеля), але це досить добре для моїх цілей.

Шмік ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` Просто повертає список IP починаючи з 0 і переходить до довжини списку.

## Переклад файла з розміткою

Це код, який я маю у файлі MarkdownTranslatorService.cs. Це проста служба, яка бере рядок з позначкою, мову призначення і повертає перекладений рядок.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

Як ви бачите, він має декілька кроків:

1. `  var document = Markdig.Markdown.Parse(markdown);` - За допомогою цього пункту можна розплутати рядок зі значком у документі.
2. `  var textStrings = ExtractTextStrings(document);` - За допомогою цього пункту можна видобути текстові рядки з документа.
   Цей метод використовується

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Цей параметр перевіряє чи " слово " дійсно є роботою; назви зображень можуть розбивати функціональні можливості розбиття речень на FasyNMT.

3. `  var batchSize = 10;` - Це встановлює пакетний розмір для перекладацької служби. EasyNMT має обмеження на кількість слів, які він може перекласти одним ходом (близько 500, отже, 10 рядків, загалом, є непоганим пакетним розміром).
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Це викликає метод, який відправляє пакет до служби EasyNMT.

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Це повертає перекладені рядки назад у документ. За допомогою програми MarkDig можна пересуватися документом і замінювати текстові рядки.

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## Служба з вузлами

Щоб запустити все це, я використовую IHosed LifetimeService, який запускається у файлі Program.cs. Ця служба читає у файлі markdown, перекладає її на декілька мов і записує перекладені файли на диск.

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

Як ви можете бачити, програма також перевіряє хеш файла, щоб перевірити чи його було змінено перед перекладом. Це для того, щоб уникнути перекладу файлів, які не змінилися.

Це робиться під час обчислення швидкого хешу початкового файла markdown, потім перевіряєте, чи змінився цей файл перед спробою перекласти його.

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

Налаштування програми. cs досить просте:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

Я заснував HardedService (BackgroundTranslateService) і HtpClient для MarkdoutTranslatorServicee.
Служба- вузол - це служба з довгими можливостями, яка виконується на задньому плані. Це гарне місце для того, щоб забезпечити послуги, які повинні працювати безперервно на задньому плані, або просто зайняти трохи часу, щоб завершити. Новий інтерфейс IHosed LifetimeService є трохи гнучкішим, ніж старий інтерфейс IHosedService і дозволяє нам виконувати завдання повністю на задньому плані набагато легше, ніж старіший IHosedService.

Тут ви бачите, що я встановлюю час очікування на HtpClient до 15 хвилин. Це тому, що EasyNMT може бути трохи повільним до відповіді (особливо вперше використовуючи модель мови). Я також встановлюю базову адресу IP-адресу машини, що працює на службі EasyNMT.

## Включення

Це досить простий спосіб перекласти файл з позначенням на декілька мов. Це не ідеально, але це хороший початок. Зазвичай, я запускаю цю програму для кожного допису блогу і вона використовується в `MarkdownBlogService` щоб показати назви для кожного допису блогу.