# Automatisches Übersetzen von Markdown-Dateien mit EasyNMT

## Einleitung

EasyNMT ist ein lokal installierbarer Service, der eine einfache Schnittstelle zu einer Reihe von maschinellen Übersetzungsdiensten bietet. In diesem Tutorial werden wir EasyNMT verwenden, um eine Markdown-Datei automatisch von Englisch in mehrere Sprachen zu übersetzen.

Sie finden alle Dateien für dieses Tutorial in der [GitHub-Repository](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) für dieses Projekt.

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

## Übersetzen einer Markdown-Datei

Das ist der Code, den ich in der Datei MarkdownTranslatorService.cs habe. Es ist ein einfacher Dienst, der einen Markdown-String und eine Zielsprache benötigt und den übersetzten Markdown-String zurückgibt.

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

Wie Sie sehen können, hat es eine Reihe von Schritten:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Hierdurch wird der Markdown-String in ein Dokument eingeblendet.
2. `  var textStrings = ExtractTextStrings(document);` - Das extrahiert die Textstrings aus dem Dokument.
3. `  var batchSize = 50;` - Hiermit wird die Batchgröße für den Übersetzungsdienst festgelegt. EasyNMT hat ein Limit für die Anzahl der Zeichen, die es in einem Zug übersetzen kann.
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

## Gehosteter Dienst

Um all dies auszuführen, benutze ich einen IHostedLifetimeService, der in der Datei Program.cs gestartet wird. Dieser Dienst liest sich in einer Markdown-Datei, übersetzt sie in eine Reihe von Sprachen und schreibt die übersetzten Dateien auf die Festplatte.

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
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

Ich habe den HostedService (BackgroundTranslateService) und den HttpClient für den MarkdownTranslatorService eingerichtet.
Ein Hosted Service ist ein langfristiger Dienst, der im Hintergrund läuft. Es ist ein guter Ort, um Dienstleistungen, die kontinuierlich im Hintergrund laufen müssen oder nur eine Weile dauern, um abzuschließen. Die neue IHostedLifetimeService-Schnittstelle ist etwas flexibler als die alte IHostedService-Schnittstelle und lässt uns Aufgaben ganz im Hintergrund leichter ausführen als der ältere IHostedService.

Hier sehen Sie, dass ich den Timeout für den HttpClient auf 15 Minuten feststelle. Dies liegt daran, dass EasyNMT ein wenig langsam reagieren kann (besonders das erste Mal mit einem Sprachmodell). Außerdem setze ich die Basisadresse auf die IP-Adresse der Maschine, die den EasyNMT-Service betreibt.

I. ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNG DER ENTWICKLUNGEN