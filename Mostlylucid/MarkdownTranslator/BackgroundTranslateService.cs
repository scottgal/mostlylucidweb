using Mostlylucid.Services;

namespace Mostlylucid.MarkdownTranslator;

public class BackgroundTranslateService(
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
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 2, CancellationToken = cancellationToken};
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "gr", "jap", "zh" };
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