# Traduzione automatica dei file Markdown con EasyNMT

## Introduzione

EasyNMT è un servizio localmente installabile che fornisce una semplice interfaccia a un certo numero di servizi di traduzione automatica. In questo tutorial, useremo EasyNMT per tradurre automaticamente un file Markdown dall'inglese in più lingue.

## Prerequisiti

Per seguire questo tutorial è necessaria un'installazione di EasyNMT. Di solito lo gestisco come servizio Docker. Puoi trovare le istruzioni di installazione [qui](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) che copre come farlo funzionare come un servizio docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

O se dispone di una GPU NVIDIA:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

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

1. `csharp  var document = Markdig.Markdown.Parse(markdown);` - Questo analizza la stringa di markdown in un documento.
2. `csharp  var textStrings = ExtractTextStrings(document);` - Questo estrae le stringhe di testo dal documento.
3. `csharp  var batchSize = 50;` - Questo imposta la dimensione del lotto per il servizio di traduzione. EasyNMT ha un limite al numero di caratteri che può tradurre in una sola volta.
4. `csharp await Post(batch, targetLang, cancellationToken)`