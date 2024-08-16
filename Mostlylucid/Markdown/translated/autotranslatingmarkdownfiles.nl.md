# Automatisch mark-downbestanden vertalen met EasyNMT

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Inleiding

EasyNMT is een lokaal te installeren dienst die een eenvoudige interface biedt naar een aantal machinevertaaldiensten. In deze tutorial zullen we EasyNMT gebruiken om automatisch een Markdown-bestand van Engels naar meerdere talen te vertalen.

U kunt alle bestanden voor deze tutorial vinden in de [GitHub repository](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) voor dit project.

De uitvoer hiervan genereerde een BUNCH van nieuwe markdown bestanden in de doeltalen. Dit is een super eenvoudige manier om een blog post vertaald in meerdere talen.

[Vertaalde berichten](/translatedposts.png)

[TOC]

## Vereisten

Een installatie van EasyNMT is vereist om deze tutorial te volgen. Ik doe het meestal als een Docker service. U kunt de installatie-instructies vinden [Hier.](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) die betrekking heeft op hoe het te draaien als een docker service.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

OF als u een NVIDIA GPU beschikbaar heeft:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

De MAX_WORKERS_BACKEND en MAX_WORKERS_FRONTEND omgevingsvariabelen bepalen het aantal werknemers dat EasyNMT zal gebruiken. U kunt deze aanpassen aan uw machine.

OPMERKING: EasyNMT is niet de SMOOTHEST dienst om te draaien, maar het is de beste die ik heb gevonden voor dit doel. Het is een beetje persnickety over de invoer string die het is doorgegeven, dus je kan nodig hebben om een aantal pre-verwerking van uw invoer tekst te doen voordat het door te geven aan EasyNMT.

Oh en het vertaalde ook 'Conclusie' in een of andere onzin over het indienen van het voorstel bij de EU...verraad van de training set.

## Native Approach to Load Balancing

Easy NMT is een dorst beest als het gaat om middelen, dus in mijn MarkdownVertalerService heb ik een super eenvoudige willekeurige IP-selector die gewoon draait door de lijst van IP's van een lijst van machines die ik gebruik om EasyNMT draaien.

In eerste instantie doet dit een stap op de `model_name` methode op de EasyNMT service, dit is een snelle, eenvoudige manier om te controleren of de service is up. Als dat zo is, voegt het IP toe aan een lijst van werkende IP's. Als dat niet zo is, voegt het het niet toe aan de lijst.

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

Vervolgens binnen de `Post` methode van `MarkdownTranslatorService` We draaien door de werkende IP's om de volgende te vinden.

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

Dit is een super eenvoudige manier om het saldo van de verzoeken over een aantal machines te laden. Het is niet perfect (het is niet verantwoordelijk voor een super drukke machine voor exampel), maar het is goed genoeg voor mijn doeleinden.

De schmick. ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` draait gewoon door de lijst van IP's die begint op 0 en gaat naar de lengte van de lijst.

## Een markdown-bestand vertalen

Dit is de code die ik heb in het MarkdownVertalerService.cs bestand. Het is een eenvoudige dienst die een markdown string en een doeltaal neemt en de vertaalde markdown string teruggeeft.

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

Zoals u kunt zien heeft het een aantal stappen:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Dit verwerkt de markdown string in een document.
2. `  var textStrings = ExtractTextStrings(document);` - Dit haalt de tekststrings uit het document.
   Dit maakt gebruik van de methode

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Dit controleert of het 'woord' echt een werk is; afbeeldingsnamen kunnen de zinsverdelingsfunctionaliteit in EasyNMT verstoren.

3. `  var batchSize = 10;` - Dit bepaalt de batchgrootte voor de vertaaldienst. EasyNMT heeft een limiet op het aantal woorden dat het kan vertalen in één keer (ongeveer 500, dus 10 lijnen is over het algemeen een goede batch grootte hier).
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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Dit plaatst de vertaalde tekenreeksen terug in het document. Met behulp van MarkDig's mogelijkheid om het document te laten lopen en teksttekens te vervangen.

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

## Hosted Service

Om dit alles uit te voeren gebruik ik een IHostedLifetimeService die gestart is in het programma.cs bestand. Deze dienst leest in een markdown bestand, vertaalt het naar een aantal talen en schrijft de vertaalde bestanden naar de schijf.

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

Zoals je kunt zien controleert het ook de hash van het bestand om te zien of het is veranderd voordat het te vertalen. Dit is om te voorkomen dat het vertalen van bestanden die niet zijn veranderd.

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
});
```

Ik heb de HostedService (BackgroundTranslateService) en de HttpClient voor de MarkdownTranslatorService opgezet.
Een Hosted Service is een langlopende service die op de achtergrond draait. Het is een goede plek om diensten te plaatsen die continu op de achtergrond moeten draaien of gewoon een tijdje duren om te voltooien. De nieuwe IHostedLifetimeService interface is een beetje flexibeler dan de oude IHostedService interface en laat ons taken volledig op de achtergrond gemakkelijker uitvoeren dan de oudere IHostedService.

Hier zie je dat ik de time-out voor de HttpClient op 15 minuten zet. Dit komt omdat EasyNMT een beetje traag kan reageren (vooral de eerste keer met behulp van een taalmodel). Ik stel ook het basisadres in op het IP-adres van de machine die de EasyNMT service draait.

## Conclusie

Dit is een vrij eenvoudige manier om een markdown bestand te vertalen naar meerdere talen. Het is niet perfect, maar het is een goed begin. Ik over het algemeen uitvoeren van dit voor elke nieuwe blog post en het wordt gebruikt in de `MarkdownBlogService` om de vertaalde namen voor elke blog post te trekken.