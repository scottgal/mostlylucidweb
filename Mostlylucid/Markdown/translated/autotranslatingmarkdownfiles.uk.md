# Автоматично перекладати файли з розміткою за допомогою EasyNMT

<datetime class="hidden">2024- 08- 03T13: 30</datetime>

<!--category-- ASP.NET, Markdown -->
## Вступ

EasyNMT - це служба локального встановлення, яка надає простий інтерфейс багатьом службам перекладу машин. У цьому підручнику ми скористаємося EasyNMT, щоб автоматично перекласти файл Markdown з англійської на декілька мов.

Ви можете знайти всі файли для цього підручника у [Сховище GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) для цього проекту.

ЩО МОЖНА ЗРОБИТИ. Це все ще досить важко, я продовжу удосконалювати його.

Я переклав лише цей файл [про мене](/blog/aboutme) я уточнюю цей метод; є декілька питань з перекладом, який мені треба зробити.

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

## Наївний приступ, щоб завантажити дисбаланс

Простий NMT - це жадібний звір, коли справа доходить до ресурсів, тому в моєму MarkdownTranslatorService я маю супер простий вибір IP, який просто бере випадковий IP зі списку машин, які я маю. Це трохи наївно і можна покращити, використовуючи більш складний алгоритм балансування вантажу.

```csharp
    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };

     var ip = IPs[random.Next(IPs.Length)];
     logger.LogInformation("Sendign request to {IP}", ip);
     var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

```

## Переклад файла з розміткою

Це код, який я маю у файлі MarkdownTranslatorService.cs. Це проста служба, яка бере рядок з позначкою, мову призначення і повертає перекладений рядок.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 50;
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
3. `  var batchSize = 50;` - Це встановлює пакетний розмір для перекладацької служби. EasyNMT має обмеження на кількість символів, які він може перекладати за один хід.
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

## Служба з вузлами

Щоб запустити все це, я використовую IHosed LifetimeService, який запускається у файлі Program.cs. Ця служба читає у файлі markdown, перекладає її на декілька мов і записує перекладені файли на диск.

```csharp
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "jap", "uk", "zh" };
        foreach (var language in languages)
        {
            foreach (var file in files)
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, cancellationToken);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            }
        }

        logger.LogInformation("Background translation service started");
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
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

Я заснував HardedService (BackgroundTranslateService) і HtpClient для MarkdoutTranslatorServicee.
Служба- вузол - це служба з довгими можливостями, яка виконується на задньому плані. Це гарне місце для того, щоб забезпечити послуги, які повинні працювати безперервно на задньому плані, або просто зайняти трохи часу, щоб завершити. Новий інтерфейс IHosed LifetimeService є трохи гнучкішим, ніж старий інтерфейс IHosedService і дозволяє нам виконувати завдання повністю на задньому плані набагато легше, ніж старіший IHosedService.

Тут ви бачите, що я встановлюю час очікування на HtpClient до 15 хвилин. Це тому, що EasyNMT може бути трохи повільним до відповіді (особливо вперше використовуючи модель мови). Я також встановлюю базову адресу IP-адресу машини, що працює на службі EasyNMT.

I