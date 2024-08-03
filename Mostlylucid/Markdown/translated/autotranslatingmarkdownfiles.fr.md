# Traduire automatiquement les fichiers de balisage avec EasyNMT

## Présentation

EasyNMT est un service installable localement qui fournit une interface simple à un certain nombre de services de traduction automatique. Dans ce tutoriel, nous utiliserons EasyNMT pour traduire automatiquement un fichier Markdown de l'anglais vers plusieurs langues.

Vous pouvez trouver tous les fichiers pour ce tutoriel dans le [Dépôt GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) pour ce projet.

NOTE: C'est encore assez dur, je vais continuer à l'affiner au fur et à mesure.

[TOC]

## Préalables

Une installation de EasyNMT est nécessaire pour suivre ce tutoriel. D'habitude, c'est un service Docker. Vous pouvez trouver les instructions d'installation [Ici.](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) qui couvre la façon de le faire fonctionner en tant que service de docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

OU si vous avez un GPU NVIDIA disponible:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Les variables d'environnement MAX_WORKERS_BACKEND et MAX_WORKERS_FRONTEND définissent le nombre de travailleurs que EasyNMT utilisera. Vous pouvez les ajuster en fonction de votre machine.

NOTE: EasyNMT n'est pas le service SMOOTHEST à exécuter, mais c'est le meilleur que j'ai trouvé à cet effet. Il est un peu persnickety sur la chaîne d'entrée qu'il est passé, de sorte que vous pouvez avoir besoin de faire un certain pré-traitement de votre texte d'entrée avant de le passer à EasyNMT.

## Traduire un fichier Markdown

C'est le code que j'ai dans le fichier MarkdownTranslatorService.cs. C'est un service simple qui prend une chaîne de balisage et une langue cible et renvoie la chaîne de balisage traduite.

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

Comme vous pouvez le voir, il comporte un certain nombre d'étapes :

1. `  var document = Markdig.Markdown.Parse(markdown);` - Cela analyse la chaîne de marquage dans un document.
2. `  var textStrings = ExtractTextStrings(document);` - Ceci extrait les chaînes de texte du document.
3. `  var batchSize = 50;` - Cela définit la taille du lot pour le service de traduction. EasyNMT a une limite sur le nombre de caractères qu'il peut traduire en un seul coup.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Cela fait appel à la méthode qui envoie ensuite le lot au service EasyNMT.

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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Cela réinsère les chaînes traduites dans le document. En utilisant la capacité de MarkDig de marcher le document et de remplacer les chaînes de texte.

## Service hébergé

Pour exécuter tout cela, j'utilise un IHostedLifetimeService qui est démarré dans le fichier Program.cs. Ce service se lit dans un fichier balisage, le traduit dans un certain nombre de langues et écrit les fichiers traduits sur disque.

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

Comme vous pouvez le voir, il vérifie également le hash du fichier pour voir s'il a changé avant de le traduire. C'est pour éviter de traduire des fichiers qui n'ont pas changé.

Cela se fait en calculant un hash rapide du fichier de balisage original puis en testant pour voir si ce fichier a changé avant de tenter de le traduire.

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

L'installation dans Program.cs est assez simple :

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

J'ai mis en place le ServiceHosted (BackgroundTranslateService) et le HttpClient pour le Service de Traducteur Markdown.
Un service hébergé est un service de longue durée qui fonctionne en arrière-plan. C'est un bon endroit pour mettre des services qui doivent fonctionner en continu en arrière-plan ou juste prendre un peu de temps à compléter. La nouvelle interface IHostedLifetimeService est un peu plus flexible que l'ancienne interface IHostedService et nous permet d'exécuter les tâches complètement en arrière-plan plus facilement que l'ancienne IHostedService.

Ici vous pouvez voir que je règle le temps d'arrêt pour le HttpClient à 15 minutes. C'est parce qu'EasyNMT peut être un peu lent à répondre (surtout la première fois à l'aide d'un modèle de langue). Je mets également l'adresse de base à l'adresse IP de la machine exécutant le service EasyNMT.

Annexe I