# Automatisches Übersetzen von Markdown-Dateien mit EasyNMT

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Einleitung

EasyNMT ist ein lokal installierbarer Service, der eine einfache Schnittstelle zu einer Reihe von maschinellen Übersetzungsdiensten bietet. In diesem Tutorial werden wir EasyNMT verwenden, um eine Markdown-Datei automatisch von Englisch in mehrere Sprachen zu übersetzen.

Sie finden alle Dateien für dieses Tutorial in der [GitHub-Repository](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) für dieses Projekt.

Die Ausgabe dieser erzeugte eine BUNCH von neuen Markdown-Dateien in den Zielsprachen. Dies ist ein super einfacher Weg, um einen Blog-Post in mehrere Sprachen übersetzt zu bekommen.

[Übersetzte Beiträge](/translatedposts.png)

[TOC]

## Voraussetzungen

Um diesem Tutorial zu folgen, ist eine Installation von EasyNMT erforderlich. Normalerweise leite ich es als Docker-Service. Die Installationsanleitung finden Sie [Hierher](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) die wie man es als Docker-Service laufen lässt.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

ODER wenn Sie eine NVIDIA GPU zur Verfügung haben:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Die Umgebungsvariablen MAX_WORKERS_BACKEND und MAX_WORKERS_FRONTEND setzen die Anzahl der Mitarbeiter, die EasyNMT verwenden wird. Sie können diese an Ihre Maschine anpassen.

HINWEIS: EasyNMT ist nicht der SMOOTHEST Service, aber es ist das Beste, was ich für diesen Zweck gefunden habe. Es ist ein bisschen persnickety über die Eingabe Zeichenkette, die es übergeben wird, so dass Sie möglicherweise einige Vorverarbeitung Ihres Eingabetextes tun müssen, bevor Sie es an EasyNMT übergeben.

Oh und es übersetzte auch "Schlussfolgerung" zu irgendeinem Unsinn über die Einreichung des Vorschlags bei der EU...Vertrauen es ist Training-Set.

## Naive Ansatz zur Belastung Balancing

Easy NMT ist ein Dursttier, wenn es um Ressourcen geht, so in meinem MarkdownTranslatorService habe ich einen super einfachen zufälligen IP-Selektor, der gerade durch die Liste der IPs einer Liste von Maschinen rotiert, die ich benutze, um EasyNMT auszuführen.

Zunächst macht dies einen Schritt auf der `model_name` Methode auf dem EasyNMT-Service, dies ist eine schnelle, einfache Möglichkeit, zu überprüfen, ob der Service ist up. Wenn ja, fügt es die IP zu einer Liste der funktionierenden IPs hinzu. Wenn nicht, fügt es es nicht zur Liste hinzu.

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

Dann innerhalb der `Post` Verfahren der `MarkdownTranslatorService` wir drehen durch die funktionierenden IPs, um die nächste zu finden.

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

Dies ist eine super einfache Möglichkeit, die Anforderungen über eine Reihe von Maschinen hinweg auszugleichen. Es ist nicht perfekt (es ist nicht für eine super beschäftigte Maschine für exampel), aber es ist gut genug für meine Zwecke.

Der Dreckskerl ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` dreht sich einfach durch die Liste der IPs ab 0 und geht auf die Länge der Liste.

## Übersetzen einer Markdown-Datei

Das ist der Code, den ich in der Datei MarkdownTranslatorService.cs habe. Es ist ein einfacher Dienst, der einen Markdown-String und eine Zielsprache benötigt und den übersetzten Markdown-String zurückgibt.

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

Wie Sie sehen können, hat es eine Reihe von Schritten:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Hierdurch wird der Markdown-String in ein Dokument eingeblendet.
2. `  var textStrings = ExtractTextStrings(document);` - Das extrahiert die Textstrings aus dem Dokument.
   Dabei wird die Methode verwendet.

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Dies prüft, ob das 'Wort' wirklich ein Werk ist; Bildnamen können die Satzspaltungsfunktionalität in EasyNMT durcheinander bringen.

3. `  var batchSize = 10;` - Hiermit wird die Batchgröße für den Übersetzungsdienst festgelegt. EasyNMT hat ein Limit für die Anzahl der Wörter, die es in einem Zug übersetzen kann (ca. 500, so dass 10 Zeilen ist in der Regel eine gute Batch-Größe hier).
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Dies ruft die Methode auf, die dann die Charge an den EasyNMT-Dienst postet.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Damit werden die übersetzten Zeichenfolgen wieder in das Dokument eingefügt. Mit MarkDigs Fähigkeit, das Dokument zu gehen und Textstrings zu ersetzen.

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

## Gehosteter Dienst

Um all dies auszuführen, benutze ich einen IHostedLifetimeService, der in der Datei Program.cs gestartet wird. Dieser Dienst liest sich in einer Markdown-Datei, übersetzt sie in eine Reihe von Sprachen und schreibt die übersetzten Dateien auf die Festplatte.

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

Wie Sie sehen können, überprüft es auch den Hash der Datei, um zu sehen, ob er sich vor der Übersetzung geändert hat. Um zu vermeiden, dass Dateien übersetzt werden, die sich nicht geändert haben.

Dies geschieht, indem man einen schnellen Hash der ursprünglichen Markdown-Datei berechnet, um dann zu prüfen, ob sich diese Datei geändert hat, bevor man versucht, sie zu übersetzen.

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

Das Setup in Program.cs ist ziemlich einfach:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

Ich habe den HostedService (BackgroundTranslateService) und den HttpClient für den MarkdownTranslatorService eingerichtet.
Ein Hosted Service ist ein langfristiger Dienst, der im Hintergrund läuft. Es ist ein guter Ort, um Dienstleistungen, die kontinuierlich im Hintergrund laufen müssen oder nur eine Weile dauern, um abzuschließen. Die neue IHostedLifetimeService-Schnittstelle ist etwas flexibler als die alte IHostedService-Schnittstelle und lässt uns Aufgaben ganz im Hintergrund leichter ausführen als der ältere IHostedService.

Hier sehen Sie, dass ich den Timeout für den HttpClient auf 15 Minuten feststelle. Dies liegt daran, dass EasyNMT ein wenig langsam reagieren kann (besonders das erste Mal mit einem Sprachmodell). Außerdem setze ich die Basisadresse auf die IP-Adresse der Maschine, die den EasyNMT-Service betreibt.

## Schlussfolgerung

Dies ist ein ziemlich einfacher Weg, um eine Markdown-Datei in mehrere Sprachen zu übersetzen. Es ist nicht perfekt, aber es ist ein guter Anfang. Ich laufe dies in der Regel für jeden neuen Blog-Post und es wird in der `MarkdownBlogService` um die übersetzten Namen für jeden Blog-Post zu ziehen.