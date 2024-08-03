# Automatisch mark-downbestanden vertalen met EasyNMT

## Inleiding

EasyNMT is een lokaal installeerbare dienst die een eenvoudige interface biedt naar een aantal machinevertaaldiensten. In deze tutorial gebruiken we EasyNMT om automatisch een Markdown-bestand van Engels naar meerdere talen te vertalen.

U kunt alle bestanden voor deze tutorial vinden in de[GitHub repository](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator)voor dit project.

LET OP: Dit is nog steeds vrij ruw, Ik zal blijven verfijnen als ik ga.

[TOC]

## Vereisten

Een installatie van EasyNMT is vereist om deze tutorial te volgen. Ik voer het meestal uit als een Docker service. U kunt de installatie instructies vinden[Hier.](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md)die betrekking heeft op hoe het te draaien als een docker service.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

OF als u een NVIDIA GPU beschikbaar heeft:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

De MAX_WORKERS_BACKEND en MAX_WORKERS_FRONTEND omgevingsvariabelen stellen het aantal werknemers in dat EasyNMT zal gebruiken. U kunt deze aanpassen aan uw machine.

OPMERKING: EasyNMT is niet de SMOOTHEST-service om uit te voeren, maar het is de beste die ik heb gevonden voor dit doel. Het is een beetje persnickety over de invoer string die het is doorgegeven, dus je kan nodig hebben om wat pre-processing van uw invoer tekst te doen voordat het door te geven aan EasyNMT.

## Een markdown-bestand vertalen

Dit is de code die ik heb in het MarkdownTranslatorService.cs bestand. Het is een eenvoudige dienst die een markdown string en een doeltaal neemt en de vertaalde markdown string teruggeeft.

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

Zoals u kunt zien heeft het een aantal stappen:

1. `  var document = Markdig.Markdown.Parse(markdown);`- Dit verwerkt de markdown string in een document.
2. `  var textStrings = ExtractTextStrings(document);`- Dit haalt de tekststrings uit het document.
3. `  var batchSize = 50;`- Dit stelt de batchgrootte voor de vertaaldienst in. EasyNMT heeft een limiet op het aantal tekens dat het in één keer kan vertalen.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Dit roept op tot de methode die vervolgens de batch plaatst naar de EasyNMT service.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());`- Dit plaatst de vertaalde tekenreeksen weer terug in het document. Met behulp van MarkDig's mogelijkheid om het document te laten lopen en teksttekens te vervangen.

## Hosted Service

Om dit alles uit te voeren gebruik ik een IHostedLifetimeService die wordt gestart in het Program.cs bestand. Deze dienst leest in een markdown bestand, vertaalt het naar een aantal talen en schrijft de vertaalde bestanden naar schijf.

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

Zoals u kunt zien controleert het ook de hash van het bestand om te zien of het is veranderd voordat het te vertalen. Dit is om te voorkomen dat het vertalen van bestanden die niet zijn veranderd.

Dit wordt gedaan door het berekenen van een snelle hash van het oorspronkelijke markdown bestand vervolgens testen om te zien of dat bestand is veranderd voordat het probeert te vertalen.

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

De Setup in Program.cs is vrij eenvoudig:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

Ik heb de HostedService (BackgroundTranslateService) en de HttpClient voor de MarkdownTranslatorService opgezet.
Een Hosted Service is een langlopende service die op de achtergrond draait. Het is een goede plek om diensten te plaatsen die continu op de achtergrond moeten draaien of gewoon een tijdje duren om te voltooien. De nieuwe IHostedLifetimeService interface is een beetje flexibeler dan de oude IHostedService interface en laat ons taken volledig op de achtergrond gemakkelijker uitvoeren dan de oudere IHostedService.

Hier kunt u zien dat ik de timeout voor de HttpClient op 15 minuten zet. Dit komt omdat EasyNMT een beetje traag kan reageren (vooral de eerste keer met een taalmodel). Ik stel ook het basisadres in op het IP-adres van de machine die de EasyNMT service draait.

I