# Traduzione automatica dei file Markdown con EasyNMT

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Introduzione

EasyNMT è un servizio localmente installabile che fornisce una semplice interfaccia a un certo numero di servizi di traduzione automatica. In questo tutorial, useremo EasyNMT per tradurre automaticamente un file Markdown dall'inglese in più lingue.

È possibile trovare tutti i file per questo tutorial nel [Repository GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) per questo progetto.

L'output di questo ha generato un BUNCH di nuovi file di markdown nelle lingue di destinazione. Questo è un modo super semplice per ottenere un post sul blog tradotto in più lingue.

[Post tradotti](/translatedposts.png)

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

Oh e ha anche tradotto 'Conclusione' ad alcune sciocchezze sul presentare la proposta all'UE... tradire è set di formazione.

## Approccio naive al bilanciamento del carico

Easy NMT è una bestia assetata quando si tratta di risorse, quindi nel mio MarkdownTranslatorService ho un selettore IP casuale super semplice che ruota attraverso l'elenco degli IP di una lista di macchine che uso per eseguire EasyNMT.

Inizialmente questo fa un get on `model_name` metodo sul servizio EasyNMT, questo è un modo semplice e veloce per verificare se il servizio è attivo. Se lo è, aggiunge l'IP ad un elenco di IP funzionanti. Se non lo è, non lo aggiunge alla lista.

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

Poi all'interno della `Post` metodo di `MarkdownTranslatorService` Ruotiamo attraverso gli IP di lavoro per trovare quello successivo.

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

Questo è un modo super semplice per caricare l'equilibrio delle richieste su un certo numero di macchine. Non è perfetto (non spiega per una macchina super occupato per l'esamepel), ma è abbastanza buono per i miei scopi.

Lo scimmicco... ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` semplicemente ruota attraverso l'elenco degli IP a partire da 0 e andando alla lunghezza dell'elenco.

## Traduzione di un file Markdown

Questo è il codice che ho nel file MarkdownTranslatorService.cs. È un servizio semplice che prende una stringa di markdown e una lingua di destinazione e restituisce la stringa di markdown tradotta.

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

Come potete vedere ha una serie di passaggi:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Questo analizza la stringa di markdown in un documento.
2. `  var textStrings = ExtractTextStrings(document);` - Questo estrae le stringhe di testo dal documento.
   In questo modo si utilizza il metodo

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Questo controlla se la 'parola' è davvero un lavoro; i nomi delle immagini possono rovinare la funzionalità di divisione frase in EasyNMT.

3. `  var batchSize = 10;` - Questo imposta la dimensione del lotto per il servizio di traduzione. EasyNMT ha un limite al numero di parole che può tradurre in una sola volta (circa 500, quindi 10 linee è generalmente una buona dimensione batch qui).
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

## Servizio ospitato

Per eseguire tutto questo uso un IHostedLifetimeService che viene avviato nel file Program.cs. Questo servizio legge in un file markdown, lo traduce in un certo numero di lingue e scrive i file tradotti su disco.

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
});
```

Ho creato l'HostedService (BackgroundTranslateService) e l'HttpClient per il MarkdownTranslatorService.
Un Servizio Hosted è un servizio di lunga durata che funziona in background. E 'un buon posto per mettere i servizi che hanno bisogno di funzionare continuamente in background o solo prendere un po 'di tempo per completare. La nuova interfaccia IHostedLifetimeService è un po 'più flessibile rispetto alla vecchia interfaccia IHostedService e ci permette di eseguire le attività completamente in background più facilmente rispetto al vecchio IHostedService.

Qui potete vedere che sto impostando il timeout per l'HttpClient a 15 minuti. Questo perché EasyNMT può essere un po 'lento a rispondere (soprattutto la prima volta utilizzando un modello di lingua). Sto anche impostando l'indirizzo base all'indirizzo IP della macchina che esegue il servizio EasyNMT.

## In conclusione

Questo è un modo abbastanza semplice per tradurre un file markdown in più lingue. Non e' perfetto, ma e' un buon inizio. Io generalmente eseguire questo per ogni nuovo post sul blog e viene utilizzato nel `MarkdownBlogService` per tirare i nomi tradotti per ogni post del blog.