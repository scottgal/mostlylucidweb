# Automatically Translating Markdown Files with EasyNMT

## Introduction
EasyNMT is a locally installable service that provides a simple interface to a number of machine translation services. In this tutorial, we will use EasyNMT to automatically translate a Markdown file from English to multiple languages.

## Prerequisites
An installation of EasyNMT is required to follow this tutorial. I usually run it as a Docker service. You can find the installation instructions [here](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) which covers how to run it as a docker service.
```shell 
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

OR if you have an NVIDIA GPU available:
```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

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