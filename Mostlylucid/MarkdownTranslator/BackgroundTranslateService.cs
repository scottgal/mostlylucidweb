using Mostlylucid.Config;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Services;

namespace Mostlylucid.MarkdownTranslator;

public class BackgroundTranslateService(MarkdownConfig markdownConfig,
    TranslateServiceConfig translateServiceConfig,
    MarkdownTranslatorService blogService,
    ILogger<BackgroundTranslateService> logger) : IHostedLifecycleService
{
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
     logger.LogInformation("Background translation service started");
    }



    public async Task StopAsync(CancellationToken cancellationToken)
    {
       logger.LogInformation("Background translation service stopped");
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "usingimagesharpweb.md");

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
       
   
   

        logger.LogInformation("Background translation service started");
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service starting");
    }

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Background translation service stopped");
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
       logger.LogInformation("Background translation service stopping");
    }
}