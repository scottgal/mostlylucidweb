# EasyNMT:n avulla automaattisesti käännetään Markdown-tiedostoja

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Johdanto

EasyNMT on paikallisesti asennettava palvelu, joka tarjoaa yksinkertaisen käyttöliittymän useille konekääntämispalveluille. Tässä tutoriaalissa käytämme EasyNMT:tä kääntääksemme Markdown-tiedoston automaattisesti englannista useille kielille.

Tämän oppitunnin kaikki tiedostot löydät osoitteesta [GitHub-varasto](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) tälle hankkeelle.

Tämän tulosteena syntyi BUNCH uusia markown-tiedostoja kohdekielillä. Tämä on superyksinkertainen tapa saada blogikirjoitus käännettyä useille kielille.

[Käännettyjä viestejä](/translatedposts.png)

[TÄYTÄNTÖÖNPANO

## Edeltävät opinnot

Tämän opetusohjelman noudattaminen edellyttää EasyNMT:n asennusta. Yleensä pyöritän sitä Docker-palveluna. Asennusohjeet löydät [täällä](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) joka kattaa, kuinka sitä voi pyörittää docker-palveluna.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

TAI jos sinulla on NVIDIA GPU saatavilla:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_WORKERS_BACKEND ja MAX_WORKERS_FRONTEND -ympäristömuuttujat määrittävät EasyNMT:n käyttämien työntekijöiden määrän. Voit säätää nämä koneellesi sopivaksi.

HUOMAUTUS: EasyNMT ei ole SMOOTHEST-palvelu, mutta se on parasta, mitä olen tähän tarkoitukseen löytänyt. Se on hieman nirso syötteestä, jonka se on läpäissyt, joten voi olla, että sinun on esikäsiteltävä syötettäsi ennen kuin annat sen EasyNMT:lle.

Se myös käänsi "päätelmän" somehölynpölyyn ehdotuksen jättämisestä EU:lle... petkuttaen sen koulutuspakettia.

## Naiivi lähestymistapa kuorman tasapainottamiseen

Easy NMT on resurssien suhteen janoinen peto, joten MarkdownTranslatorService -palvelussani minulla on superyksinkertainen satunnainen IP-valitsin, joka vain pyörittää EasyNMT-ohjelmaluettelon IP-luetteloa.

Alun perin tämä saa aikaan sen, että `model_name` EasyNMT-palvelun menetelmä on nopea ja yksinkertainen tapa tarkistaa, onko palvelu päällä. Jos näin on, se lisää IP:n työllisten tutkimusinfrastruktuurien luetteloon. Jos se ei ole, se ei lisää sitä listaan.

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

Sitten sisällä `Post` menetelmä, jolla `MarkdownTranslatorService` kierrämme työ-ip:n kautta, että löydämme seuraavan.

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

Tämä on superyksinkertainen tapa ladata pyynnöt tasapainoon useiden koneiden välillä. Se ei ole täydellinen (se ei selitä super kiireistä konetta tenttiin), mutta se on tarpeeksi hyvä tarkoituksiini.

Ääliö ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` vain pyörittää listaa nollasta alkaen ja menee listan pituuteen.

## Käännetään markdown-tiedostoa

Tämä on koodi, joka minulla on MarkdownTranslatorService.cs-tiedostossa. Se on yksinkertainen palvelu, joka vie merkkijonon ja kohdekielen ja palauttaa käännetyn merkkijonon.

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

Kuten näet, siinä on useita vaiheita:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Tämä jäsentää merkkijonon asiakirjaksi.
2. `  var textStrings = ExtractTextStrings(document);` - Tämä poistaa tekstijonot asiakirjasta.
   Tässä käytetään menetelmää

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Tämä tarkistaa, onko "sana" todella teos; kuvanimet voivat sotkea lauseen jakotoiminnon EasyNMT:ssä.

3. `  var batchSize = 10;` - Tämä määrää käännöspalvelun erän koon. EasyNMT:llä on raja sanojen määrälle, jonka se voi kääntää yhdellä kertaa (noin 500, joten 10 riviä on yleensä hyvä eräkoko tässä).
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Tämä edellyttää menetelmää, joka lähettää erän EasyNMT-palveluun.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Tämä lisää käännetyt jouset takaisin asiakirjaan. Käyttämällä MarkDigin kykyä kävellä dokumentissa ja korvata tekstijoustoja.

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

## Isännöity palvelu

Suoritan kaiken tämän käyttämällä IHostedLifetimeService -palvelua, joka on aloitettu Program.cs-tiedostossa. Tämä palvelu lukee markdown-tiedostoa, kääntää sen useille kielille ja kirjoittaa käännetyt tiedostot levylle.

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

Kuten näet, se tarkistaa myös tiedoston hasiksen nähdäkseen, onko se muuttunut ennen sen kääntämistä. Näin vältät muuttumattomien tiedostojen kääntämisen.

Tämä tehdään laskemalla nopea hash alkuperäinen markown-tiedosto ja testaamalla, onko tiedosto muuttunut ennen kuin yrität kääntää sitä.

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

Setup in Program.cs on aika yksinkertainen:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

Perustin HostedServicen (Background TranslateService) ja Markdown TranslatorServicen HttpClientin.
Hosted Service on taustalla kulkeva pitkäaikainen palvelu. Se on hyvä paikka laittaa palvelut, joiden pitää pyöriä jatkuvasti taustalla tai kestää vain jonkin aikaa. Uusi IHostedLifetimeService -käyttöliittymä on hieman joustavampi kuin vanha IHostedService -käyttöliittymä ja antaa meidän hoitaa tehtävät täysin taustalla helpommin kuin vanhempi IHostedService -käyttöliittymä.

Tässä näette, että asetan HttpClientin aikalisän 15 minuuttiin. Tämä johtuu siitä, että EasyNMT voi olla hieman hidas reagoimaan (etenkin ensimmäisellä kerralla käyttämällä kielimallia). Asetan myös EasyNMT-palvelua käyttävän koneen IP-osoitteen.

## Johtopäätöksenä

Tämä on melko yksinkertainen tapa kääntää markown-tiedosto useille kielille. Se ei ole täydellinen, mutta hyvä alku. Yleensä pyöritän tätä jokaiseen uuteen blogikirjoitukseen, ja sitä käytetään `MarkdownBlogService` vetämään käännetyt nimet jokaiseen blogikirjoitukseen.