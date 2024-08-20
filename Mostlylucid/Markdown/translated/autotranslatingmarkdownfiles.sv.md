# Automatiskt översätta markeringsfiler med EasyNMT

<datetime class="hidden">2024-08-03T13:30 Ordförande</datetime>

<!--category-- EasyNMT, Markdown -->
## Inledning

EasyNMT är en lokalt installationsbar tjänst som ger ett enkelt gränssnitt till ett antal maskinöversättningstjänster. I denna handledning kommer vi att använda EasyNMT för att automatiskt översätta en Markdown-fil från engelska till flera språk.

Du kan hitta alla filer för denna handledning i [GitHub- arkivName](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) för detta projekt.

Utmatningen av detta genererade en BUNCH av nya markdown-filer på målspråken. Detta är ett super enkelt sätt att få ett blogginlägg översatt till flera språk.

[Översatta inlägg](/translatedposts.png)

[TOC]

## Förutsättningar

En installation av EasyNMT krävs för att följa denna handledning. Jag brukar sköta det som en Docker-tjänst. Du hittar installationsanvisningarna [här](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) som täcker hur man kör det som en docker service.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

ELLER om du har en NVIDIA GPU tillgänglig:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_WORKERS_BACKEND och MAX_WORKERS_FRONTEND miljövariabler ange antalet arbetstagare som EasyNMT kommer att använda. Du kan justera dessa för att passa din maskin.

OBS: EasyNMT är inte den SMOOTHEST tjänst att köra, men det är det bästa jag har hittat för detta ändamål. Det är lite persnicketty om inmatningssträngen det har passerat, så du kan behöva göra lite förbehandling av din ingång text innan du skickar den till EasyNMT.

Och det översatte också "Slutsats" till en del nonsens om att lägga fram förslaget för EU... för att lura dess utbildning uppsättning.

## Naive tillvägagångssätt för att ladda balansering

Easy NMT är en törst odjur när det gäller resurser, så i min MarkdownTranslatorService har jag en super enkel slumpmässig IP-väljare som bara roterar genom listan över IPs av en lista över maskiner jag använder för att köra EasyNMT.

Inledningsvis detta gör en få på `model_name` metoden på EasyNMT tjänsten, är detta ett snabbt, enkelt sätt att kontrollera om tjänsten är uppe. Om den är det, lägger den till IP-adressen till en lista över fungerande IP-adresser. Om det inte är det, lägger det inte till det på listan.

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

Därefter inom `Post` metod för `MarkdownTranslatorService` vi roterar genom arbets-IPs för att hitta nästa.

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

Detta är ett super enkelt sätt att ladda balans förfrågningar över ett antal maskiner. Det är inte perfekt (det förklarar inte för en super upptagen maskin för provspel), men det är tillräckligt bra för mina syften.

Den där schmicken. ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` roterar bara genom listan över IPs som börjar vid 0 och går till listans längd.

## Översätter en nerräkningsfil@ info: whatsthis

Det här är koden jag har i MarkdownTranslatorService.cs-filen. Det är en enkel tjänst som tar en markdown sträng och ett målspråk och returnerar den översatta markdown strängen.

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

Som du kan se det har ett antal steg:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Det här tolkar markeringssträngen till ett dokument.
2. `  var textStrings = ExtractTextStrings(document);` - Detta extraherar texten strängar från dokumentet.
   Detta använder metoden

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Detta kontrollerar om "ordet" verkligen är ett verk; bildnamn kan förstöra mening delning funktionalitet i EasyNMT.

3. `  var batchSize = 10;` - Detta anger batchstorleken för översättningstjänsten. EasyNMT har en gräns för antalet ord det kan översätta i en go (ca 500, så 10 rader är i allmänhet en bra batch storlek här).
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Detta kallar in metoden som sedan lägger batchen till EasyNMT tjänsten.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Det här sätter tillbaka de översatta strängarna i dokumentet. Använda MarkDigs förmåga att gå dokumentet och ersätta textsträngar.

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

## Värdtjänst

För att köra allt detta använder jag en IHostedLifetimeService som startas i filen Program.cs. Denna tjänst läser i en markdown-fil, översätter den till ett antal språk och skriver de översatta filerna ut till disk.

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

Som du kan se kontrollerar det också hash i filen för att se om den har ändrats innan du översätter den. Detta för att undvika att översätta filer som inte har ändrats.

Detta görs genom att beräkna en snabb hash av den ursprungliga markdown-filen sedan testa för att se om den filen har ändrats innan du försöker översätta den.

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

Inställningen i Program.cs är ganska enkel:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

Jag satte upp HostedService (BakgrundTranslateService) och HttpClient för MarkdownTranslatorService.
En Hosted Service är en långvarig tjänst som körs i bakgrunden. Det är en bra plats att sätta tjänster som behöver köras kontinuerligt i bakgrunden eller bara ta ett tag att slutföra. Det nya IHostedLifetimeService-gränssnittet är lite mer flexibelt än det gamla IHostedService-gränssnittet och låter oss köra uppgifter helt i bakgrunden lättare än den äldre IHostedService.

Här kan du se att jag ställer in tiden för HttpClient till 15 minuter. Detta beror på att EasyNMT kan vara lite långsam att svara (särskilt första gången med en språkmodell). Jag ställer också in basadressen till IP-adressen till maskinen som kör EasyNMT-tjänsten.

## Slutsatser

Detta är ett ganska enkelt sätt att översätta en markdown-fil till flera språk. Det är inte perfekt, men det är en bra början. Jag brukar köra detta för varje nytt blogginlägg och det används i `MarkdownBlogService` att dra de översatta namnen för varje blogginlägg.