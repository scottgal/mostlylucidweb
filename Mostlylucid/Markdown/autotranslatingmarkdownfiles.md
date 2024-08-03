# Automatically Translating Markdown Files with EasyNMT

## Introduction
EasyNMT is a locally installable service that provides a simple interface to a number of machine translation services. In this tutorial, we will use EasyNMT to automatically translate a Markdown file from English to multiple languages.

You can find all the files for this tutorial in the [GitHub repository](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) for this project.

NOTE: This is still pretty rough, I'll continue refining it as I go.

[TOC]

## Prerequisites
An installation of EasyNMT is required to follow this tutorial. I usually run it as a Docker service. You can find the installation instructions [here](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) which covers how to run it as a docker service.
```shell 
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

OR if you have an NVIDIA GPU available:
```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```
The MAX_WORKERS_BACKEND and MAX_WORKERS_FRONTEND environment variables set the number of workers that EasyNMT will use. You can adjust these to suit your machine.

NOTE: EasyNMT isn't the SMOOTHEST service to run, but it's the best I've found for this purpose. It is a bit persnickety about the input string it's passed, so you may need to do some pre-processing of your input text before passing it to EasyNMT.

## Translating a Markdown File
This is the code I have in the MarkdownTranslatorService.cs file. It's a simple service that takes a markdown string and a target language and returns the translated markdown string.

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
As you can see it has a number of steps:
1. ```  var document = Markdig.Markdown.Parse(markdown);``` - This parses the markdown string into a document.
2. ```  var textStrings = ExtractTextStrings(document);``` - This extracts the text strings from the document.
3. ```  var batchSize = 50;``` - This sets the batch size for the translation service. EasyNMT has a limit on the number of characters it can translate in one go.
4. ```csharp await Post(batch, targetLang, cancellationToken)```
This calls into the method which then posts the batch to the EasyNMT service.
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
5. ```  ReinsertTranslatedStrings(document, translatedStrings.ToArray());``` - This reinserts the translated strings back into the document. Using MarkDig's ability to walk the document and replace text strings.

## Hosted Service

To run all this I use an IHostedLifetimeService which is started in the Program.cs file. This service reads in a markdown file, translates it to a number of languages and writes the translated files out to disk.

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
As you can see it also checks the hash of the file to see if it's changed before translating it. This is to avoid translating files that haven't changed.

This is done by computing a fast hash of the original markdown file then testing to see if that file has changed before attempting to translate it. 

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


The Setup in Program.cs is pretty simple:
```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```
I set up the HostedService (BackgroundTranslateService) and the HttpClient for the MarkdownTranslatorService.
A Hosted Service is a long-running service that runs in the background. It's a good place to put services that need to run continuously in the background or just take a while to complete. The new IHostedLifetimeService interface is a bit more flexible than the old IHostedService interface and lets us run tasks completely in the background more easily than the older IHostedService.

Here you can see I'm setting the timeout for the HttpClient to 15 minutes. This is because EasyNMT can be a bit slow to respond (especially the first time using a language model). I'm also setting the base address to the IP address of the machine running the EasyNMT service.

I