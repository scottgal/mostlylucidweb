# Traduzione automatica dei file Markdown con EasyNMT

## Introduzione

EasyNMT è un servizio localmente installabile che fornisce una semplice interfaccia a un certo numero di servizi di traduzione automatica. In questo tutorial, useremo EasyNMT per tradurre automaticamente un file Markdown dall'inglese in più lingue.

È possibile trovare tutti i file per questo tutorial nel [Repository GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) per questo progetto.

NOTA: Questo è ancora abbastanza ruvido, continuerò a raffinarlo mentre vado.

[TOC]

## Prerequisiti

Per seguire questo tutorial è necessaria un'installazione di EasyNMT. Di solito lo gestisco come servizio Docker. Puoi trovare le istruzioni di installazione [qui](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) che copre come farlo funzionare come un servizio docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

O se dispone di una GPU NVIDIA:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Le variabili d'ambiente MAX_WORKERS_BACKEND e MAX_WORKERS_FRONTEND impostano il numero di lavoratori che EasyNMT utilizzerà. Puoi adattarli alla tua macchina.

NOTA: EasyNMT non è il servizio SMOOTHEST da eseguire, ma è il meglio che ho trovato per questo scopo. È un po 'persnickety circa la stringa di ingresso è passato, quindi potrebbe essere necessario fare un po 'pre-elaborazione del testo di ingresso prima di passarlo a EasyNMT.

## Traduzione di un file Markdown

Questo è il codice che ho nel file MarkdownTranslatorService.cs. È un servizio semplice che prende una stringa di markdown e una lingua di destinazione e restituisce la stringa di markdown tradotta.

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

Come potete vedere ha una serie di passaggi:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Questo analizza la stringa di markdown in un documento.
2. `  var textStrings = ExtractTextStrings(document);` - Questo estrae le stringhe di testo dal documento.
3. `  var batchSize = 50;` - Questo imposta la dimensione del lotto per il servizio di traduzione. EasyNMT ha un limite al numero di caratteri che può tradurre in una sola volta.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Questo richiede il metodo che poi posta il batch al servizio EasyNMT.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Questo reinserisce le stringhe tradotte nel documento. Utilizzando la capacità di MarkDig di percorrere il documento e sostituire le stringhe di testo.

## Servizio ospitato

Per eseguire tutto questo uso un IHostedLifetimeService che viene avviato nel file Program.cs. Questo servizio legge in un file markdown, lo traduce in un certo numero di lingue e scrive i file tradotti su disco.

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

Come potete vedere controlla anche l'hash del file per vedere se è cambiato prima di tradurlo. Questo è per evitare di tradurre i file che non sono cambiati.

Questo viene fatto calcolando un hash veloce del file markdown originale quindi testando per vedere se quel file è cambiato prima di tentare di tradurlo.

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

La configurazione in Program.cs è abbastanza semplice:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

Ho creato l'HostedService (BackgroundTranslateService) e l'HttpClient per il MarkdownTranslatorService.
Un Servizio Hosted è un servizio di lunga durata che funziona in background. E 'un buon posto per mettere i servizi che hanno bisogno di funzionare continuamente in background o solo prendere un po 'di tempo per completare. La nuova interfaccia IHostedLifetimeService è un po 'più flessibile rispetto alla vecchia interfaccia IHostedService e ci permette di eseguire le attività completamente in background più facilmente rispetto al vecchio IHostedService.

Qui potete vedere che sto impostando il timeout per l'HttpClient a 15 minuti. Questo perché EasyNMT può essere un po 'lento a rispondere (soprattutto la prima volta utilizzando un modello di lingua). Sto anche impostando l'indirizzo base all'indirizzo IP della macchina che esegue il servizio EasyNMT.

I